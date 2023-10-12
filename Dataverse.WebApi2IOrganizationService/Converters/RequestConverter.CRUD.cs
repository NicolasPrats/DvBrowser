using System;
using System.Linq;
using System.Text.Json;
using Dataverse.Utils.Constants;
using Dataverse.WebApi2IOrganizationService.Model;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.WebApi2IOrganizationService.Converters
{
    public partial class RequestConverter

    {

        private void ConvertToDeleteRequest(RequestConversionResult conversionResult, ODataPath path)
        {
            var entitySegment = path.FirstSegment as EntitySetSegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var keySegment = path.LastSegment as KeySegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var entity = this.Context.MetadataCache.GetEntityFromSetName(entitySegment.Identifier);
            if (entity == null)
            {
                throw new NotSupportedException("Entity not found: " + entity);
            }
            DeleteRequest deleteRequest = new DeleteRequest
            {
                Target = GetEntityReferenceFromKeySegment(entity, keySegment)
            };
            conversionResult.ConvertedRequest = deleteRequest;
        }

        private void ConvertToRetrieveRequest(RequestConversionResult conversionResult, ODataUriParser parser, ODataPath path)
        {
            var entitySegment = path.FirstSegment as EntitySetSegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var keySegment = path.LastSegment as KeySegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var entity = this.Context.MetadataCache.GetEntityFromSetName(entitySegment.Identifier);
            if (entity == null)
            {
                throw new NotSupportedException("Entity not found: " + entity);
            }

            RetrieveRequest retrieveRequest = new RetrieveRequest
            {
                Target = GetEntityReferenceFromKeySegment(entity, keySegment),
                ColumnSet = GetColumnSet(parser)
            };
            conversionResult.ConvertedRequest = retrieveRequest;
        }

        private ColumnSet GetColumnSet(ODataUriParser parser)
        {
            var selectAndExpand = parser.ParseSelectAndExpand();
            if (selectAndExpand == null || selectAndExpand.AllSelected)
                return new ColumnSet(true);
            ColumnSet columnSet = new ColumnSet();
            foreach (var item in selectAndExpand.SelectedItems)
            {
                if (!(item is PathSelectItem pathSelectItem))
                {
                    throw new NotImplementedException("Item not supported:" + item.GetType().Name);
                }
                if (pathSelectItem.HasOptions)
                    throw new NotSupportedException("Options are not supported");

                if (pathSelectItem.SelectedPath.Count != 1)
                    throw new NotSupportedException("Only 1 segment was expected");
                if (!(pathSelectItem.SelectedPath.FirstSegment is PropertySegment propertySegment))
                    throw new NotSupportedException("Only property segment are supported");
                var navigationProperties = propertySegment.Property.DeclaringType.NavigationProperties();
                var navigationProperty = navigationProperties.FirstOrDefault(p => p.ReferentialConstraint != null && p.ReferentialConstraint.PropertyPairs.Any(rc => rc.DependentProperty?.Name == propertySegment.Identifier));
                if (navigationProperty == null)
                {
                    columnSet.AddColumn(propertySegment.Identifier);
                }
                else
                {
                    columnSet.AddColumn(navigationProperty.Name);
                }
            }
            return columnSet;
        }


        private void ConvertToCreateUpdateRequest(RequestConversionResult conversionResult, ODataPath path)
        {
            var entity = this.Context.MetadataCache.GetEntityFromSetName(path.FirstSegment.Identifier) ?? throw new ApplicationException("Entity not found: " + path.FirstSegment.Identifier);
            KeySegment keySegment = null;
            if (conversionResult.SrcRequest.Method == "PATCH")
            {
                keySegment = path.LastSegment as KeySegment;

            }
            conversionResult.ConvertedRequest = ConvertToCreateUpdateRequest(keySegment, conversionResult, entity.LogicalName);
        }

        private OrganizationRequest ConvertToCreateUpdateRequest(KeySegment keySegment, RequestConversionResult conversionResult, string entityLogicalName)
        {
            string body = conversionResult.SrcRequest.Body ?? throw new NotSupportedException("A body was expected!");
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
                GetIdFromKeySegment(keySegment, out var id, out var keys);
                if (id == Guid.Empty)
                {
                    record.KeyAttributes = keys;
                }
                else
                {
                    record.Id = id;
                }
            }

            using (JsonDocument json = JsonDocument.Parse(body))
            {
                ReadEntityFromJson(entityMetadata, record, json.RootElement);
            }

            return request;
        }

        private void ReadEntityFromJson(EntityMetadata entityMetadata, Entity record, JsonElement json, string primaryKeyPropertyName = null)
        {
            foreach (var node in json.EnumerateObject())
            {
                string key = node.Name;
                if (key.EndsWith("@OData.Community.Display.V1.FormattedValue"))
                    continue;

                if (key.EndsWith("@odata.bind"))
                {
                    key = ExtractAttributeNameFromodatabind(entityMetadata, key);
                }
                else if (key.Contains("@odata.type"))
                {
                    //ignore
                    //Todo : check if coherent with entitymetadata ?
                    continue;
                }
                else if (key.Contains("@"))
                {
                    throw new NotSupportedException("Unknow property key:" + key);
                }

                AttributeMetadata attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == key);
                if (attributeMetadata != null)
                {
                    object value = ConvertValueToAttribute(attributeMetadata, node.Value);
                    record.Attributes.Add(key, value);
                }
                else if (key == primaryKeyPropertyName)
                {
                    attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.IsPrimaryId == true);
                    record.Id = (Guid)ConvertValueToAttribute(attributeMetadata, node.Value);
                }
                else
                {
                    var relation = entityMetadata.OneToManyRelationships.FirstOrDefault(r => r.SchemaName == key) ?? throw new NotSupportedException("No attribute nor relation found: " + key);
                    if (relation.ReferencingEntity != "activityparty")
                    {
                        throw new NotSupportedException("Unsupported relation found: " + key);
                    }
                    AddActivityParties(record, node.Value);
                }
            }
        }

        private static string ExtractAttributeNameFromodatabind(EntityMetadata entityMetadata, string odatabindValue)
        {
            string key = odatabindValue.Substring(0, odatabindValue.Length - "@odata.bind".Length).ToLowerInvariant();
            var relation = entityMetadata.ManyToOneRelationships.FirstOrDefault(r => r.ReferencingEntityNavigationPropertyName == key);
            if (relation != null)
            {
                key = relation.ReferencingAttribute;
            }
            return key;
        }

        private void AddActivityParties(Entity record, JsonElement values)
        {
            var entityMetadata = this.Context.MetadataCache.GetEntityMetadataWithAttributes("activityparty");
            foreach (var value in values.EnumerateArray())
            {
                int participationTypeMask = -1;
                EntityReference entityReference = null;
                foreach (var attribute in value.EnumerateObject())
                {
                    if (attribute.Name == "participationtypemask")
                    {
                        participationTypeMask = attribute.Value.GetInt32();
                    }
                    else if (attribute.Name.StartsWith("partyid_") && attribute.Name.EndsWith("@odata.bind"))
                    {
                        string attributeName = ExtractAttributeNameFromodatabind(entityMetadata, attribute.Name);
                        AttributeMetadata attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == attributeName);
                        if (attributeMetadata != null)
                        {
                            entityReference = ConvertValueToAttribute(attributeMetadata, attribute.Value) as EntityReference;
                        }
                    }
                }
                if (participationTypeMask == -1)
                {
                    throw new NotSupportedException("ParticipationTypeMask not found!");
                }

                var targetAttributeName = GetActivityPartyAttributeName(record.LogicalName, participationTypeMask);
                EntityCollection collection = record.GetAttributeValue<EntityCollection>(targetAttributeName);
                if (collection == null)
                {
                    record[targetAttributeName] = collection = new EntityCollection();
                    collection.EntityName = "activityparty";
                }
                var party = new Entity("activityparty");
                //todo:other columns necessary ?
#pragma warning disable S2583 // Conditionally executed code should be reachable. Justification = false positive. There are some paths where entityReference is null and others where it's not null.
                party["partyid"] = entityReference ?? throw new NotSupportedException("Target record in activity party list not found!");
#pragma warning restore S2583 // Conditionally executed code should be reachable

                collection.Entities.Add(party);
            }

        }

        private string GetActivityPartyAttributeName(string logicalName, int participationTypeMask)
        {
            //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/activityparty-entity#activity-party-types-available-for-each-activity
            switch ((logicalName, participationTypeMask))
            {
                case var tuple when tuple.logicalName == "appointment" && ActivityPartyType.OptionalAttendee == tuple.participationTypeMask:
                    return "optionalattendees";
                case var tuple when tuple.logicalName == "appointment" && ActivityPartyType.Organizer == tuple.participationTypeMask:
                    return "organizer";
                case var tuple when tuple.logicalName == "appointment" && ActivityPartyType.RequiredAttendee == tuple.participationTypeMask:
                    return "requiredattendees";
                case var tuple when tuple.logicalName == "campaignactivity" && ActivityPartyType.Sender == tuple.participationTypeMask:
                    return "from";
                case var tuple when tuple.logicalName == "campaignresponse" && ActivityPartyType.Customer == tuple.participationTypeMask:
                    return "from";
                case var tuple when tuple.logicalName == "email" && ActivityPartyType.BccRecipient == tuple.participationTypeMask:
                    return "bcc";
                case var tuple when tuple.logicalName == "email" && ActivityPartyType.CCRecipient == tuple.participationTypeMask:
                    return "cc";
                case var tuple when tuple.logicalName == "email" && ActivityPartyType.Sender == tuple.participationTypeMask:
                    return "from";
                case var tuple when tuple.logicalName == "email" && ActivityPartyType.ToRecipient == tuple.participationTypeMask:
                    return "to";
                case var tuple when tuple.logicalName == "fax" && ActivityPartyType.Sender == tuple.participationTypeMask:
                    return "from";
                case var tuple when tuple.logicalName == "fax" && ActivityPartyType.ToRecipient == tuple.participationTypeMask:
                    return "to";
                case var tuple when tuple.logicalName == "letter" && ActivityPartyType.BccRecipient == tuple.participationTypeMask:
                    return "bcc";
                case var tuple when tuple.logicalName == "letter" && ActivityPartyType.Sender == tuple.participationTypeMask:
                    return "from";
                case var tuple when tuple.logicalName == "letter" && ActivityPartyType.ToRecipient == tuple.participationTypeMask:
                    return "to";
                case var tuple when tuple.logicalName == "phonecall" && ActivityPartyType.Sender == tuple.participationTypeMask:
                    return "from";
                case var tuple when tuple.logicalName == "phonecall" && ActivityPartyType.ToRecipient == tuple.participationTypeMask:
                    return "to";
                case var tuple when tuple.logicalName == "recurringappointmentmaster" && ActivityPartyType.OptionalAttendee == tuple.participationTypeMask:
                    return "optionalattendees";
                case var tuple when tuple.logicalName == "recurringappointmentmaster" && ActivityPartyType.Organizer == tuple.participationTypeMask:
                    return "organizer";
                case var tuple when tuple.logicalName == "recurringappointmentmaster" && ActivityPartyType.RequiredAttendee == tuple.participationTypeMask:
                    return "requiredattendees";
                case var tuple when tuple.logicalName == "serviceappointment" && ActivityPartyType.Customer == tuple.participationTypeMask:
                    return "customer";
                case var tuple when tuple.logicalName == "serviceappointment" && ActivityPartyType.Resource == tuple.participationTypeMask:
                    return "resource";
                case var tuple when ActivityPartyType.Owner == tuple.participationTypeMask:
                    return "ownerid";
            }
            throw new NotImplementedException("Unknow activity party attribute: " + logicalName + "/" + participationTypeMask);
        }


        private object ConvertValueToAttribute(AttributeMetadata attributeMetadata, JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }
            switch (attributeMetadata.AttributeType)
            {
                case AttributeTypeCode.BigInt:
                    return value.GetInt64();
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    var parser = new ODataUriParser(this.Context.Model, new Uri(value.GetString(), UriKind.Relative))
                    {
                        Resolver = new AlternateKeysODataUriResolver(this.Context.Model)
                    };
                    var path = parser.ParsePath();
                    if (path.Count != 2)
                    {
                        throw new NotSupportedException("2 segments was expected:" + value.GetString());
                    }
                    var entitySegment = path.FirstSegment as EntitySetSegment;
                    var keySegment = path.LastSegment as KeySegment;
                    if (entitySegment == null || keySegment == null)
                    {
                        throw new NotSupportedException($"Error while parsing[{value.GetString()}]: {entitySegment}-{keySegment}");
                    }
                    var entity = this.Context.MetadataCache.GetEntityFromSetName(path.FirstSegment.Identifier);
                    return GetEntityReferenceFromKeySegment(entity, keySegment);
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
                    throw new NotSupportedException("Unsupported type:" + attributeMetadata.AttributeType);
            }
        }



    }
}
