using System;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.WebApi2IOrganizationService.Converters
{
    public partial class RequestConverter
    {
        private OrganizationRequest ConvertToExecuteMultipleRequest(RequestConversionResult conversionResult)
        {
            var originRequest = conversionResult.SrcRequest;
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
                    var convertedRequest = Convert(innerRequest);
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

        private WebApiRequest CreateSimplifiedRequestFromMimeMessage(byte[] data)
        {
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
            var request = new WebApiRequest("GET", url, new NameValueCollection(), null);
            //TODO : body and headers
            return request;
        }

        private static MemoryStream AddMissingLF(WebApiRequest request)
        {
            // Les requêtes batch de CRM contiennent uniquement des LF en séparateurs de lignes et pas de CR
            var data = Encoding.UTF8.GetBytes(request.Body);
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
