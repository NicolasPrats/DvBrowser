using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using CefSharp.Callback;
using CefSharp;
using Microsoft.Xrm.Sdk;
using System.Text.Encodings.Web;
using System.Text;
using Dataverse.Browser.Context;
using Dataverse.Plugin.Emulator.ExecutionTree;

namespace Dataverse.Browser.Requests
{
    internal abstract class BaseResourceHandler<T>
            : IResourceHandler
         where T : OrganizationRequest
    {

        private bool IsAlreadyExecuted { get; set; }
        private Exception ExecuteException { get; set; }
        protected byte[] ResultBody { get; set; }
        protected NameValueCollection ResultHeaders { get; set; }
        protected int ResultStatusCode { get; set; }

        private int TotalBytesRead { get; set; }

        public DataverseContext Context { set; get; }
        public T Request { set; get; }
        public InterceptedWebApiRequest WebApiRequest { set; get; }


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
                //TODO faire en sorte que l'erreur soit bien remontée dans le navigateur
                var errorText = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(this.ExecuteException.Message);
                var errorDetails = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(this.ExecuteException.ToString());
                this.ResultStatusCode = 400;
                this.ResultBody = Encoding.UTF8.GetBytes(
 $@"{{
                ""error"":
                    {{
                    ""code"":""0x80040265"",
                    ""message"":""{errorText}"",
                    ""@Microsoft.PowerApps.CDS.ErrorDetails.HttpStatusCode"":""400"",
                    ""@Microsoft.PowerApps.CDS.InnerError"":""{errorText}"",
                    ""@Microsoft.PowerApps.CDS.TraceText"":""{errorDetails}""
                    }}
                }}{new string(' ' , 100000)}");
                //TODO : si le payload est trop petit, il n'est pas chargé en entier quand status code != 200
                //problème de flush ? de header ?


                this.ResultHeaders = new NameValueCollection()
                {
                    { "OData-Version", "4.0" },
                    { "Content-Type", "application/json; odata.metadata = minimal" },
                    { "Content-Length", this.ResultBody.Length.ToString() }
                };
                response.StatusText = "Error";
            }
            var headers = this.ResultHeaders;
            if (headers == null)
            {
                responseLength = -1;
                redirectUrl = null;
                return;
            }
            response.StatusCode = this.ResultStatusCode;
            response.Headers = headers;
            responseLength = this.ResultBody.Length;
            redirectUrl = null;
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
            int bytesToRead = ResultBody.Length - TotalBytesRead;
            if (bytesToRead > dataOut.Length)
                bytesToRead = (int)dataOut.Length;
            dataOut.Write(ResultBody, TotalBytesRead, bytesToRead);
            TotalBytesRead += bytesToRead;
            if (TotalBytesRead == ResultBody.Length)
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
                this.Execute();
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
            var response = this.Context.ProxyForWeb.ExecuteWithTree(this.Request, this.WebApiRequest.ExecutionTreeRoot);
            this.Context.LastRequests.TriggerUpdateRequest(this.WebApiRequest);
            return response;
        }

        protected abstract void Execute();
    }
}
