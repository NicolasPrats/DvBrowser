using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dataverse.Utils.Constants;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm;
using Microsoft.Xrm.Sdk;

namespace Dataverse.WebApi2IOrganizationService.Converters
{
    public partial class RequestConverter
    {
        private void ConvertToAction(IEdmOperation operation, RequestConversionResult conversionResult, bool isBound)
        {
            OrganizationRequest request = new OrganizationRequest(operation.Name);
            string boundParameterName = null;
            if (isBound)
            {
                boundParameterName = operation.Parameters.First().Name;
            }
            using (JsonDocument json = JsonDocument.Parse(conversionResult.SrcRequest.Body))
            {
                foreach (var node in json.RootElement.EnumerateObject())
                {
                    string key = node.Name;
                    var parameter = operation.FindParameter(key) ?? throw new NotSupportedException($"parameter {key} not found!");
                    var customApiRequestParameter = this.Context.MetadataCache.GetCustomApiRequestParameter(key);
                    if (key == boundParameterName)
                    {
                        key = "Target";
                    }
                    request[key] = ConvertValueToAttribute(node.Value, customApiRequestParameter, parameter.Type);
                }
            }
            conversionResult.ConvertedRequest = request;
        }

        private object ConvertValueToAttribute(JsonElement value, Entity customApiRequestParameter, IEdmTypeReference edmType)
        {
            int type = customApiRequestParameter?.GetAttributeValue<OptionSetValue>("type")?.Value ?? GetRequestParameterTypeFromEdmType(edmType); ;
            if (value.ValueKind == JsonValueKind.Null)
                return null;
            string typeName = edmType.FullName();
            switch (type)
            {
                case CustomApiRequestParameterType.Boolean:
                    return value.GetBoolean();
                case CustomApiRequestParameterType.DateTime:
                    return value.GetDateTime();
                case CustomApiRequestParameterType.Decimal:
                    return value.GetDecimal();
                case CustomApiRequestParameterType.Entity:
                    return ConvertToEntity(value, edmType, typeName);
                case CustomApiRequestParameterType.EntityCollection:
                    var collection = new EntityCollection();
                    foreach (var item in value.EnumerateArray())
                    {
                        collection.Entities.Add(ConvertToEntity(item, null, null));
                    }
                    collection.EntityName = collection.Entities.FirstOrDefault()?.LogicalName;
                    if (collection.EntityName == null && customApiRequestParameter != null)
                    {
                        collection.EntityName = customApiRequestParameter.GetAttributeValue<string>("entitylogicalname");
                    }
                    //TODO if collection.EntityName == null, check type of items in collection ?
                    return collection;
                case CustomApiRequestParameterType.EntityReference:
                    return ConvertToEntity(value, edmType, typeName).ToEntityReference();
                case CustomApiRequestParameterType.Float:
                    return value.GetDouble();
                case CustomApiRequestParameterType.Integer:
                    return value.GetInt32();
                case CustomApiRequestParameterType.Money:
                    return new Money(value.GetDecimal());
                case CustomApiRequestParameterType.Picklist:
                    return new OptionSetValue(value.GetInt32());
                case CustomApiRequestParameterType.String:
                    return value.GetString();
                case CustomApiRequestParameterType.StringArray:
                    List<string> list = new List<string>();
                    foreach (var item in value.EnumerateArray())
                    {
                        list.Add(item.GetString());
                    }
                    return list.ToArray();
                case CustomApiRequestParameterType.Guid:
                    return value.GetGuid();
                default:
                    throw new NotImplementedException($"Type {typeName}({type}) is not implemented!");
            }
        }

        private int GetRequestParameterTypeFromEdmType(IEdmTypeReference edmType)
        {
            switch (edmType.FullName())
            {
                case "Edm.Boolean":
                    return CustomApiRequestParameterType.Boolean;
                case "Edm.Byte":
                    return CustomApiRequestParameterType.Integer;
                case "Edm.DateTime":
                    return CustomApiRequestParameterType.DateTime;
                case "Edm.Decimal":
                    return CustomApiRequestParameterType.Decimal;
                case "Edm.Double":
                    return CustomApiRequestParameterType.Float;
                case "Edm.Single":
                    return CustomApiRequestParameterType.Float;
                case "Edm.Guid":
                    return CustomApiRequestParameterType.Guid;
                case "Edm.Int16":
                    return CustomApiRequestParameterType.Integer;
                case "Edm.Int32":
                    return CustomApiRequestParameterType.Integer;
                case "Edm.Int64":
                    return CustomApiRequestParameterType.Integer;
                case "Edm.SByte":
                    return CustomApiRequestParameterType.Integer;
                case "Edm.String":
                    return CustomApiRequestParameterType.String;
                default:
                    if (edmType.TypeKind() == EdmTypeKind.Entity)
                    {
                        return CustomApiRequestParameterType.EntityReference;
                    }
                    else if (edmType.TypeKind() == EdmTypeKind.Collection)
                    {
                        return CustomApiRequestParameterType.EntityCollection;
                    }
                    else
                    {
                        throw new NotImplementedException($"Type {edmType.TypeKind()} is not implemented!");
                    }

            }
        }

        private Entity ConvertToEntity(JsonElement value, IEdmTypeReference type, string typeName)
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
                throw new NotSupportedException($"IEdmEntityType was expected but not found for type {typeName}!");
            }
            var key = definition.DeclaredKey.FirstOrDefault()?.Name;
            if (!value.TryGetProperty(key, out var id))
            {
                throw new NotSupportedException($"@{key} property must be set!");
            }
            return new Entity(definition.Name, new Guid(id.GetString()));
        }
    }
}
