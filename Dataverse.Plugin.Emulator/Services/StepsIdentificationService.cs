﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            request = GetSpecializedMessage(request);
            switch (request)
            {
                case CreateRequest createRequest:
                    return GetStepsToExecute_Simple(createRequest, out targetLogicalName);
                case UpsertRequest upsertMultipleRequest:
                    return GetStepsToExecute_Simple(upsertMultipleRequest, out targetLogicalName);
                case UpdateRequest updateRequest:
                    return GetStepsToExecute_Simple(updateRequest, out targetLogicalName);
                case DeleteRequest deleteRequest:
                    return GetStepsToExecute_Simple(deleteRequest, out targetLogicalName);
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

        private OrganizationRequest GetSpecializedMessage(OrganizationRequest request)
        {
            if (request.GetType() != typeof(OrganizationRequest))
                return request;
            string targetTypeName = "Microsoft.Xrm.Sdk.Messages." + request.RequestName + "Request";
            var assembly = Assembly.GetAssembly(typeof(OrganizationRequest));
            var targetType = assembly.GetType(targetTypeName);
            if (targetType == null)
                return request;
            var newRequest = (OrganizationRequest)Activator.CreateInstance(targetType);
            newRequest.ExtensionData = request.ExtensionData;
            newRequest.RequestId = request.RequestId;
            newRequest.Parameters = request.Parameters;
            return newRequest;
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(CreateMultipleRequest createMultipleRequest, out string targetLogicalName)
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
            //TODO : add simple Create
            return stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = createMultipleRequest,
                Targets = targets
            });
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(UpsertMultipleRequest upsertMultipleRequest, out string targetLogicalName)
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
            //TODO : add simple Upserts
            //TODO : in case of update: filtering attributes
            return stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = upsertMultipleRequest,
                Targets = targets
            });
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Multiple(UpdateMultipleRequest updateMultipleRequest, out string targetLogicalName)
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
            //TODO : add simple update
            return stepsToExecute?.Select(s => new MultipleStepTriggered()
            {
                StepDescription = s,
                OrganizationRequest = updateMultipleRequest,
                Targets = targets
            });
        }

        private IEnumerable<IStepTriggered> GetStepsToExecute_Simple(OrganizationRequest request, out string targetLogicalName)
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
                    stepsToExecute_multiple = GetStepsToExecute_Multiple(fakeCreateMultipleRequest, out _);
                    break;
                case UpsertRequest upsertRequest:
                    target = upsertRequest.Target;
                    var fakeUpsertMultipleRequest = new UpsertMultipleRequest()
                    {
                        RequestName = "UpsertMultiple",
                        Targets = new EntityCollection(new[] { target })
                    };
                    stepsToExecute_multiple = GetStepsToExecute_Multiple(fakeUpsertMultipleRequest, out _);
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
                    stepsToExecute_multiple = GetStepsToExecute_Multiple(fakeUpdateMultipleRequest, out _);
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
            if (stepsToExecute_multiple == null)
                return stepsToExecuteSimple;
            return stepsToExecuteSimple.Union(stepsToExecute_multiple);
        }



    }
}
