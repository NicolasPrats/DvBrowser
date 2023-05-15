using System;
using System.Collections.Generic;
using System.Linq;
using Dataverse.Plugin.Emulator.Context;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Dataverse.Plugin.Emulator.Steps;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Plugin.Emulator.Services
{
    public class OrganizationServiceWithEmulatedPlugins
        : IOrganizationService
    {

        private IOrganizationService InnerService { get; }
        internal PluginEmulator Emulator { get; }
        internal EmulatedPluginContext CurrentContext { get; }
        internal WhoAmIResponse WhoAmIResponse { get; }
        internal OrganizationDetail CurrentOrganization { get; }

        internal OrganizationServiceWithEmulatedPlugins(IOrganizationService innerService, PluginEmulator manager, EmulatedPluginContext currentContext)
        {
            this.InnerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
            this.Emulator = manager ?? throw new ArgumentNullException(nameof(manager));
            this.CurrentContext = currentContext;

            WhoAmIRequest request = new WhoAmIRequest();
            this.WhoAmIResponse = (WhoAmIResponse)InnerExecute(request);

            RetrieveCurrentOrganizationRequest orgRequest = new RetrieveCurrentOrganizationRequest();
            this.CurrentOrganization = ((RetrieveCurrentOrganizationResponse)InnerExecute(orgRequest)).Detail;


        }

        private OrganizationResponse InnerExecute(OrganizationRequest request)
        {
            request.Parameters.Add("BypassCustomPluginExecution", true);
            return this.InnerService.Execute(request);
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            AssociateRequest request = new AssociateRequest()
            {
                Target = new EntityReference(entityName, entityId),
                Relationship = relationship,
                RelatedEntities = relatedEntities
            };
            Execute(request);
        }

        public Guid Create(Entity entity)
        {
            CreateRequest request = new CreateRequest()
            {
                Target = entity
            };
            var response = (CreateResponse)Execute(request);
            return response.id;
        }

        public void Delete(string entityName, Guid id)
        {
            DeleteRequest request = new DeleteRequest()
            {
                Target = new EntityReference(entityName, id)
            };
            Execute(request);

        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            DisassociateRequest request = new DisassociateRequest()
            {
                Target = new EntityReference(entityName, entityId),
                Relationship = relationship,
                RelatedEntities = relatedEntities
            };
            Execute(request);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            return this.ExecuteWithTree(request, new ExecutionTreeNode());
        }

        public OrganizationResponse ExecuteWithTree(OrganizationRequest request, ExecutionTreeNode executionTreeNode)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (executionTreeNode.Type != ExecutionTreeNodeType.NotInitialized)
            {
                throw new ArgumentException(nameof(executionTreeNode));
            }

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

            IEnumerable<PluginStepDescription> stepsToExecute = null;
            EntityReference targetRef = null;
            Entity target = null;
            string targetLogicalName = null;
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
                    //TODO find attribute pk name instead of a random column of type guid
                    foreach (var kvp in target.Attributes) {
                        if (kvp.Value is Guid id)
                        {
                            target.Id = id;
                        }
                    }

                }
                targetRef = target.ToEntityReference();
            }
            //TODO pour create and update on suppose que l'id de la target est défini
            //mais théoriquement il pourrait être à null et l'attribut guid de la target défini
            targetLogicalName = targetLogicalName ?? targetRef?.LogicalName;
            executionTreeNode.Type = ExecutionTreeNodeType.Message;
            executionTreeNode.Title = request.RequestName;
            if (targetLogicalName != null)
            {
                executionTreeNode.Title += " " + targetLogicalName;
                stepsToExecute = stepsToExecute?.Where(s => s.PrimaryEntity == targetLogicalName);
            }

            this.CurrentContext?.ExecutionTreeRoot.ChildNodes.Add(executionTreeNode);
            if (stepsToExecute?.Any() == true)
            {
                return Execute(executionTreeNode, request, targetRef, stepsToExecute);
            }
            else
            {
                return this.InnerExecute(request);
            }
        }

        private OrganizationResponse Execute(ExecutionTreeNode treeNode, OrganizationRequest request, EntityReference target, IEnumerable<PluginStepDescription> steps)
        {
            ParameterCollection sharedVariables = new ParameterCollection();
            var preValidateSteps = steps.Where(s => s.Stage == 10);
            var preImages = GetImages(target, steps, 0);
            foreach (var step in preValidateSteps.OrderBy(s => s.Rank))
            {
                ExecuteStep(treeNode, step, request, null, sharedVariables, preImages[step], null);
            }
            var preExecuteSteps = steps.Where(s => s.Stage == 20);
            foreach (var step in preExecuteSteps.OrderBy(s => s.Rank))
            {
                ExecuteStep(treeNode, step, request, null, sharedVariables, preImages[step], null);
            }


            treeNode.ChildNodes.Add(new ExecutionTreeNode("30 Execute Operation", ExecutionTreeNodeType.InnerOperation));
            var response = this.InnerExecute(request);

            if (request is CreateRequest createRequest && response is CreateResponse createResponse)
            {
                target.Id = createResponse.id;
                createRequest.Target.Id = createResponse.id;
            }

            var postExecuteSteps = steps.Where(s => s.Stage == 40);
            var postImages = GetImages(target, postExecuteSteps, 1);
            foreach (var step in postExecuteSteps.OrderBy(s => s.IsAsynchronous ? 1 : 0).ThenBy(s => s.Rank))
            {
                ExecuteStep(treeNode, step, request, response, sharedVariables, preImages[step], postImages[step]);
            }
            return response;
        }

        private Dictionary<PluginStepDescription, EntityImageCollection> GetImages(EntityReference target, IEnumerable<PluginStepDescription> steps, int imageType)
        {
            var images = new Dictionary<PluginStepDescription, EntityImageCollection>();
            foreach (var step in steps)
            {
                images[step] = new EntityImageCollection();
                foreach (var image in step.Images.Where(i => i.ImageType == imageType || i.ImageType == 2))
                {
                    if (target == null)
                    {
                        throw new NotSupportedException();
                    }
                    ColumnSet columns;
                    if (image.Attributes == null || image.Attributes.Length == 0)
                    {
                        columns = new ColumnSet(true);
                    }
                    else
                    {
                        columns = new ColumnSet(image.Attributes);
                    }
                    //TODO utiliser InnerExecute
                    var record = this.InnerService.Retrieve(target.LogicalName, target.Id, columns);
                    images[step][image.EntityAlias] = record;
                }
            }
            return images;
        }

        //[DebuggerStepThrough()]
        private void ExecuteStep(ExecutionTreeNode executionTreeNode, PluginStepDescription step, OrganizationRequest request, OrganizationResponse response, ParameterCollection sharedVariables, EntityImageCollection preImages, EntityImageCollection postImages)
        {
            string title = step.Stage + " " + step.MessageName + " " + (step.IsAsynchronous ? "Async " : " ") + step.EventHandler;
            var stepExecutionTreeNode = new ExecutionTreeNode(title, ExecutionTreeNodeType.Step);
            executionTreeNode.ChildNodes.Add(stepExecutionTreeNode);
            var context = GenerateContext(stepExecutionTreeNode, step, request, response, sharedVariables, preImages, postImages);

            var serviceProvider = new EmulatedPluginServiceProvider();
            serviceProvider.AddService(context);
            serviceProvider.AddService(new EmulatedPluginTracingService(stepExecutionTreeNode));
            serviceProvider.AddService(new EmulatedPluginsServiceFactory(this.Emulator, context));
            serviceProvider.AddService(new EmulatedPluginServiceProvider());
            serviceProvider.AddService(new EmulatedPluginLogger(stepExecutionTreeNode));
            serviceProvider.AddService(new EmulatedServiceEndpointNotificationService());

            var plugin = this.Emulator.PluginCache.GetPlugin(step);
            //System.Diagnostics.Debugger.Launch();
            //System.Diagnostics.Debugger.Break();

            plugin.Execute(serviceProvider);
        }


        private EmulatedPluginContext GenerateContext(ExecutionTreeNode executionTreeNode, PluginStepDescription step, OrganizationRequest request, OrganizationResponse response, ParameterCollection sharedVariables, EntityImageCollection preImages, EntityImageCollection postImages)
        {
            var newContext = new EmulatedPluginContext
            {
                BusinessUnitId = this.WhoAmIResponse.BusinessUnitId,
                CorrelationId = this.CurrentContext == null ? Guid.NewGuid() : this.CurrentContext.CorrelationId,
                Depth = this.CurrentContext == null ? 1 : this.CurrentContext.Depth + 1,
                InitiatingUserId = this.CurrentContext == null ? this.WhoAmIResponse.UserId : this.CurrentContext.InitiatingUserId,
                InputParameters = request.Parameters,
                Mode = step.IsAsynchronous ? 1 : 0,
                MessageName = step.MessageName,
                OrganizationName = this.CurrentOrganization.UniqueName,
                OperationCreatedOn = DateTime.UtcNow,
                OrganizationId = this.WhoAmIResponse.OrganizationId,
                OutputParameters = MapResponseParameters(response),
                ParentContext = this.CurrentContext,
                PostEntityImages = postImages,
                PreEntityImages = preImages,
                PrimaryEntityName = step.PrimaryEntity,
                RequestId = Guid.NewGuid(),
                SecondaryEntityName = step.SecondaryEntity,
                SharedVariables = sharedVariables,
                Stage = step.Stage,
                UserId = this.WhoAmIResponse.UserId,
                IsInTransaction = this.CurrentContext != null && this.CurrentContext.IsInTransaction || step.Stage != 10,
                ExecutionTreeRoot = executionTreeNode
            };

            return newContext;
        }

        private ParameterCollection MapResponseParameters(OrganizationResponse response)
        {
            if (response == null)
                return null;
            if (!ResponsePropertyMapping.Mapping.TryGetValue(response.GetType(), out var mapping))
                return response.Results;
            var parameters = new ParameterCollection();
            foreach (var parameter in response.Results)
            {
                if (parameter.Key != mapping.Item1)
                {
                    parameters.Add(parameter);
                }
                else
                {
                    parameters.Add(mapping.Item2, parameter.Value);
                }
            }
            return parameters;
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            RetrieveRequest request = new RetrieveRequest()
            {
                Target = new EntityReference(entityName, id),
                ColumnSet = columnSet
            };
            var response = (RetrieveResponse)Execute(request);
            return response.Entity;
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            RetrieveMultipleRequest request = new RetrieveMultipleRequest()
            {
                Query = query
            };
            var response = (RetrieveMultipleResponse)Execute(request);
            return response.EntityCollection;
        }

        public void Update(Entity entity)
        {
            UpdateRequest request = new UpdateRequest()
            {
                Target = entity
            };
            Execute(request);
        }
    }
}
