using System;
using System.Activities.DurableInstancing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.DevTools.DOM;
using Dataverse.Browser.Constants;
using Dataverse.Browser.Context;
using Dataverse.Browser.Requests.SimpleClasses;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Dataverse.Browser.Requests.Converter
{
    internal partial class WebApiRequestConverter

    {
       

        private void ConvertToDeleteRequest(InterceptedWebApiRequest webApiRequest, ODataPath path)
        {
            var entitySegment = path.FirstSegment as EntitySetSegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var keySegment = path.LastSegment as KeySegment ?? throw new NotSupportedException("First segment should not be of type: " + path.FirstSegment.EdmType);
            var entity = this.Context.MetadataCache.GetEntityFromSetName(entitySegment.Identifier);
            if (entity == null)
            {
                throw new ApplicationException("Entity not found: " + entity);
            }
            var id = GetIdFromKeySegment(keySegment);

            DeleteRequest deleteRequest = new DeleteRequest
            {
                Target = new EntityReference(entity.LogicalName, id)
            };
            webApiRequest.ConvertedRequest = deleteRequest;
        }

        private void ConvertToRetrieveRequest(InterceptedWebApiRequest webApiRequest, ODataUriParser parser, ODataPath path)
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
                ColumnSet = GetColumnSet(parser)
            };
            webApiRequest.ConvertedRequest = retrieveRequest;
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


        private void ConvertToCreateUpdateRequest(InterceptedWebApiRequest webApiRequest, ODataPath path)
        {
            var entity = this.Context.MetadataCache.GetEntityFromSetName(path.FirstSegment.Identifier);
            if (entity == null)
            {
                throw new ApplicationException("Entity not found: " + entity);
            }
            KeySegment keySegment = null;
            if (webApiRequest.SimpleHttpRequest.Method == "PATCH")
            {
                keySegment = path.LastSegment as KeySegment;

            }
            webApiRequest.ConvertedRequest = ConvertToCreateUpdateRequest(keySegment, webApiRequest, entity.LogicalName);
        }

       private OrganizationRequest ConvertToCreateUpdateRequest(KeySegment keySegment, InterceptedWebApiRequest webApiRequest, string entityLogicalName)
        {
            string body = webApiRequest.SimpleHttpRequest.Body ?? throw new NotSupportedException("A body was expected!");
            webApiRequest.SimpleHttpRequest.Body = body;
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
                        key = ExtractAttributeNameFromodatabind(entityMetadata, key);
                    }
                    else if (key.Contains("@"))
                    {
                        throw new ApplicationException("Unknow property key:" + key);
                    }

                    AttributeMetadata attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == key);
                    if (attributeMetadata != null)
                    {
                        object value = ConvertValueToAttribute(attributeMetadata, node.Value);
                        record.Attributes.Add(key, value);
                    }
                    else
                    {
                        var relation = entityMetadata.OneToManyRelationships.FirstOrDefault(r => r.SchemaName == key);
                        if (relation == null)
                        {
                            throw new NotSupportedException("No attribute nor relation found: " + key);
                        }
                        if (relation.ReferencingEntity != "activityparty")
                        {
                            throw new NotSupportedException("Unsupported relation found: " + key);
                        }
                        AddActivityParties(record, node.Value);
                    }
                }
            }

            return request;
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
            foreach (var value in values.EnumerateArray()) {
                int participationTypeMask = -1;
                EntityReference entityReference = null;
                foreach (var attribute in value.EnumerateObject())
                {
                    if (attribute.Name == "participationtypemask")
                    {
                        participationTypeMask = attribute.Value.GetInt32();
                    }
                    else
                    {
                        string attributeName = ExtractAttributeNameFromodatabind(entityMetadata, attribute.Name);
                        AttributeMetadata attributeMetadata = entityMetadata.Attributes.FirstOrDefault(a => a.LogicalName == attributeName);
                        if (attributeMetadata != null)
                        {
                            entityReference = ConvertValueToAttribute(attributeMetadata, attribute.Value) as EntityReference;
                        }
                    }
                }
                if (participationTypeMask == -1) {
                    throw new NotSupportedException("ParticipationTypeMask not found!");
                }
                if (entityReference == null) {
                    throw new NotSupportedException("Target record in activity party list not found!");
                }
                var targetAttributeName = GetActivityPartyAttributeName(record.LogicalName, participationTypeMask);
                EntityCollection collection = record.GetAttributeValue<EntityCollection>(targetAttributeName); ;
                if (collection == null)
                {
                    record[targetAttributeName] = collection = new EntityCollection();
                    collection.EntityName = "activityparty";
                }
                var party = new Entity("activityparty");
                //todo:other columns necessary ?
                party["partyid"] = entityReference;

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
            }
            throw new NotImplementedException("Unknow activity party attribute: " + logicalName + "/" + participationTypeMask);
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
