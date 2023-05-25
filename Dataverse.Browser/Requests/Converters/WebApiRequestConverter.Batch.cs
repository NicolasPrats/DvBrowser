using System;
using System.Activities.DurableInstancing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.DevTools.DOM;
using Dataverse.Browser.Constants;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Dataverse.Browser.Requests.Converter
{
    internal partial class WebApiRequestConverter

    {
       

        private OrganizationRequest ConvertToExecuteMultipleRequest(InterceptedWebApiRequest webApiRequest)
        {
            var originRequest = webApiRequest.SimpleHttpRequest.OriginRequest;
            string contentType = originRequest.Headers["Content-Type"];
            if (!contentType.StartsWith("multipart/mixed;"))
            {
                throw new NotImplementedException("ContentType " + contentType + " is not supported for batch requests");
            }

            ExecuteMultipleRequest executeMultipleRequest = new ExecuteMultipleRequest()
            {
                Requests = new OrganizationRequestCollection()
            };

            MemoryStream dataStream = AddMissingLF(originRequest);
            using (var content = new StreamContent(dataStream))
            {
                //TODO support des changesets
                //TODO support des continue on error
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                MultipartMemoryStreamProvider provider = content.ReadAsMultipartAsync().Result;

                //TODO changesets
                foreach (var httpContent in provider.Contents)
                {
                    var data = httpContent.ReadAsByteArrayAsync().Result;
                    var innerRequest = CreateSimplifiedRequestFromMimeMessage(data);
                    var convertedRequest = this.ConvertUnknowSimplifiedRequestToOrganizationRequest(innerRequest);
                    if (convertedRequest == null)
                    {
                        throw new NotSupportedException("Only web api requests are supported!");
                    }
                    else
                    if (convertedRequest.ConvertedRequest != null)
                    {
                        executeMultipleRequest.Requests.Add(convertedRequest.ConvertedRequest);
                    }
                    else
                    {
                        throw new NotSupportedException("One inner request could not be converted:" + convertedRequest.ConvertFailureMessage);
                    }
                }

            }
            return executeMultipleRequest;
        }

        private SimpleHttpRequest CreateSimplifiedRequestFromMimeMessage(byte[] data)
        {
            var request = new SimpleHttpRequest();
            int index = Array.FindIndex(data, b => b == (byte)'\r');
            if (index == -1)
            {
                throw new NotSupportedException("Unable to parse data, no \\r found!");
            }
            string firstLine = Encoding.UTF8.GetString(data, 0, index);
            if (!firstLine.StartsWith("GET"))
            {
                throw new ApplicationException("Unable to parse first line: " + firstLine);

            }
            string url = firstLine.Substring(4);
            if (url.EndsWith("HTTP/1.1"))
            {
                url = url.Substring(0, url.Length - 8);
            }
            request.Method = "GET";
            request.LocalPathWithQuery = url;
            //TODO : body and headers
            return request;
        }

        private static MemoryStream AddMissingLF(IRequest request)
        {
            // Les requêtes batch de CRM contiennent uniquement des LF en séparateurs de lignes et pas de CR
            var data = request.PostData.Elements.FirstOrDefault().Bytes;
            MemoryStream dataStream = new MemoryStream();
            bool previousIsCr = false;
            for (int i = 0; i < data.Length; i++)
            {
                var value = data[i];
                if (value == '\r')
                {
                    previousIsCr = true;
                }
                else
                {
                    if (value == '\n' && !previousIsCr)
                    {
                        dataStream.WriteByte((byte)'\r');
                    }
                    previousIsCr = false;
                }
                dataStream.WriteByte(value);
            }
            dataStream.Seek(0, SeekOrigin.Begin);
            return dataStream;
        }

    }
}
