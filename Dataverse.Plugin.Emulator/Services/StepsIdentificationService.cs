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

        private PluginEmulator Emulator { get; }

        public StepsIdentificationService(PluginEmulator emulator)
        {
            this.Emulator = emulator;
        }

        private string GetMessageNameFromRequest(OrganizationRequest request)
        {
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
            return messageName;
        }

        public IEnumerable<IStepTriggered> GetStepsToExecute(OrganizationRequest request, out string targetLogicalName)
        {
            switch (request)
            {
                case CreateRequest createRequest:
                    return GetStepsToExecute_Simple(request, out targetLogicalName);
                case UpsertRequest upsertMultipleRequest:
                    return GetStepsToExecute_Simple(request, out targetLogicalName);
                case UpdateRequest updateRequest:
                    return GetStepsToExecute_Simple(request, out targetLogicalName);
                case DeleteRequest deleteRequest:
                    return GetStepsToExecute_Simple(request, out targetLogicalName);
                //TODO : deletemultiple still not implemented in SDK ?
                case CreateMultipleRequest createMultipleRequest:
                    return GetStepsToExecute_Multiple(createMultipleRequest, out targetLogicalName);
                case UpsertMultipleRequest upsertMultipleRequest:
                    return GetStepsToExecute_Multiple(upsertMultipleRequest, out targetLogicalName);
                case UpdateMultipleRequest updateMultipleRequest:
                    return GetStepsToExecute_Multiple(updateMultipleRequest, out targetLogicalName);
                default:
                    return GetStepsToExecute_Simple(request, out targetLogicalName);
            }
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(CreateMultipleRequest createMultipleRequest, out string targetLogicalName)
        {
            string messageName = GetMessageNameFromRequest(createMultipleRequest);
            var targets = createMultipleRequest.Targets;
            targetLogicalName = targets.Entities.FirstOrDefault()?.LogicalName;
            IEnumerable<PluginStepDescription> stepsToExecute = null;
            if (this.Emulator.PluginSteps.TryGetValue(messageName, out var steps))
            {
                stepsToExecute = steps;
            }
            //TODO : add simple Create
            return stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = createMultipleRequest,
                Targets = targets
            });
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Simple(OrganizationRequest request, out string targetLogicalName)
        {
            string messageName = GetMessageNameFromRequest(request);
            IEnumerable<PluginStepDescription> stepsToExecute = null;
            EntityReference targetRef = null;
            Entity target = null;
            targetLogicalName = null;
            if (this.Emulator.PluginSteps.TryGetValue(messageName, out var steps))
            {
                stepsToExecute = steps;
            }
            switch (request)
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
            return stepsToExecute?.Select(s => new SimpleStepTriggered()
            {
                StepDescription = s,
                TargetReference = targetRef,
                OrganizationRequest = request
            });
        }



    }
}
