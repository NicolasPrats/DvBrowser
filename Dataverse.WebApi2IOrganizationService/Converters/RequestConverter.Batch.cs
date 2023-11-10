using System;
using System.Collections.Generic;
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
        private void ConvertToExecuteMultipleRequest(RequestConversionResult conversionResult)
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

            List<RequestConversionResult> conversionResults = new List<RequestConversionResult>();
            MemoryStream dataStream = AddMissingLF(originRequest);
            using (var content = new StreamContent(dataStream))
            {
                //TODO support des changesets
                //TODO support des continue on error
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                MultipartMemoryStreamProvider provider = content.ReadAsMultipartAsync().Result;

                foreach (var httpContent in provider.Contents)
                {
                    var data = httpContent.ReadAsByteArrayAsync().Result;
                    var innerRequest = CreateSimplifiedRequestFromMimeMessage(data);
                    var convertedRequest = Convert(innerRequest) ?? throw new NotSupportedException("Only web api requests are supported!");
                    conversionResults.Add(convertedRequest);
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
            executeMultipleRequest.Settings = new ExecuteMultipleSettings()
            {
                ContinueOnError = true,
                ReturnResponses = true
            };
            conversionResult.ConvertedRequest = executeMultipleRequest;
            conversionResult.CustomData["InnerConversions"] = conversionResults;
        }

        private WebApiRequest CreateSimplifiedRequestFromMimeMessage(byte[] data)
        {
            string requestString = Encoding.ASCII.GetString(data);
            // Split the request string into lines
            string[] requestLines = requestString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // First line contains the request method, URL, and HTTP version
            string[] firstLineParts = requestLines[0].Split(' ');
            string method = firstLineParts[0];
            string url = firstLineParts[1];

            // Parse headers starting from the second line
            int bodyIndex = Array.IndexOf(requestLines, ""); // Find the index of the empty line that separates headers and body
            NameValueCollection headers = new NameValueCollection();
            for (int i = 1; i < bodyIndex; i++)
            {
                string[] headerParts = requestLines[i].Split(':');
                string headerName = headerParts[0].Trim();
                string headerValue = headerParts[1].Trim();
                headers.Add(headerName, headerValue);
            }

            // Extract and display the body.
            // TODO: \r may have be stripped here
            string body = string.Join("\n", requestLines, bodyIndex + 1, requestLines.Length - bodyIndex - 1);
            var request = new WebApiRequest(method, url, headers, body);
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
