﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Dataverse.Utils;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.WebApi2IOrganizationService.Converters
{
    public class ResponseConverter
    {
        internal DataverseContext Context { get; }

        public ResponseConverter(DataverseContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private static WebApiResponse ConvertError(int errorCode, string errorText, string errorDetails)
        {
            byte[] body = Encoding.UTF8.GetBytes(
            $@"{{
                ""error"":
                    {{
                    ""code"":""0x{errorCode:X}"",
                    ""message"":""{errorText}""
                    //""@Microsoft.PowerApps.CDS.ErrorDetails.HttpStatusCode"":""400"",
                    //""@Microsoft.PowerApps.CDS.InnerError"":""{errorText}"",
                    //""@Microsoft.PowerApps.CDS.TraceText"":""{errorDetails}""
                    }}
                }}");

            return new WebApiResponse()
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
        }

        public WebApiResponse Convert(Exception ex)
        {
            var errorText = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(ex.Message);
            var errorDetails = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(ex.ToString());
            return ConvertError(-2147220891, errorText, errorDetails); //ISVAborted
        }


        public WebApiResponse Convert(RequestConversionResult conversionResult, OrganizationResponse response)
        {
            switch (response)
            {
                case CreateResponse createResponse:
                    return ConvertCreateResponse((CreateRequest)conversionResult.ConvertedRequest, createResponse);
                case UpdateResponse _:
                    return ConvertUpdateResponse((UpdateRequest)conversionResult.ConvertedRequest);
                case RetrieveResponse _:
                    return ConvertRetrieveResponse(conversionResult);
                case DeleteResponse _:
                    return ConvertDeleteResponse();
                case ExecuteMultipleResponse executeMultipleResponse:
                    return ConvertExecuteMultipleResponse(conversionResult, executeMultipleResponse);
                default:
                    //if (response.GetType() != typeof(OrganizationResponse))
                    //{
                    //    throw new NotImplementedException("Message has been executed but response is not implemented:" + response.GetType().Name);
                    //}
                    //OrganizationResponse without specialized type are assumed to be CustomApi
                    return ConvertCustomApiResponse(response);

            }
        }

        private WebApiResponse ConvertExecuteMultipleResponse(RequestConversionResult conversionResult, ExecuteMultipleResponse executeMultipleResponse)
        {
            var innerConversionResults = conversionResult.CustomData["InnerConversions"] as List<RequestConversionResult>;
            StringBuilder bodyBuilder = new StringBuilder();
            if (innerConversionResults.Count != executeMultipleResponse.Responses.Count)
                throw new NotSupportedException($"Expected same number of responses: {innerConversionResults.Count} != {executeMultipleResponse.Responses.Count}");
            string delimiter = "batchresponse_dvbrowser" + Guid.NewGuid();
            for (int i = 0; i < innerConversionResults.Count; i++)
            {
                var requestConversionResult = innerConversionResults[i];
                var responseItem = executeMultipleResponse.Responses[i];
                if (responseItem.Fault == null && responseItem.Response == null)
                {
                    continue;
                }
                WebApiResponse convertedResponse;
                if (responseItem.Fault != null)
                {
                    convertedResponse = ConvertError(responseItem.Fault.ErrorCode, responseItem.Fault.Message, responseItem.Fault.ToString());
                }
                else
                {
                    convertedResponse = Convert(requestConversionResult, responseItem.Response);
                }
                bodyBuilder.Append("--").AppendLine(delimiter);

                if (!(responseItem.Response is ExecuteMultipleResponse))
                {

                    bodyBuilder.AppendLine("Content-Type: application/http");
                    bodyBuilder.AppendLine("Content-Transfer-Encoding: binary");
                    bodyBuilder.AppendLine("Content-ID: " + (i + 1));
                    bodyBuilder.AppendLine();
                    bodyBuilder.Append("HTTP/1.1 ").Append(convertedResponse.StatusCode).Append(" N/A").AppendLine();
                    foreach (string header in convertedResponse.Headers.Keys)
                    {
                        bodyBuilder.Append(header).Append(": ").Append(convertedResponse.Headers[header]).AppendLine();
                    }
                }
                else
                {
                    bodyBuilder.Append("Content-Type").Append(": ").Append(convertedResponse.Headers["Content-Type"]).AppendLine();
                }

                bodyBuilder.AppendLine();
                if (convertedResponse.Body != null)
                {
                    bodyBuilder.AppendLine(Encoding.UTF8.GetString(convertedResponse.Body));
                }
                bodyBuilder.AppendLine();
            }
            bodyBuilder.Append("--").Append(delimiter).Append("--");

            return new WebApiResponse()
            {
                Body = Encoding.UTF8.GetBytes(bodyBuilder.ToString()),
                StatusCode = 200,
                Headers = new NameValueCollection() { { "OData-Version", "4.0" }, { "Content-Type", $"multipart/mixed; boundary={delimiter}" } }
            };
        }

        private WebApiResponse ConvertCustomApiResponse(OrganizationResponse organizationResponse)
        {

            if (organizationResponse.Results.Count == 0)
            {
                return new WebApiResponse()
                {
                    Body = null,
                    Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                    StatusCode = 204
                };
            }

            var body = new JsonObject
            {
                ["@odata.context"] = $"https://{this.Context.Host}/api/data/v9.2/$metadata#Microsoft.Dynamics.CRM.{organizationResponse.ResponseName}Response"
            };
            var operation = this.Context.Model.FindDeclaredOperations("Microsoft.Dynamics.CRM." + organizationResponse.ResponseName).Single();
            var returnTypeDefinition = operation.ReturnType.Definition as IEdmStructuredType;

            bool bodyIsEmpty = true;
            foreach (var property in organizationResponse.Results)
            {
                var parameter = returnTypeDefinition?.DeclaredProperties?.FirstOrDefault(p => p.Name == property.Key);
                if (parameter != null || returnTypeDefinition?.FullTypeName() == "Microsoft.Dynamics.CRM.crmbaseentity")
                {
                    AddValueToJsonObject(body, property, null, parameter);
                    bodyIsEmpty = false;
                }
            }
            if (bodyIsEmpty)
            {
                return new WebApiResponse()
                {

                    Headers = new NameValueCollection
                    {
                        { "OData-Version", "4.0" }
                    },
                    StatusCode = 204
                };
            }
            string jsonBody = body.ToJsonString();
            return new WebApiResponse()
            {
                Body = Encoding.UTF8.GetBytes(jsonBody),
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                StatusCode = 200
            };
        }

        private void AddValueToJsonObject(JsonObject body, KeyValuePair<string, object> property, string currentEntityLogicalName, IEdmProperty parameter)
        {
            switch (property.Value)
            {
                case null:
                    body[property.Key] = null;
                    break;
                case string strValue:
                    body[property.Key] = strValue;
                    break;
                case int intValue:
                    body[property.Key] = intValue;
                    break;
                case byte byteValue:
                    body[property.Key] = byteValue;
                    break;
                case Guid guidValue:
                    body[property.Key] = guidValue;
                    break;
                case float floatValue:
                    body[property.Key] = floatValue;
                    break;
                case double doubleValue:
                    body[property.Key] = doubleValue;
                    break;
                case decimal decimalValue:
                    body[property.Key] = decimalValue;
                    break;
                case DateTime dateTimeValue:
                    body[property.Key] = dateTimeValue;
                    break;
                case bool boolValue:
                    body[property.Key] = boolValue;
                    break;
                case OptionSetValue optionSetValue:
                    body[property.Key] = optionSetValue.Value;
                    break;
                case Money moneyValue:
                    body[property.Key] = moneyValue.Value;
                    break;
                case EntityReference entityReferenceValue:
                    {
                        if (currentEntityLogicalName == null)
                        {
                            // Here it's directly a property response
                            //The format is not the same if it's the only one property or if there are other properties
                            string typeName = "Microsoft.Dynamics.CRM." + entityReferenceValue.LogicalName;
                            var definition = this.Context.Model.FindType(typeName) as IEdmEntityType;
                            if (parameter == null)
                            {
                                body["@odata.type"] = "#" + typeName;
                                body[property.Key] = entityReferenceValue.Id;
                            }
                            else
                            {
                                var value = new JsonObject
                                {
                                    [definition.DeclaredKey.Single().Name] = entityReferenceValue.Id
                                };
                                body[property.Key] = value;
                            }
                        }
                        else
                        {
                            // Here it's a property of an entity record included in a property response
                            var entityTypeDefinition = (IEdmStructuredType)this.Context.Model.FindDeclaredType("Microsoft.Dynamics.CRM." + currentEntityLogicalName);
                            var declaredProperty = entityTypeDefinition.DeclaredProperties.Single(p => p.Name == property.Key);
                            if (declaredProperty.PropertyKind == EdmPropertyKind.Navigation)
                            {
                                body["_" + property.Key + "_value"] = entityReferenceValue.Id;
                            }
                            else
                            {
                                body[property.Key] = entityReferenceValue.Id;
                            }

                        }
                    }
                    break;
                case Entity record:
                    {
                        var parameterDefinition = parameter.Type.Definition;
                        JsonObject recordJson = ConvertEntityToJson(record, parameterDefinition.FullTypeName() == "Microsoft.Dynamics.CRM.crmbaseentity");
                        body[property.Key] = recordJson;

                    }
                    break;
                case EntityCollection collection:
                    {
                        var arrayJson = new JsonArray();
                        body[property.Key] = arrayJson;
                        foreach (var record in collection.Entities)
                        {
                            JsonObject recordJson = ConvertEntityToJson(record, true);
                            arrayJson.Add(recordJson);
                        }
                    }
                    break;
                case Guid[] ids:
                    {
                        var arrayJson = new JsonArray();
                        body[property.Key] = arrayJson;
                        foreach (var id in ids)
                        {
                            arrayJson.Add(id);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException($"Message has been executed but response cannot be generated. Parameter:{property.Key}={property.Value}");
            }
        }

        private JsonObject ConvertEntityToJson(Entity record, bool addType)
        {
            var recordJson = new JsonObject
            {
                ["@odata.etag"] = "DvbError: NotImplemented"
            };
            if (addType)
            {
                recordJson["@odata.type"] = "#Microsoft.Dynamics.CRM." + record.LogicalName;
            }

            string typeName = "Microsoft.Dynamics.CRM." + record.LogicalName;
            var definition = this.Context.Model.FindType(typeName) as IEdmEntityType;
            var key = definition.DeclaredKey.FirstOrDefault()?.Name;
            if (key != null && !record.Contains(key))
            {
                record[key] = record.Id;
            }

            foreach (var kvp in record.Attributes)
            {
                AddValueToJsonObject(recordJson, kvp, record.LogicalName, null);
            }

            return recordJson;
        }

        private WebApiResponse ConvertDeleteResponse()
        {
            return new WebApiResponse()
            {
                Body = new byte[0],
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" }
                },
                StatusCode = 204
            };
        }

        private WebApiResponse ConvertUpdateResponse(UpdateRequest updateRequest)
        {
            string setName = this.Context.MetadataCache.GetEntityFromLogicalName(updateRequest.Target.LogicalName).EntitySetName;
            var id = $"{this.Context.WebApiBaseUrl}{setName}({updateRequest.Target.Id})";
            return new WebApiResponse()
            {
                Body = new byte[0],
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                },
                StatusCode = 204
            };
        }

        private WebApiResponse ConvertCreateResponse(CreateRequest createRequest, CreateResponse createResponse)
        {
            string setName = this.Context.MetadataCache.GetEntityFromLogicalName(createRequest.Target.LogicalName).EntitySetName;
            var id = $"{this.Context.WebApiBaseUrl}{setName}({createResponse.id})";

            //TODO: convert the response instead of requesting
            //TODO : returns result only if return representation was requested
            var retrieveResult = HttpGet(id, true);
            return new WebApiResponse()
            {
                Body = retrieveResult.Content.ReadAsByteArrayAsync().Result,
                Headers = new NameValueCollection
                {
                    { "OData-Version", "4.0" },
                    { "OData-EntityId", id }
                },
                StatusCode = 201
            };
        }

        private WebApiResponse ConvertRetrieveResponse(RequestConversionResult conversionResult)
        {
            //TODO: convert the response instead of requesting
            string url = $"https://{this.Context.Host}{conversionResult.SrcRequest.LocalPathWithQuery}";
            HttpResponseMessage retrieveResult = HttpGet(url, false);
            NameValueCollection headers = new NameValueCollection();
            foreach (var header in retrieveResult.Headers)
            {
                headers.Add(header.Key, String.Concat(header.Value, ","));
            }
            return new WebApiResponse()
            {
                Body = retrieveResult.Content.ReadAsByteArrayAsync().Result,
                Headers = headers,
                StatusCode = 200
            };
        }

        private HttpResponseMessage HttpGet(string url, bool bypassPLugins)
        {
            HttpRequestMessage retrieveMessage = new HttpRequestMessage(HttpMethod.Get, url);
            this.Context.AddAuthorizationHeaders(retrieveMessage);
            if (bypassPLugins)
            {
                retrieveMessage.Headers.Add("MSCRM.BypassCustomPluginExecution", "true");
            }
            var retrieveResult = this.Context.HttpClient.SendAsync(retrieveMessage).Result;
            return retrieveResult;
        }
    }
}
