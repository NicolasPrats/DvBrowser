using System;
using System.Collections.Generic;
using System.Linq;
using Dataverse.Plugin.Emulator.Steps;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class StepsIdentificationService
    {

        private OrganizationRequest Request { get; }
        private string MessageName { get; }
        private PluginEmulator Emulator { get; }

        public StepsIdentificationService(PluginEmulator emulator, OrganizationRequest request)
        {
            this.Request = request;

            string messageName = request.RequestName;
            if (String.IsNullOrEmpty(messageName))
            {
                var typeName = request.GetType().Name;
                if (typeName.EndsWith("Request"))
                {
                    messageName = typeName.Substring(0, typeName.Length - "Request".Length);
                }
                else
                {
                    messageName = typeName;
                }
            }
            if (String.IsNullOrEmpty(messageName))
            {
                throw new ApplicationException("Message has not been identified!");
            }
            this.MessageName = messageName;
            this.Emulator = emulator;
        }

        public IEnumerable<StepTriggered> GetStepsToExecute(out string targetLogicalName)
        {
            IEnumerable<PluginStepDescription> stepsToExecute = null;
            EntityReference targetRef = null;
            Entity target = null;
            targetLogicalName = null;
            if (this.Emulator.PluginSteps.TryGetValue(this.MessageName, out var steps))
            {
                stepsToExecute = steps;
            }
            switch (this.Request)
            {
                case CreateRequest createRequest:
                    target = createRequest.Target;
                    break;
                case UpsertRequest upsertRequest:
                    target = upsertRequest.Target;
                    //TODO : en cas d'update il faudrait appliquer les filtering attributes
                    break;
                case RetrieveRequest retrieveRequest:
                    targetRef = retrieveRequest.Target;
                    break;
                case UpdateRequest updateRequest:
                    target = updateRequest.Target;
                    stepsToExecute = stepsToExecute?.Where(s => s.FilteringAttributes == null || s.FilteringAttributes.Length == 0
                    || s.FilteringAttributes.Intersect(updateRequest.Target.Attributes.Select(a => a.Key)).Any());
                    break;
                case DeleteRequest deleteRequest:
                    targetRef = deleteRequest.Target;
                    break;
                case RetrieveMultipleRequest retrieveMultipleRequest:
                    if (retrieveMultipleRequest.Query is QueryExpression query)
                    {
                        targetLogicalName = query.EntityName;
                    }
                    else
                    {
                        //TODO: gérer les autres types de query
                        throw new NotImplementedException(retrieveMultipleRequest?.Query.GetType().Name);
                    }
                    break;
            }
            if (targetRef == null && target != null)
            {
                if (target.Id == Guid.Empty && target.KeyAttributes.Count == 0)
                {
                    var entityMetadata = this.Emulator.DataCache.GetMetadataEntityWithAttributes(target.LogicalName);
                    AttributeMetadata pkAttribute = entityMetadata.Attributes.FirstOrDefault(x => x.IsPrimaryId == true);
                    target.Id = target.GetAttributeValue<Guid>(pkAttribute.LogicalName);

                    //TODO: check alternate keys
                }

                targetRef = target.ToEntityReference();
            }

            targetLogicalName = targetLogicalName ?? targetRef?.LogicalName;

            if (targetLogicalName != null)
            {
                string logicalName = targetLogicalName;
                stepsToExecute = stepsToExecute?.Where(s => s.PrimaryEntity == logicalName);
            }
            return stepsToExecute?.Select(s => new StepTriggered()
            {
                StepDescription = s,
                TargetReference = targetRef,
                OrganizationRequest = Request
            }); ;
        }
    }
}
