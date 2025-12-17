using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dataverse.Browser.Context;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.Web.WebView2.Core;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    internal class WebApiRequestHandler
    {
        private BrowserContext Context { get; }
        private RequestConverter RequestConverter { get; }
        private ResponseConverter ResponseConverter { get; }

        public WebApiRequestHandler(BrowserContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.RequestConverter = new RequestConverter(this.Context);
            this.ResponseConverter = new ResponseConverter(this.Context);
        }

        public bool TryHandle(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2Environment environment)
        {
            WebApiRequest webApiRequest;
            try
            {
                webApiRequest = CreateWebApiRequest(e.Request);
            }
            catch (Exception ex)
            {
                LogToFile("[WebApi] failed to read request: " + ex);
                return false;
            }
            if (webApiRequest == null)
            {
                return false;
            }

            var conversionResult = this.RequestConverter.Convert(webApiRequest);
            
            // RequestConverter.Convert can return null for system metadata functions
            if (conversionResult == null)
            {
                var proxyResponse = ProxyToDataverse(e.Request);
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                return true;
            }
            
            var interceptedRequest = new InterceptedWebApiRequest(conversionResult);
            
            // Add null safety for Context and LastRequests (defensive programming)
            if (this.Context?.LastRequests == null)
            {
                var proxyResponse = ProxyToDataverse(e.Request);
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                return true;
            }
            
            this.Context.LastRequests.AddRequest(interceptedRequest);

            if (conversionResult.ConvertedRequest == null || !this.Context.IsEnabled)
            {
                var proxyResponse = ProxyToDataverse(e.Request);
                interceptedRequest.Proxied = true;
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
                return true;
            }

            WebApiResponse response;
            try
            {
                // Check if proxy is available (might be null during hot reload)
                if (this.Context.ProxyWithEmulator == null)
                {
                    var proxyResponse = ProxyToDataverse(e.Request);
                    interceptedRequest.Proxied = true;
                    e.Response = BuildWebViewResponse(environment, proxyResponse);
                    this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
                    return true;
                }
                
                interceptedRequest.ExecutionTreeRoot = new ExecutionTreeNode();
                var orgResponse = this.Context.ProxyWithEmulator.ExecuteWithTree(conversionResult.ConvertedRequest, interceptedRequest.ExecutionTreeRoot);
                response = this.ResponseConverter.Convert(conversionResult, orgResponse);
                this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
            }
            catch (Exception ex)
            {
                interceptedRequest.ExecuteException = ex;
                this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
                response = this.ResponseConverter.Convert(ex);
            }

            e.Response = BuildWebViewResponse(environment, response);
            return true;
        }

        private static WebApiRequest CreateWebApiRequest(CoreWebView2WebResourceRequest request)
        {
            string body = null;
            if (request.Content != null && request.Content.CanRead)
            {
                byte[] buffer;
                using (var ms = new MemoryStream())
                {
                    request.Content.CopyTo(ms);
                    buffer = ms.ToArray();
                }
                body = Encoding.UTF8.GetString(buffer);
                if (request.Content.CanSeek)
                {
                    request.Content.Position = 0;
                }
                else
                {
                    // Replace with a rewindable stream so we can reuse the body later (e.g. when proxying).
                    request.Content = new MemoryStream(buffer);
                }
            }

            var headers = new NameValueCollection();
            // Enumerate headers (WebView2 iterator API).
            var iterator = request.Headers.GetIterator();
            while (iterator.HasCurrentHeader)
            {
                var current = iterator.Current;
                if (!string.IsNullOrEmpty(current.Key))
                {
                    headers.Add(current.Key, current.Value);
                }
                if (!iterator.MoveNext())
                {
                    break;
                }
            }

            var apiRequest = WebApiRequest.Create(request.Method, request.Uri, headers, body);
            return apiRequest;
        }

        private WebApiResponse ProxyToDataverse(CoreWebView2WebResourceRequest request)
        {
            var msg = new HttpRequestMessage(new HttpMethod(request.Method), request.Uri);
            this.Context.AddAuthorizationHeaders(msg);

            // Copy headers
            var iterator = request.Headers.GetIterator();
            while (iterator.HasCurrentHeader)
            {
                var current = iterator.Current;
                // Content-Type goes to content
                if (!string.IsNullOrEmpty(current.Key))
                {
                    if (string.Equals(current.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        // handled below with content
                    }
                    else
                    {
                        msg.Headers.TryAddWithoutValidation(current.Key, current.Value);
                    }
                }
                if (!iterator.MoveNext())
                {
                    break;
                }
            }

            // Content
            if (request.Content != null && request.Content.CanRead)
            {
                byte[] body;
                using (var ms = new MemoryStream())
                {
                    request.Content.CopyTo(ms);
                    body = ms.ToArray();
                }
                var content = new ByteArrayContent(body);
                var ct = request.Headers.GetHeader("Content-Type");
                if (!string.IsNullOrEmpty(ct))
                {
                    content.Headers.TryAddWithoutValidation("Content-Type", ct);
                }
                msg.Content = content;
            }

            var httpResponse = this.Context.HttpClient.SendAsync(msg).Result;
            var responseBody = httpResponse.Content.ReadAsByteArrayAsync().Result;

            var headers = new NameValueCollection();
            foreach (var h in httpResponse.Headers)
            {
                headers.Add(h.Key, string.Join(",", h.Value));
            }
            foreach (var h in httpResponse.Content.Headers)
            {
                headers.Add(h.Key, string.Join(",", h.Value));
            }

            return new WebApiResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                Body = responseBody,
                Headers = headers
            };
        }

        private static CoreWebView2WebResourceResponse BuildWebViewResponse(CoreWebView2Environment env, WebApiResponse response)
        {
            var bodyStream = response.Body != null ? new MemoryStream(response.Body) : Stream.Null;
            string statusText = response.StatusCode >= 200 && response.StatusCode < 300 ? "OK" : "Error";
            string headerString = ToHeaderString(response.Headers);
            return env.CreateWebResourceResponse(bodyStream, response.StatusCode, statusText, headerString);
        }

        private static string ToHeaderString(NameValueCollection headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return string.Empty;
            }
            var builder = new StringBuilder();
            foreach (string key in headers.AllKeys)
            {
                builder.Append(key).Append(": ").Append(headers[key]).Append("\r\n");
            }
            return builder.ToString();
        }

        private static void LogToFile(string message)
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dataverse.Browser", "webview_api.log");
                File.AppendAllText(path, $"{DateTime.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}
