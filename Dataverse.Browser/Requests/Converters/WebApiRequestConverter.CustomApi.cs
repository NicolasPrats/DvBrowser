using System;
using System.Activities;
using System.Activities.DurableInstancing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using System.Xml;
using CefSharp;
using CefSharp.DevTools.DOM;
using Dataverse.Browser.Constants;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Dataverse.Browser.Requests.Converter
{
    internal partial class WebApiRequestConverter
    {
        private void ConvertToAction(IEdmOperation operation, InterceptedWebApiRequest webApiRequest)
        {
            OrganizationRequest request = new OrganizationRequest(operation.Name);
            using (JsonDocument json = JsonDocument.Parse(webApiRequest.SimpleHttpRequest.Body))
            {
                foreach (var node in json.RootElement.EnumerateObject())
                {
                    string key = node.Name;
                    var parameter = operation.FindParameter(key);
                    if (parameter == null)
                    {
                        throw new NotSupportedException($"parameter {key} not found!");
                    }
                    request[key] = ConvertValueToAttribute(node.Value, parameter.Type);
                }
            }
            webApiRequest.ConvertedRequest = request;
        }

        private object ConvertValueToAttribute(JsonElement value, IEdmTypeReference type)
        {
            switch (type.FullName())
            {
                case "Edm.Boolean":
                    return value.GetBoolean();
                case "Edm.Byte":
                    return value.GetByte();
                case "Edm.DateTime":
                    return value.GetDateTime();
                case "Edm.Decimal":
                    return value.GetDecimal();
                case "Edm.Double":
                    return value.GetDouble();
                case "Edm.Single":
                    return value.GetSingle();
                case "Edm.Guid":
                    return value.GetGuid();
                case "Edm.Int16":
                    return value.GetInt16();
                case "Edm.Int32":
                    return value.GetInt32();
                case "Edm.Int64":
                    return value.GetInt64();
                case "Edm.SByte":
                    return value.GetSByte();
                case "Edm.String":
                    return value.GetString();
                default:
                    if (type.TypeKind() == EdmTypeKind.Entity)
                    {
                        string typeName = type.FullName();
                        return ConvertToEntityReference(value, type, typeName);
                    }
                    else if (type.TypeKind() == EdmTypeKind.Collection)
                    {
                        EntityReferenceCollection collection = new EntityReferenceCollection();
                        foreach (var item in value.EnumerateArray())
                        {
                            collection.Add(ConvertToEntityReference(item, null, null));
                        }
                        return collection;
                    }
                    else
                    {
                        throw new NotImplementedException($"Type {type.TypeKind()} is not implemented!");
                    }

            }
            throw new NotSupportedException("Type is unknown:" + type.FullName());
        }

        private  EntityReference ConvertToEntityReference(JsonElement value, IEdmTypeReference type, string typeName)
        {
            if (!value.TryGetProperty("@odata.type", out var dataType))
            {
                throw new NotSupportedException("@odata.type property must be set!");
            }
            if (typeName != null && dataType.GetString() != typeName)
            {
                throw new NotSupportedException($"@odata.type property is of type {dataType.GetString()} whereas {typeName} was expected!");
            }
            typeName = dataType.GetString();
            IEdmEntityType definition;
            if (type == null)
            {
                definition = this.Context.Model.FindType(typeName) as IEdmEntityType;
            }
            else
            {
                definition = type?.Definition as IEdmEntityType;
            }
            if (definition == null)
            {
                throw new NotSupportedException($"IEdmEntityType was expected!");
            }
            var key = definition.DeclaredKey.FirstOrDefault()?.Name;
            if (!value.TryGetProperty(key, out var id))
            {
                throw new NotSupportedException($"@{key} property must be set!");
            }
            return new EntityReference(typeName, id.GetGuid());
        }
    }
}
