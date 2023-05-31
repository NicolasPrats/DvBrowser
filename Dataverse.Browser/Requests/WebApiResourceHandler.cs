using System;
using System.IO;
using CefSharp;
using CefSharp.Callback;
using Dataverse.Browser.Context;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Dataverse.WebApi2IOrganizationService.Converters;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Browser.Requests
{
    internal class WebApiResourceHandler
            : IResourceHandler
    {

        private bool IsAlreadyExecuted { get; set; }
        private Exception ExecuteException { get; set; }
        private WebApiResponse HttpResponse { get; set; }

        private int TotalBytesRead { get; set; }

        private BrowserContext Context { set; get; }
        private InterceptedWebApiRequest InterceptedWebApiRequest { set; get; }
        public ResponseConverter ResponseConverter { get; }

        public WebApiResourceHandler(BrowserContext context, InterceptedWebApiRequest webApiRequest = null)
        {
            this.Context = context;
            this.InterceptedWebApiRequest = webApiRequest;
            this.ResponseConverter = new ResponseConverter(this.Context);
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
                this.HttpResponse = this.ResponseConverter.Convert(this.ExecuteException);
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
            int bytesToRead = this.HttpResponse.Body.Length - this.TotalBytesRead;
            if (bytesToRead > dataOut.Length)
                bytesToRead = (int)dataOut.Length;
            dataOut.Write(this.HttpResponse.Body, this.TotalBytesRead, bytesToRead);
            this.TotalBytesRead += bytesToRead;
            if (this.TotalBytesRead == this.HttpResponse.Body.Length)
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
                OrganizationResponse response = ExecuteWithTree();
                this.HttpResponse = this.ResponseConverter.Convert(this.InterceptedWebApiRequest.ConversionResult, response);
                //TODO : si le payload est trop petit, il n'est pas chargé en entier quand status code != 200
                //problème de flush ? de header ?
                // quand le souci sera réglé, penser à repasser le set Body en internal
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
                this.InterceptedWebApiRequest.ExecuteException = ex;
                this.Context.LastRequests.TriggerUpdateRequest(this.InterceptedWebApiRequest);
            }

        }

        protected OrganizationResponse ExecuteWithTree()
        {
            this.InterceptedWebApiRequest.ExecutionTreeRoot = new ExecutionTreeNode();
            var response = this.Context.ProxyWithEmulator.ExecuteWithTree(this.InterceptedWebApiRequest.ConversionResult.ConvertedRequest, this.InterceptedWebApiRequest.ExecutionTreeRoot);
            this.Context.LastRequests.TriggerUpdateRequest(this.InterceptedWebApiRequest);
            return response;
        }


    }
}
