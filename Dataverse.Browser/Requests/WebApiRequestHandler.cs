using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
        private bool AlwaysProxyWebApi { get; }
        private bool InterceptNonGetOnly { get; }

        public WebApiRequestHandler(BrowserContext context, bool alwaysProxyWebApi, bool interceptNonGetOnly)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.AlwaysProxyWebApi = alwaysProxyWebApi;
            this.InterceptNonGetOnly = interceptNonGetOnly;
            this.RequestConverter = new RequestConverter(this.Context);
            this.ResponseConverter = new ResponseConverter(this.Context);
        }

        public async Task<bool> TryHandleAsync(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2Environment environment)
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
                var proxyResponse = await ProxyToDataverseAsync(e.Request);
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                return true;
            }
            
            var interceptedRequest = new InterceptedWebApiRequest(conversionResult);
            
            // Add null safety for Context and LastRequests (defensive programming)
            if (this.Context?.LastRequests == null)
            {
                var proxyResponse = await ProxyToDataverseAsync(e.Request);
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                return true;
            }
            
            this.Context.LastRequests.AddRequest(interceptedRequest);

            if (this.InterceptNonGetOnly)
            {
                // Let the embedded browser handle all GETs (auth via cookies, better perf).
                // Only non-GET calls are intercepted so local plugins can execute and debugger can break.
                if (string.Equals(e.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Only intercept when plugin execution is enabled and conversion succeeded.
                if (!this.Context.IsEnabled || conversionResult.ConvertedRequest == null || this.Context.ProxyWithEmulator == null)
                {
                    return false;
                }

                WebApiResponse nonGetResponse;
                try
                {
                    interceptedRequest.ExecutionTreeRoot = new ExecutionTreeNode();
                    var orgResponse = this.Context.ProxyWithEmulator.ExecuteWithTree(conversionResult.ConvertedRequest, interceptedRequest.ExecutionTreeRoot);
                    nonGetResponse = this.ResponseConverter.Convert(conversionResult, orgResponse);
                }
                catch (Exception ex)
                {
                    interceptedRequest.ExecuteException = ex;
                    nonGetResponse = this.ResponseConverter.Convert(ex);
                }

                e.Response = BuildWebViewResponse(environment, nonGetResponse);
                this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
                return true;
            }

            if (this.AlwaysProxyWebApi)
            {
                LogToFile("[WebApi] AlwaysProxy enabled: " + e.Request.Method + " " + e.Request.Uri);
                var proxyResponse = await ProxyToDataverseAsync(e.Request);
                interceptedRequest.Proxied = true;
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
                return true;
            }

            // DRB (Dataverse REST Builder) is a webresource that expects real Web API responses.
            // Our emulator conversion is optimized for plugin interception and can return shapes that
            // don't match what DRB expects (e.g. metadata queries). Always proxy DRB Web API traffic.
            if (IsRestBuilderCaller(e.Request))
            {
                LogToFile("[WebApi] DRB proxy: " + e.Request.Method + " " + e.Request.Uri);
                var proxyResponse = await ProxyToDataverseAsync(e.Request);
                interceptedRequest.Proxied = true;
                e.Response = BuildWebViewResponse(environment, proxyResponse);
                this.Context.LastRequests.TriggerUpdateRequest(interceptedRequest);
                return true;
            }

            if (conversionResult.ConvertedRequest == null || !this.Context.IsEnabled)
            {
                var proxyResponse = await ProxyToDataverseAsync(e.Request);
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
                    var proxyResponse = await ProxyToDataverseAsync(e.Request);
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

        private async Task<WebApiResponse> ProxyToDataverseAsync(CoreWebView2WebResourceRequest request)
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
                    else if (string.Equals(current.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        // Always use the CrmServiceClient token (added above). Avoid forwarding an existing Authorization header
                        // from the page because duplicate/invalid auth headers can trigger 401s.
                    }
                    else if (string.Equals(current.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
                    {
                        // Do not forward cookies from the embedded browser; we rely on bearer token auth.
                    }
                    else if (string.Equals(current.Key, "Accept-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        // Avoid compressed responses. WebView2's WebResourceRequested synthetic responses
                        // don't reliably support transparent decompression, and some pages (like DRB) expect
                        // plain JSON/text bodies.
                    }
                    else if (string.Equals(current.Key, "Host", StringComparison.OrdinalIgnoreCase))
                    {
                        // HttpClient manages Host.
                    }
                    else if (string.Equals(current.Key, "Connection", StringComparison.OrdinalIgnoreCase))
                    {
                        // HttpClient manages Connection.
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

            var httpResponse = await this.Context.HttpClient.SendAsync(msg);
            var responseBody = await httpResponse.Content.ReadAsByteArrayAsync();

            var headers = new NameValueCollection();
            foreach (var h in httpResponse.Headers)
            {
                headers.Add(h.Key, string.Join(",", h.Value));
            }
            foreach (var h in httpResponse.Content.Headers)
            {
                headers.Add(h.Key, string.Join(",", h.Value));
            }

            // If the server still returned compressed content, decompress it so JS clients receive usable bodies.
            var contentEncoding = headers["Content-Encoding"];
            if (!string.IsNullOrEmpty(contentEncoding))
            {
                var enc = contentEncoding.ToLowerInvariant();
                try
                {
                    if (enc.Contains("gzip"))
                    {
                        responseBody = DecompressGzip(responseBody);
                        headers.Remove("Content-Encoding");
                        headers["Content-Length"] = responseBody.Length.ToString();
                    }
                    else if (enc.Contains("deflate"))
                    {
                        responseBody = DecompressDeflate(responseBody);
                        headers.Remove("Content-Encoding");
                        headers["Content-Length"] = responseBody.Length.ToString();
                    }
                }
                catch (Exception ex)
                {
                    LogToFile("[WebApi] decompress failed: " + ex);
                }
            }

            if ((int)httpResponse.StatusCode == 401)
            {
                try
                {
                    var token = this.Context?.CrmServiceClient?.CurrentAccessToken ?? string.Empty;
                    LogToFile("[WebApi] 401 from Dataverse. tokenEmpty=" + string.IsNullOrEmpty(token) + " tokenLen=" + token.Length + " uri=" + request?.Uri);
                }
                catch
                {
                    // ignore
                }
            }

            // Lightweight diagnostics for DRB init calls (File menu depends on these succeeding).
            if (request?.Uri != null)
            {
                var uri = request.Uri;
                if (uri.IndexOf("/api/data/v9.", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (uri.IndexOf("EntityDefinitions", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     uri.IndexOf("systemusers", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     uri.IndexOf("$batch", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    var ct = headers["Content-Type"] ?? string.Empty;
                    var ce = contentEncoding ?? string.Empty;
                    LogToFile("[WebApi] Resp " + (int)httpResponse.StatusCode + " " + request.Method + " " + uri + " ct=" + ct + " ce=" + ce);
                }
            }

            return new WebApiResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                Body = responseBody,
                Headers = headers
            };
        }

        private static bool IsRestBuilderCaller(CoreWebView2WebResourceRequest request)
        {
            if (request == null)
                return false;

            try
            {
                var referer = request.Headers.GetHeader("Referer") ?? string.Empty;
                if (string.IsNullOrEmpty(referer))
                    return false;

                // The DRB iframe is a fake webresource: ".../WebResources/dvbrowser_fakewr_/gp_/drb/..."
                if (referer.IndexOf(ExtensionsRequestHandler.FakeIdentifier, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;

                return referer.IndexOf("/gp_/drb/", StringComparison.OrdinalIgnoreCase) >= 0
                       || referer.IndexOf("gp_%2fdrb", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] DecompressGzip(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;
            using (var input = new MemoryStream(data))
            using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private static byte[] DecompressDeflate(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;
            using (var input = new MemoryStream(data))
            using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return output.ToArray();
            }
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
