using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CefSharp;
using CefSharp.DevTools.CSS;
using CefSharp.DevTools.Network;
using Dataverse.Browser.Context;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;

namespace Dataverse.Browser.Requests
{
    internal class WebApiRequestConverter

    {
        private DataverseContext Context { get; }

        public WebApiRequestConverter(DataverseContext context)
        {

            this.Context = context ?? throw new ArgumentNullException(nameof(context));

        }

        internal InterceptedWebApiRequest ConvertToOrganizationRequest(IRequest request)
        {
            var url = new Uri(request.Url);
            var localPath = url.LocalPath;
            if (!localPath.StartsWith("/api/data/v9."))
                return null;
            InterceptedWebApiRequest webApiRequest = new InterceptedWebApiRequest()
            {
                Url = localPath,
                Method = request.Method
            };
            ODataUriParser parser;
            ODataPath path;
            try
            {
                parser = new ODataUriParser(this.Context.Model, new Uri(localPath.Substring(15), UriKind.Relative));
                path = parser.ParsePath();
            }
            catch (Exception ex)
            {
                webApiRequest.ConvertFailureMessage = "Unable to parse: " + ex.Message;
                return webApiRequest;
            }


            try
            {
                switch (request.Method)
                {
                    case "POST":
                        if (path.Count != 1)
                        {
                            throw new NotImplementedException("POST is not implemented for: " + path.Count + " segments");
                        }
                        if (path.FirstSegment.EdmType?.TypeKind == EdmTypeKind.Collection)
                        {
                            ConvertToCreateUpdateRequest(request, webApiRequest, path);
                        }
                        else if (path.FirstSegment.Identifier == "$batch")
                        {
                            ConvertToExecuteMultipleRequest(request, webApiRequest);
                        }
                        else
                        {
                            throw new NotImplementedException("POST is not implemented for: " + path.FirstSegment.EdmType?.TypeKind);
                        }
                        break;
                    case "PATCH":
                        if (path.Count != 2)
                        {
                            throw new NotImplementedException("PATCH is not implemented for: " + path.Count + " segments");
                        }
                        if (path.FirstSegment.EdmType?.TypeKind != EdmTypeKind.Collection)
                        {
                            throw new NotImplementedException("PATCH is not implemented for: " + path.FirstSegment.EdmType?.TypeKind);
                        }
                        ConvertToCreateUpdateRequest(request, webApiRequest, path);
                        break;
                    default:
                        webApiRequest.ConvertFailureMessage = "method not implemented";
                        break;
                }
            }
            catch (Exception ex)
            {
                webApiRequest.ConvertFailureMessage = ex.Message;
            }
            return webApiRequest;
        }

        private void ConvertToCreateUpdateRequest(IRequest request, InterceptedWebApiRequest webApiRequest, ODataPath path)
        {
            var entity = this.Context.MetadataCache.GetEntityFromSetName(path.FirstSegment.Identifier);
            if (entity == null)
            {
                throw new ApplicationException("Entity not found: " + entity);
            }
            KeySegment keySegment = null;
            if (request.Method == "PATCH")
            {
                keySegment = path.LastSegment as KeySegment;

            }
            webApiRequest.ConvertedRequest = ConvertToCreateUpdateRequest(keySegment, request, webApiRequest, entity.LogicalName);
        }

        private OrganizationRequest ConvertToExecuteMultipleRequest(IRequest request, InterceptedWebApiRequest webApiRequest)
        {
            ExtractRequestBody(request, webApiRequest);
            string contentType = request.Headers["Content-Type"];
            if (!contentType.StartsWith("multipart/mixed;"))
            {
                throw new NotImplementedException("ContentType " + contentType + " is not supported for batch requests");
            }


            MemoryStream dataStream = AddMissingLF(request);
            using (var content = new StreamContent(dataStream))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                MultipartMemoryStreamProvider provider = content.ReadAsMultipartAsync().Result;

                //TODO changesets
                foreach (var httpContent in provider.Contents)
                {
                    var data = httpContent.ReadAsByteArrayAsync().Result;
                    IRequest webRequest = CreateWebRequestFromMimeMessage(data);
                }

            }
            throw new NotImplementedException("Batch requests are not implemented");
        }

        private IRequest CreateWebRequestFromMimeMessage(byte[] data)
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
            //TODO : simplified request ?
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

        private OrganizationRequest ConvertToCreateUpdateRequest(KeySegment keySegment, IRequest request, InterceptedWebApiRequest webApiRequest, string entityLogicalName)
        {
            string body = ExtractRequestBody(request, webApiRequest);
            return ConvertToCreateUpdateRequest(keySegment, body, entityLogicalName);
        }

        private static string ExtractRequestBody(IRequest request, InterceptedWebApiRequest webApiRequest)
        {
            if (request.PostData.Elements.Count != 1)
            {
                throw new ApplicationException("Unable to parse body");
            }
            var postDataElement = request.PostData.Elements.FirstOrDefault();
            if (postDataElement.Type != PostDataElementType.Bytes)
            {
                throw new ApplicationException("Unknown body type");
            }
            //TODO encoding
            var body = Encoding.UTF8.GetString(postDataElement.Bytes);
            webApiRequest.Body = body;
            return body;
        }

        private OrganizationRequest ConvertToCreateUpdateRequest(KeySegment keySegment, string body, string entityLogicalName)
        {
            var entityMetadata = this.Context.MetadataCache.GetEntityMetadataWithAttributes(entityLogicalName);
            Entity record = new Entity(entityLogicalName);
            OrganizationRequest request;
            if (keySegment == null)
            {
                request = new CreateRequest()
                {
                    Target = record
                };
            }
            else
            {
                request = new UpdateRequest()
                {
                    Target = record
                };
                if (keySegment.Keys.Count() != 1)
                {
                    throw new NotImplementedException("Alternate key not supported");
                }
                var key = keySegment.Keys.First();
                if (!(key.Value is Guid))
                {
                    throw new NotImplementedException("Alternate key not supported");
                }
                record.Id = (Guid)key.Value;
            }

            using (JsonDocument json = JsonDocument.Parse(body))
            {
                foreach (var node in json.RootElement.EnumerateObject())
                {
                    string key = node.Name;
                    if (key.EndsWith("@OData.Community.Display.V1.FormattedValue"))
                        continue;

                    if (key.EndsWith("@odata.bind"))
                    {
                        key = key.Substring(0, key.Length - "@odata.bind".Length).ToLowerInvariant();
                        var relation = entityMetadata.ManyToOneRelationships.FirstOrDefault(r => r.ReferencingEntityNavigationPropertyName == key);
                        if (relation != null)
                        {
                            key = relation.ReferencingAttribute;
                        }
                    }
                    else if (key.Contains("@"))
                    {
                        throw new ApplicationException("Unknow property key:" + key);
                    }
                    object value = ConvertValueToAttribute(entityLogicalName, key, node.Value);
                    record.Attributes.Add(key, value);
                }
            }

            return request;
        }

        private object ConvertValueToAttribute(string entityLogicalName, string key, JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }
            var entityMetadata = this.Context.MetadataCache.GetEntityMetadataWithAttributes(entityLogicalName);
            var attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == key) ?? throw new ApplicationException("attribute not found:" + key);
            switch (attributeMetadata.AttributeType)
            {
                case AttributeTypeCode.BigInt:
                    return value.GetInt64();
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    Regex targetRegex = new Regex(@"/?(?<entityset>.*)\((?<id>.*)\)");
                    var result = targetRegex.Match(value.GetString());
                    if (!result.Success)
                        return null;
                    var entitySet = result.Groups["entityset"].Value;
                    var id = result.Groups["id"].Value;
                    var lookupMetadata = (LookupAttributeMetadata)attributeMetadata;
                    var targetEntity = this.Context.MetadataCache.GetEntityFromSetName(entitySet) ?? throw new ApplicationException("Target entity not found: " + entitySet);
                    if (!lookupMetadata.Targets.Contains(targetEntity.LogicalName))
                    {
                        throw new ApplicationException("Target entity not matching allowed targets: " + entitySet);
                    }
                    if (!Guid.TryParse(id, out var recordId))
                    {
                        //TODO alternate keys
                        throw new ApplicationException("Invalid guid: " + id);
                    }
                    return new EntityReference(targetEntity.LogicalName, recordId);
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return value.GetString();
                case AttributeTypeCode.Boolean:
                    return value.GetBoolean();
                case AttributeTypeCode.DateTime:
                    return value.GetDateTime();
                case AttributeTypeCode.Decimal:
                    return value.GetDecimal();
                case AttributeTypeCode.Money:
                    return new Money(value.GetDecimal());
                case AttributeTypeCode.Double:
                    return value.GetDouble();
                case AttributeTypeCode.Integer:
                    return value.GetInt32();
                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    return new OptionSetValue(value.GetInt32());
                case AttributeTypeCode.Uniqueidentifier:
                    return value.GetGuid();
                default:
                    throw new ApplicationException("Unsupported type:" + attributeMetadata.AttributeType);
            }
        }



    }
}
