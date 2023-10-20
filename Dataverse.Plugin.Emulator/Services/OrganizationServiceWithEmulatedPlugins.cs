using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private EmulatorOptions EmulatorOptions { get; }
        private PluginEmulator Emulator { get; }
        private EmulatedPluginContext CurrentContext { get; }
        private WhoAmIResponse WhoAmIResponse { get; }
        private static OrganizationDetail CurrentOrganization { get; set; }

        internal OrganizationServiceWithEmulatedPlugins(IOrganizationService innerService, PluginEmulator emulator, EmulatedPluginContext currentContext, EmulatorOptions emulatorOptions)
        {
            this.InnerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
            this.Emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
            this.CurrentContext = currentContext;

            WhoAmIRequest request = new WhoAmIRequest();
            this.WhoAmIResponse = (WhoAmIResponse)InnerExecute(request);

            if (CurrentOrganization == null)
            {
                RetrieveCurrentOrganizationRequest orgRequest = new RetrieveCurrentOrganizationRequest();
                CurrentOrganization = ((RetrieveCurrentOrganizationResponse)InnerExecute(orgRequest)).Detail;
            }
            this.EmulatorOptions = emulatorOptions ?? throw new ArgumentNullException(nameof(emulatorOptions));
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
            return ExecuteWithTree(request, new ExecutionTreeNode());
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
            var stepsIdentificationService = new StepsIdentificationService(this.Emulator);
            var stepsToExecute = stepsIdentificationService.GetStepsToExecute(request, out var targetLogicalName);

            executionTreeNode.Type = ExecutionTreeNodeType.Message;
            executionTreeNode.Title = request.RequestName;
            if (targetLogicalName != null)
            {
                executionTreeNode.Title += " " + targetLogicalName;
            }

            this.CurrentContext?.ExecutionTreeRoot.ChildNodes.Add(executionTreeNode);
            if (stepsToExecute?.Any() == true)
            {
                return Execute(executionTreeNode, request, stepsToExecute);
            }
            else
            {
                return InnerExecute(request);
            }
        }

        private OrganizationResponse Execute(ExecutionTreeNode treeNode, OrganizationRequest request, IEnumerable<IStepTriggered> steps)
        {
            ParameterCollection sharedVariables = new ParameterCollection();
            var preValidateSteps = steps.Where(s => s.StepDescription.Stage == 10);
            foreach (var step in preValidateSteps.OrderBy(s => s.StepDescription.Rank))
            {
                step.GenerateImages(0, InnerExecute);
                ExecuteStep(sharedVariables, treeNode, step);
            }
            var preExecuteSteps = steps.Where(s => s.StepDescription.Stage == 20);
            foreach (var step in preExecuteSteps.OrderBy(s => s.StepDescription.Rank))
            {
                step.GenerateImages(0, InnerExecute);
                ExecuteStep(sharedVariables, treeNode, step);
            }

            treeNode.ChildNodes.Add(new ExecutionTreeNode("30 Execute Operation", ExecutionTreeNodeType.InnerOperation));
            var operationSteps = steps.Where(s => s.StepDescription.Stage == 30);
            OrganizationResponse response;
            if (operationSteps.Any())
            {
                response = new OrganizationResponse
                {
                    ResponseName = request.RequestName,
                    Results = new ParameterCollection()
                };
                foreach (var step in operationSteps.OrderBy(s => s.StepDescription.Rank))
                {
                    step.SetOrganizationResponse(response);
                    ExecuteStep(sharedVariables, treeNode, step);
                }
            }
            else
            {
                response = InnerExecute(request);
                //TODO merged plugins ?
                // Il faut soit fusionner les réponses (dans le cas simple -> multiple)
                // soit splitter les réponses (dans le cas multiple -> simple)

                if (response is CreateResponse createResponse)
                {
                    RetrieveRequest retrieveUpdatedTargetRequest = new RetrieveRequest
                    {
                        ColumnSet = new ColumnSet(true),
                        Target = new EntityReference(((CreateRequest)request).Target.LogicalName, createResponse.id)
                    };
                    var retrieveUpdatedTargetResponse = (RetrieveResponse)InnerExecute(retrieveUpdatedTargetRequest);
                    var updatedTarget = retrieveUpdatedTargetResponse.Entity;
                    foreach (var step in steps)
                    {
                        step.SetOrganizationResponse(createResponse, updatedTarget);
                    }
                }
                else
                {

                    foreach (var step in steps)
                    {
                        step.SetOrganizationResponse(response);
                    }
                }
            }

            var postExecuteSteps = steps.Where(s => s.StepDescription.Stage == 40);
            foreach (var step in postExecuteSteps.OrderBy(s => s.StepDescription.IsAsynchronous ? 1 : 0).ThenBy(s => s.StepDescription.Rank))
            {
                step.GenerateImages(1, InnerExecute);
                ExecuteStep(sharedVariables, treeNode, step);
            }
            return response;
        }

        [System.Diagnostics.DebuggerStepThrough()]
        private void ExecuteStep(ParameterCollection sharedVariables, ExecutionTreeNode executionTreeNode, IStepTriggered step)
        {
            string title = step.StepDescription.Stage + " " + step.StepDescription.MessageName + " " + (step.StepDescription.IsAsynchronous ? "Async " : " ") + step.StepDescription.EventHandler;
            var stepExecutionTreeNode = new ExecutionTreeNode(title, ExecutionTreeNodeType.Step);
            executionTreeNode.ChildNodes.Add(stepExecutionTreeNode);
            var context = GenerateContext(sharedVariables, stepExecutionTreeNode, step);

            var serviceProvider = new EmulatedPluginServiceProvider();
            serviceProvider.AddService(context);
            serviceProvider.AddService(new EmulatedPluginTracingService(stepExecutionTreeNode));
            serviceProvider.AddService(new EmulatedPluginsServiceFactory(this.Emulator, context));
            serviceProvider.AddService(new EmulatedPluginServiceProvider());
            serviceProvider.AddService(new EmulatedPluginLogger(stepExecutionTreeNode));
            serviceProvider.AddService(new EmulatedServiceEndpointNotificationService());

            var plugin = this.Emulator.PluginCache.GetPlugin(step.StepDescription);

            if (this.EmulatorOptions.BreakBeforeExecutingPlugins && Debugger.IsAttached)
            {
                Debug.Print(title);
                Debug.Print("\"Step into\" to debug the plugin");
                Debugger.Break();
            }

            plugin.Execute(serviceProvider);
        }


        private EmulatedPluginContext GenerateContext(ParameterCollection sharedVariables, ExecutionTreeNode executionTreeNode, IStepTriggered step)
        {
            var newContext = new EmulatedPluginContext
            {
                BusinessUnitId = this.WhoAmIResponse.BusinessUnitId,
                CorrelationId = this.CurrentContext == null ? Guid.NewGuid() : this.CurrentContext.CorrelationId,
                Depth = this.CurrentContext == null ? 1 : this.CurrentContext.Depth + 1,
                InitiatingUserId = this.CurrentContext == null ? this.WhoAmIResponse.UserId : this.CurrentContext.InitiatingUserId,
                InputParameters = step.OrganizationRequest.Parameters,
                Mode = step.StepDescription.IsAsynchronous ? 1 : 0,
                MessageName = step.StepDescription.MessageName,
                OrganizationName = CurrentOrganization.UniqueName,
                OperationCreatedOn = DateTime.UtcNow,
                OrganizationId = this.WhoAmIResponse.OrganizationId,
                OutputParameters = MapResponseParameters(step.OrganizationResponse),
                OwningExtension = new EntityReference("sdkmessageprocessingstep", step.StepDescription.Id),
                ParentContext = this.CurrentContext,
                PrimaryEntityName = step.StepDescription.PrimaryEntity,
                RequestId = Guid.NewGuid(),
                SecondaryEntityName = step.StepDescription.SecondaryEntity,
                SharedVariables = sharedVariables,
                Stage = step.StepDescription.Stage,
                UserId = this.WhoAmIResponse.UserId,
                IsInTransaction = (this.CurrentContext != null && this.CurrentContext.IsInTransaction) || step.StepDescription.Stage != 10,
                ExecutionTreeRoot = executionTreeNode
            };
            step.FillContext(newContext);
            newContext.UserAzureActiveDirectoryObjectId = this.Emulator.DataCache.GetAzureADIdFromSystemUserId(newContext.UserId);
            newContext.InitiatingUserAzureActiveDirectoryObjectId = this.Emulator.DataCache.GetAzureADIdFromSystemUserId(newContext.InitiatingUserId);
            return newContext;
        }

        private ParameterCollection MapResponseParameters(OrganizationResponse response)
        {
            if (response == null)
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null. We really want return null here
                return null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
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
