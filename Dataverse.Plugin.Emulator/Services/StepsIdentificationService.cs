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
                throw new NotSupportedException("Message has not been identified!");
            }
            return messageName;
        }

        public IEnumerable<IStepTriggered> GetStepsToExecute(OrganizationRequest request, out string targetLogicalName)
        {
            switch (request)
            {
                case CreateRequest createRequest:
                    return GetStepsToExecute_Simple(true, createRequest, out targetLogicalName);
                case UpsertRequest upsertMultipleRequest:
                    return GetStepsToExecute_Simple(true, upsertMultipleRequest, out targetLogicalName);
                case UpdateRequest updateRequest:
                    return GetStepsToExecute_Simple(true, updateRequest, out targetLogicalName);
                case DeleteRequest deleteRequest:
                    return GetStepsToExecute_Simple(true, deleteRequest, out targetLogicalName);
                //TODO : deletemultiple still not implemented in SDK ?
                case CreateMultipleRequest createMultipleRequest:
                    return GetStepsToExecute_Multiple(true, createMultipleRequest, out targetLogicalName);
                case UpsertMultipleRequest upsertMultipleRequest:
                    return GetStepsToExecute_Multiple(true, upsertMultipleRequest, out targetLogicalName);
                case UpdateMultipleRequest updateMultipleRequest:
                    return GetStepsToExecute_Multiple(true, updateMultipleRequest, out targetLogicalName);
                default:
                    return GetStepsToExecute_Simple(true, request, out targetLogicalName);
            }
        }


        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(bool includeMergedPipeline, CreateMultipleRequest createMultipleRequest, out string targetLogicalName)
        {
            string messageName = GetMessageNameFromRequest(createMultipleRequest);
            var targets = createMultipleRequest.Targets;
            targetLogicalName = targets.Entities.FirstOrDefault()?.LogicalName;
            IEnumerable<PluginStepDescription> stepsToExecute = null;
            if (this.Emulator.PluginSteps.TryGetValue(messageName, out var steps))
            {
                string targetName = targetLogicalName;
                stepsToExecute = steps.Where(s => s.PrimaryEntity == targetName);
            }
            var stepsToExecute_multiple = stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = createMultipleRequest,
                Targets = targets
            });

            if (!includeMergedPipeline)
            {
                return stepsToExecute_multiple;
            }
            var stepsToExecute_simple = new List<IStepTriggered>();
            for (int i = 0; i < targets.Entities.Count; i++)
            {
                CreateRequest fakeRequest = new CreateRequest
                {
                    Target = targets.Entities[i],
                    Parameters = {
                        { "DvBrowserTargetIndex",i}
                    }
                };
                stepsToExecute_simple.AddRange(GetStepsToExecute_Simple(false, fakeRequest, out _));
            }
            return stepsToExecute_multiple.Union(stepsToExecute_simple);
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(bool includeMergedPipeline, UpsertMultipleRequest upsertMultipleRequest, out string targetLogicalName)
        {
            string messageName = GetMessageNameFromRequest(upsertMultipleRequest);
            var targets = upsertMultipleRequest.Targets;
            targetLogicalName = targets.Entities.FirstOrDefault()?.LogicalName;
            IEnumerable<PluginStepDescription> stepsToExecute = null;
            if (this.Emulator.PluginSteps.TryGetValue(messageName, out var steps))
            {

                string targetName = targetLogicalName;
                stepsToExecute = steps.Where(s => s.PrimaryEntity == targetName);
            }
            //TODO : in case of update: filtering attributes
            var stepsToExecute_multiple = stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = upsertMultipleRequest,
                Targets = targets
            });
            if (!includeMergedPipeline)
            {
                return stepsToExecute_multiple;
            }
            var stepsToExecute_simple = new List<IStepTriggered>();
            foreach (var target in targets.Entities)
            {
                UpsertRequest fakeRequest = new UpsertRequest
                {
                    Target = target
                };
                stepsToExecute_simple.AddRange(GetStepsToExecute_Simple(false, fakeRequest, out _));
            }
            return stepsToExecute_multiple.Union(stepsToExecute_simple);
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(bool includeMergedPipeline, UpdateMultipleRequest updateMultipleRequest, out string targetLogicalName)
        {
            string messageName = GetMessageNameFromRequest(updateMultipleRequest);
            var targets = updateMultipleRequest.Targets;
            targetLogicalName = targets.Entities.FirstOrDefault()?.LogicalName;
            IEnumerable<PluginStepDescription> stepsToExecute = null;
            if (this.Emulator.PluginSteps.TryGetValue(messageName, out var steps))
            {
                string targetName = targetLogicalName;
                stepsToExecute = steps.Where(s => s.PrimaryEntity == targetName);
            }
            var attributesInTargets = new HashSet<string>();
            foreach (var target in targets.Entities)
            {
                foreach (var attribute in target.Attributes)
                {
                    attributesInTargets.Add(attribute.Key);
                }
            }
            stepsToExecute = stepsToExecute?.Where(s => s.FilteringAttributes == null || s.FilteringAttributes.Length == 0
                    || s.FilteringAttributes.Intersect(attributesInTargets).Any());
            var stepsToExecute_multiple = stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = updateMultipleRequest,
                Targets = targets
            });
            if (!includeMergedPipeline)
            {
                return stepsToExecute_multiple;
            }
            var stepsToExecute_simple = new List<IStepTriggered>();
            foreach (var target in targets.Entities)
            {
                UpdateRequest fakeRequest = new UpdateRequest
                {
                    Target = target
                };
                stepsToExecute_simple.AddRange(GetStepsToExecute_Simple(false, fakeRequest, out _));
            }
            return stepsToExecute_multiple.Union(stepsToExecute_simple);
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Simple(bool includeMergedPipeline, OrganizationRequest request, out string targetLogicalName)
        {
            string messageName = GetMessageNameFromRequest(request);
            IEnumerable<IStepTriggered> stepsToExecute_multiple = null;
            IEnumerable<PluginStepDescription> stepDescriptions = null;
            EntityReference targetRef = null;
            Entity target = null;
            targetLogicalName = null;
            if (this.Emulator.PluginSteps.TryGetValue(messageName, out var steps))
            {
                stepDescriptions = steps;
            }
            switch (request)
            {
                case CreateRequest createRequest:
                    target = createRequest.Target;
                    var fakeCreateMultipleRequest = new CreateMultipleRequest()
                    {
                        RequestName = "CreateMultiple",
                        Targets = new EntityCollection(new[] { target })
                    };
                    stepsToExecute_multiple = GetStepsToExecute_Multiple(false, fakeCreateMultipleRequest, out _);
                    break;
                case UpsertRequest upsertRequest:
                    target = upsertRequest.Target;
                    var fakeUpsertMultipleRequest = new UpsertMultipleRequest()
                    {
                        RequestName = "UpsertMultiple",
                        Targets = new EntityCollection(new[] { target })
                    };
                    stepsToExecute_multiple = GetStepsToExecute_Multiple(false, fakeUpsertMultipleRequest, out _);
                    //TODO : en cas d'update il faudrait appliquer les filtering attributes
                    break;
                case RetrieveRequest retrieveRequest:
                    targetRef = retrieveRequest.Target;
                    break;
                case UpdateRequest updateRequest:
                    target = updateRequest.Target;
                    var fakeUpdateMultipleRequest = new UpsertMultipleRequest()
                    {
                        RequestName = "UpsertMultiple",
                        Targets = new EntityCollection(new[] { target })
                    };
                    stepsToExecute_multiple = GetStepsToExecute_Multiple(false, fakeUpdateMultipleRequest, out _);
                    stepDescriptions = stepDescriptions?.Where(s => s.FilteringAttributes == null || s.FilteringAttributes.Length == 0
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
                        throw new NotImplementedException(retrieveMultipleRequest.Query.GetType().Name);
                    }
                    break;
            }
            if (targetRef == null && target != null)
            {
                if (target.Id == Guid.Empty && target.KeyAttributes.Count == 0)
                {
                    var entityMetadata = this.Emulator.DataCache.GetMetadataEntityWithAttributes(target.LogicalName);
                    AttributeMetadata pkAttribute = entityMetadata.Attributes.First(x => x.IsPrimaryId == true);
                    target.Id = target.GetAttributeValue<Guid>(pkAttribute.LogicalName);

                    //TODO: check alternate keys
                }

                targetRef = target.ToEntityReference();
            }

            targetLogicalName = targetLogicalName ?? targetRef?.LogicalName;

            if (targetLogicalName != null)
            {
                string logicalName = targetLogicalName;
                stepDescriptions = stepDescriptions?.Where(s => s.PrimaryEntity == logicalName);
            }
            var stepsToExecuteSimple = stepDescriptions?.Select(s => (IStepTriggered)new SimpleStepTriggered()
            {
                StepDescription = s,
                TargetReference = targetRef,
                OrganizationRequest = request
            });
            if (stepsToExecute_multiple == null || !includeMergedPipeline)
                return stepsToExecuteSimple;
            return stepsToExecuteSimple.Union(stepsToExecute_multiple);
        }



    }
}
