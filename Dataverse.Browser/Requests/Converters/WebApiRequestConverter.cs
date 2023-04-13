using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CefSharp;
using CefSharp.DevTools.CSS;
using CefSharp.DevTools.IndexedDB;
using CefSharp.DevTools.Network;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using static System.Net.WebRequestMethods;

namespace Dataverse.Browser.Requests.Converter
{
    internal class WebApiRequestConverter

    {
        private DataverseContext Context { get; }

        public WebApiRequestConverter(DataverseContext context)
        {

            this.Context = context ?? throw new ArgumentNullException(nameof(context));

        }

        internal InterceptedWebApiRequest ConvertUnknowRequestToOrganizationRequest(IRequest request)
        {
            var url = new Uri(request.Url);
            var localPath = url.LocalPath;
            if (!localPath.StartsWith("/api/data/v9."))
                return null;
            SimpleHttpRequest simplifiedRequest;
            try
            {
                simplifiedRequest = new SimpleHttpRequest(request, localPath);
            }
            catch (Exception ex)
            {
                return new InterceptedWebApiRequest()
                {
                    Url = localPath,
                    ConvertFailureMessage = ex.Message,
                    ExecuteException = ex
                };
            }
            return ConvertDataApiSimplifiedRequestToOrganizationRequest(simplifiedRequest);
        }

        private InterceptedWebApiRequest ConvertUnknowSimplifiedRequestToOrganizationRequest(SimpleHttpRequest request)
        {
            if (!request.LocalPath.StartsWith("/api/data/v9."))
                return null;
            return ConvertDataApiSimplifiedRequestToOrganizationRequest(request);
        }

        private InterceptedWebApiRequest ConvertDataApiSimplifiedRequestToOrganizationRequest(SimpleHttpRequest request)
        {
            InterceptedWebApiRequest webApiRequest = new InterceptedWebApiRequest()
            {
                Url = request.LocalPath,
                Method = request.Method
            };
            ODataUriParser parser;
            ODataPath path;
            try
            {
                parser = new ODataUriParser(this.Context.Model, new Uri(request.LocalPath.Substring(15), UriKind.Relative));
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
                            if (request.OriginRequest == null)
                            {
                                throw new NotSupportedException("batch requests embedded in another batch request are not supported!");
                            }
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
                    case "GETX":
                        switch (path.Count)
                        {
                            case 1:
                                throw new NotImplementedException("Retrievemultiple are not implemented");
                            case 2:
                                ConvertToRetrieveRequest(request, webApiRequest, path);
                                break;
                            default:
                                throw new NotSupportedException("Unexpected number of segments:" + path.Count);
                        }
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

        private void ConvertToRetrieveRequest(SimpleHttpRequest request, InterceptedWebApiRequest webApiRequest, ODataPath path)
        {
            var entitySegment = path.FirstSegment as EntitySetSegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var keySegment = path.LastSegment as KeySegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var entity = this.Context.MetadataCache.GetEntityFromSetName(entitySegment.Identifier);
            if (entity == null)
            {
                throw new ApplicationException("Entity not found: " + entity);
            }
            var id = GetIdFromKeySegment(keySegment);

            RetrieveRequest retrieveRequest = new RetrieveRequest
            {
                Target = new EntityReference(entity.LogicalName, id),
                ColumnSet = GetColumnSet(request, entity)
            };
            webApiRequest.ConvertedRequest = retrieveRequest;
        }

        private ColumnSet GetColumnSet(SimpleHttpRequest request, EntityMetadata entity)
        {
            int index = request.LocalPath.IndexOf("?");
            if (index == -1 || index == request.LocalPath.Length - 1)
                return new ColumnSet(true);
            var parsedQuery = new FormDataCollection(request.LocalPath.Substring(index + 1));
            string[] columnList = null;
            foreach (var kvp in parsedQuery)
            {
                if (kvp.Key != "$select")
                    throw new NotImplementedException("Query not supported:" + kvp.Key);
                columnList = kvp.Value.Split(',');
            }
            if (columnList == null)
            {
                return new ColumnSet(true);
            }
            var columnSet = new ColumnSet();
            var entityMetadata = this.Context.MetadataCache.GetEntityMetadataWithAttributes(entity.LogicalName);
            foreach (var column in columnList)
            {
                var attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == column) ?? throw new ApplicationException("attribute not found:" + column);
                columnSet.AddColumn(attributeMetadata.LogicalName);
            }
            return columnSet;
        }

        private void ConvertToCreateUpdateRequest(SimpleHttpRequest request, InterceptedWebApiRequest webApiRequest, ODataPath path)
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

        private OrganizationRequest ConvertToExecuteMultipleRequest(SimpleHttpRequest request, InterceptedWebApiRequest webApiRequest)
        {
            var originRequest = request.OriginRequest;
            string contentType = originRequest.Headers["Content-Type"];
            if (!contentType.StartsWith("multipart/mixed;"))
            {
                throw new NotImplementedException("ContentType " + contentType + " is not supported for batch requests");
            }

            ExecuteMultipleRequest executeMultipleRequest = new ExecuteMultipleRequest();

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
                    if (convertedRequest.ConvertedRequest != null)
                    {
                        executeMultipleRequest.Requests.Add(convertedRequest.ConvertedRequest);
                    }
                    else
                    {
                        webApiRequest.ConvertFailureMessage = convertedRequest.ConvertFailureMessage;
                        throw new NotSupportedException("One inner request could not be converted");
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
            request.LocalPath = url;
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

        private OrganizationRequest ConvertToCreateUpdateRequest(KeySegment keySegment, SimpleHttpRequest request, InterceptedWebApiRequest webApiRequest, string entityLogicalName)
        {
            string body = request.Body ?? throw new NotSupportedException("A body was expected!");
            webApiRequest.Body = body;
            return ConvertToCreateUpdateRequest(keySegment, body, entityLogicalName);
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
                record.Id = GetIdFromKeySegment(keySegment); ;
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

        private static Guid GetIdFromKeySegment(KeySegment keySegment)
        {
            if (keySegment.Keys.Count() != 1)
            {
                throw new NotImplementedException("Alternate key not supported");
            }
            var key = keySegment.Keys.First();
            if (!(key.Value is Guid))
            {
                throw new NotImplementedException("Alternate key not supported");
            }
            var id = (Guid)key.Value;
            return id;
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
