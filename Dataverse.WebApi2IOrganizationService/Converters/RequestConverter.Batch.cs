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

            System.Diagnostics.Trace.WriteLine($"[Batch] Processing batch request with {originRequest.Body?.Length ?? 0} bytes");

            ExecuteMultipleRequest executeMultipleRequest = new ExecuteMultipleRequest()
            {
                Requests = new OrganizationRequestCollection()
            };

            // Check for odata.continue-on-error preference
            bool continueOnError = true; // Default to true for backward compatibility
            var preferHeader = originRequest.Headers?["Prefer"];
            if (preferHeader != null)
            {
                if (preferHeader.Contains("odata.continue-on-error=false"))
                {
                    continueOnError = false;
                }
            }

            List<RequestConversionResult> conversionResults = new List<RequestConversionResult>();
            MemoryStream dataStream = AddMissingLF(originRequest);
            using (var content = new StreamContent(dataStream))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                MultipartMemoryStreamProvider provider = content.ReadAsMultipartAsync().Result;

                foreach (var httpContent in provider.Contents)
                {
                    var data = httpContent.ReadAsByteArrayAsync().Result;
                    RequestConversionResult convertedRequest;
                    if (data.Length > 2 && data[0] == '-' && data[1] == '-')
                    {
                        // Contains a changeset = embedded ExecuteMultiple with transactional behavior
                        // Changesets ensure all-or-nothing execution
                        convertedRequest = CreateMultipleRequestFromMimeMessage(httpContent.Headers, data);
                        
                        // Mark the inner ExecuteMultiple to use transactional mode
                        if (convertedRequest.ConvertedRequest is ExecuteMultipleRequest innerExecuteMultiple)
                        {
                            // Changesets are transactional - if one fails, all fail
                            innerExecuteMultiple.Settings.ContinueOnError = false;
                        }
                    }
                    else
                    {
                        //contains a single request
                        WebApiRequest innerRequest;
                        try
                        {
                            innerRequest = CreateSimplifiedRequestFromMimeMessage(data);
                        }
                        catch (Exception ex)
                        {
                            conversionResult.ConvertFailureMessage = "One inner request could not be converted:" + ex.Message;
                            conversionResult.ConvertedRequest = null;
                            conversionResult.CustomData["InnerConversions"] = conversionResults;
                            return;
                        }
                        if (innerRequest == null)
                        {
                            conversionResult.ConvertFailureMessage = "One inner request could not be converted: not a Web API request";
                            conversionResult.ConvertedRequest = null;
                            conversionResult.CustomData["InnerConversions"] = conversionResults;
                            return;
                        }
                        convertedRequest = Convert(innerRequest);
                    }
                    conversionResults.Add(convertedRequest);
                    if (convertedRequest.ConvertedRequest == null)
                    {
                        conversionResult.ConvertFailureMessage = "One inner request could not be converted:" + convertedRequest.ConvertFailureMessage;
                        conversionResult.ConvertedRequest = null;
                        conversionResult.CustomData["InnerConversions"] = conversionResults;
                        return;
                    }
                    executeMultipleRequest.Requests.Add(convertedRequest.ConvertedRequest);

                }

            }
            executeMultipleRequest.Settings = new ExecuteMultipleSettings()
            {
                ContinueOnError = continueOnError,
                ReturnResponses = true
            };
            conversionResult.ConvertedRequest = executeMultipleRequest;
            conversionResult.CustomData["InnerConversions"] = conversionResults;
        }

        private RequestConversionResult CreateMultipleRequestFromMimeMessage(HttpContentHeaders headers, byte[] data)
        {
            NameValueCollection headersCollection = new NameValueCollection();
            foreach (var header in headers)
            {
                string headerValue = string.Join(", ", header.Value);
                headersCollection.Add(header.Key, headerValue);
            }
            WebApiRequest request = new WebApiRequest("POST", "/api/data/v9.2/$batch", headersCollection, Encoding.UTF8.GetString(data));
            return Convert(request);
        }

        private WebApiRequest CreateSimplifiedRequestFromMimeMessage(byte[] data)
        {
            string requestString = Encoding.ASCII.GetString(data);
            // Split the request string into lines
            string[] requestLines = requestString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // First line contains the request method, URL, and HTTP version
            string[] firstLineParts = requestLines[0].Split(' ');
            if (firstLineParts.Length < 2)
            {
                throw new NotSupportedException("Invalid batch item request line: " + requestLines[0]);
            }
            string method = firstLineParts[0];
            string url = firstLineParts[1];

            // Parse headers starting from the second line
            int bodyIndex = Array.IndexOf(requestLines, ""); // Find the index of the empty line that separates headers and body
            if (bodyIndex == -1)
            {
                throw new NotSupportedException("Batch part does not contain a blank line separating headers and body.");
            }
            NameValueCollection headers = new NameValueCollection();
            for (int i = 1; i < bodyIndex; i++)
            {
                var headerLine = requestLines[i];
                int colonIndex = headerLine.IndexOf(':');
                if (colonIndex <= 0)
                {
                    continue;
                }
                string headerName = headerLine.Substring(0, colonIndex).Trim();
                string headerValue = headerLine.Substring(colonIndex + 1).Trim();
                headers.Add(headerName, headerValue);
            }

            // Extract and display the body.
            // TODO: \r may have be stripped here
            string body = string.Join("\n", requestLines, bodyIndex + 1, requestLines.Length - bodyIndex - 1);
            // Use the helper to normalize absolute URLs into local Web API paths.
            return WebApiRequest.Create(method, url, headers, body);
        }

        private static MemoryStream AddMissingLF(WebApiRequest request)
        {
            if (request.Body == null)
                throw new NotSupportedException("Body is empty!");
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
