using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using CefSharp;
using CefSharp.Callback;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.Converters;
using Dataverse.Browser.Requests.SimpleClasses;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    internal class WebApiResourceHandler
            : IResourceHandler
    {

        private bool IsAlreadyExecuted { get; set; }
        private Exception ExecuteException { get; set; }
        private SimpleHttpResponse HttpResponse { get; set; }

        private int TotalBytesRead { get; set; }

        private DataverseContext Context { set; get; }
        private InterceptedWebApiRequest WebApiRequest { set; get; }

        public WebApiResourceHandler(DataverseContext context, InterceptedWebApiRequest webApiRequest = null)
        {
            this.Context = context;
            this.WebApiRequest = webApiRequest;
        }

        public void Cancel()
        {
            //
        }

        public void Dispose()
        {
            //
        }

        public void GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl)
        {
            if (this.ExecuteException != null)
            {
                GenerateHttpResponseError();
            }
            var headers = this.HttpResponse.Headers;
            if (headers == null)
            {
                responseLength = -1;
                redirectUrl = null;
                return;
            }
            response.StatusCode = this.HttpResponse.StatusCode;
            response.Headers = headers;
            responseLength = this.HttpResponse.Body.Length;
            redirectUrl = null;
        }

        private void GenerateHttpResponseError()
        {
            var errorText = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(this.ExecuteException.Message);
            var errorDetails = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(this.ExecuteException.ToString());

            byte[] body = Encoding.UTF8.GetBytes(
$@"{{
                ""error"":
                    {{
                    ""code"":""0x80040265"",
                    ""message"":""{errorText}"",
                    ""@Microsoft.PowerApps.CDS.ErrorDetails.HttpStatusCode"":""400"",
                    ""@Microsoft.PowerApps.CDS.InnerError"":""{errorText}"",
                    ""@Microsoft.PowerApps.CDS.TraceText"":""{errorDetails}""
                    }}
                }}{new string(' ', 100000)}");
            //TODO : si le payload est trop petit, il n'est pas chargé en entier quand status code != 200
            //problème de flush ? de header ?
           
            this.HttpResponse = new SimpleHttpResponse()
            {
                StatusCode = 400,
                Body = body,
                Headers = new NameValueCollection()
                {
                    { "OData-Version", "4.0" },
                    { "Content-Type", "application/json; odata.metadata = minimal" },
                    { "Content-Length", body.Length.ToString() }
                }
            };
            //response.StatusText = "Error";
        }

        public bool Open(IRequest request, out bool handleRequest, ICallback callback)
        {
            InnerExecute();
            handleRequest = true;
            return true;
        }

        public bool ProcessRequest(IRequest request, ICallback callback)
        {
            InnerExecute();
            callback.Continue();
            return true;
        }

        public bool Read(Stream dataOut, out int bytesRead, IResourceReadCallback callback)
        {
            callback?.Dispose();
            InnerExecute();
            int bytesToRead = this.HttpResponse.Body.Length - TotalBytesRead;
            if (bytesToRead > dataOut.Length)
                bytesToRead = (int)dataOut.Length;
            dataOut.Write(this.HttpResponse.Body, TotalBytesRead, bytesToRead);
            TotalBytesRead += bytesToRead;
            if (TotalBytesRead == this.HttpResponse.Body.Length)
            {
                dataOut.Flush();
                bytesRead = 0;
                return false;
            }
            bytesRead = bytesToRead;
            return true;
        }

        public bool ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            throw new NotImplementedException();
        }

        public bool Skip(long bytesToSkip, out long bytesSkipped, IResourceSkipCallback callback)
        {
            //
            throw new NotImplementedException();
        }

        protected void InnerExecute()
        {
            if (this.IsAlreadyExecuted)
            {
                return;
            }
            this.IsAlreadyExecuted = true;
            try
            {
                OrganizationResponse response = this.ExecuteWithTree();
                this.HttpResponse = OrganizationResponseConverter.Convert(this.Context, this.WebApiRequest, response);
                //TODO : si le payload est trop petit, il n'est pas chargé en entier quand status code != 200
                //problème de flush ? de header ?
                if (this.HttpResponse.Body != null && this.HttpResponse.Body.Length > 0 && this.HttpResponse.Body.Length < 100000)
                {
                    var newBody = new byte[100000];
                    Array.Copy(this.HttpResponse.Body, newBody, this.HttpResponse.Body.Length);
                    for (int i = this.HttpResponse.Body.Length; i < newBody.Length; i++)
                    {
                        newBody[i] = (byte)' ';
                    }
                    this.HttpResponse.Body = newBody;
                }
            }
            catch (Exception ex)
            {
                this.ExecuteException = ex;
                this.WebApiRequest.ExecuteException = ex;
                this.Context.LastRequests.TriggerUpdateRequest(this.WebApiRequest);
            }

        }

        protected OrganizationResponse ExecuteWithTree()
        {
            this.WebApiRequest.ExecutionTreeRoot = new ExecutionTreeNode();
            var response = this.Context.ProxyForWeb.ExecuteWithTree(this.WebApiRequest.ConvertedRequest, this.WebApiRequest.ExecutionTreeRoot);
            this.Context.LastRequests.TriggerUpdateRequest(this.WebApiRequest);
            return response;
        }


    }
}
