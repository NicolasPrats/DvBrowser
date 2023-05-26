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
            int type = customApiRequestParameter.GetAttributeValue<OptionSetValue>("type").Value;
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
                    if (collection.EntityName == null)
                    {
                        collection.EntityName = customApiRequestParameter.GetAttributeValue<string>("entitylogicalname") ?? "contact";
                    }
                    return collection;
                case CustomApiRequestParameterType.EntityReference:
                    return ConvertToEntity(value, edmType, typeName).ToEntityReference();
                case CustomApiRequestParameterType.Float:
                    return value.GetDouble();
                case CustomApiRequestParameterType.Integer:
                    return value.GetInt32();
                case CustomApiRequestParameterType.Money:
                    return new Money(value.GetInt32());
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
                throw new NotSupportedException($"IEdmEntityType was expected!");
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
