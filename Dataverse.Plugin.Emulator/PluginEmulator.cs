using System;
using System.Collections.Generic;
using System.Reflection;
using Dataverse.Plugin.Emulator.Context;
using Dataverse.Plugin.Emulator.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Plugin.Emulator.Steps
{
    public class PluginEmulator
    {

        public EmulatorOptions EmulatorOptions { get; } = new EmulatorOptions();
        private IOrganizationService InnerServiceProxy { get; }
        internal DataCache DataCache { get; }

        private Func<Guid, IOrganizationService> ProxyFactory { get; }
        internal Dictionary<string, List<PluginStepDescription>> PluginSteps { get; } = new Dictionary<string, List<PluginStepDescription>>();
        private List<PluginStepDescription> AsynchronousSteps { get; } = new List<PluginStepDescription>();

        internal PluginInstanceCache PluginCache { get; } = new PluginInstanceCache();

        public PluginEmulator(Func<Guid, IOrganizationService> proxyFactory)
        {
            this.ProxyFactory = proxyFactory ?? throw new ArgumentNullException(nameof(proxyFactory));
            this.InnerServiceProxy = this.ProxyFactory(Guid.Empty);
            this.DataCache = new DataCache(this.InnerServiceProxy);
        }



        public bool AddPluginAssembly(string pluginPath)
        {
            bool allStepsHaveBeenLoaded = true;

            if (pluginPath == null)
            {
                throw new ArgumentNullException(nameof(pluginPath));
            }

            Assembly assembly = Assembly.LoadFrom(pluginPath);

            QueryExpression querySteps = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("stage", "rank", "filteringattributes", "eventhandlertypecode", "configuration", "impersonatinguserid", "mode", "sdkmessageprocessingstepsecureconfigid")
            };

            //Plugin Type
            LinkEntity linkToPluginType = new LinkEntity("sdkmessageprocessingstep", "plugintype", "plugintypeid", "plugintypeid", JoinOperator.Inner)
            {
                Columns = new ColumnSet("name"),
                EntityAlias = "plugintype"
            };
            //TODO : inclure la configuration et la secure config
            querySteps.LinkEntities.Add(linkToPluginType);
            //Assembly
            LinkEntity linkToAssembly = new LinkEntity("plugintype", "pluginassembly", "pluginassemblyid", "pluginassemblyid", JoinOperator.Inner);
            linkToAssembly.LinkCriteria.AddCondition("name", ConditionOperator.Equal, assembly.GetName().Name);
            linkToPluginType.LinkEntities.Add(linkToAssembly);
            //Message
            LinkEntity linkToMessage = new LinkEntity("sdkmessageprocessingstep", "sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.Inner)
            {
                Columns = new ColumnSet("name"),
                EntityAlias = "sdkmessage"
            };
            querySteps.LinkEntities.Add(linkToMessage);
            //Filter
            LinkEntity linkToFilter = new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter)
            {
                Columns = new ColumnSet("primaryobjecttypecode", "secondaryobjecttypecode"),
                EntityAlias = "filter"
            };
            querySteps.LinkEntities.Add(linkToFilter);


            var result = this.InnerServiceProxy.RetrieveMultiple(querySteps);

            foreach (var step in result.Entities)
            {

                if (step.Contains("configuration")
                    || step.Contains("sdkmessageprocessingstepsecureconfigid"))
                {
                    allStepsHaveBeenLoaded = false;
                    continue;
                    //throw new NotImplementedException("configuration in step is not implemented!");
                }

                if (step.Contains("impersonatinguserid") && step.GetAttributeValue<EntityReference>("impersonatinguserid").Id != Guid.Empty)
                {
                    allStepsHaveBeenLoaded = false;
                    continue;
                    //throw new NotImplementedException("Impersonation in step is not implemented!");
                }

                var stepDescription = new PluginStepDescription()
                {
                    Id = step.Id,
                    Assembly = assembly,
                    MessageName = (string)step.GetAttributeValue<AliasedValue>("sdkmessage.name")?.Value,
                    Stage = step.GetAttributeValue<OptionSetValue>("stage").Value,
                    Rank = step.GetAttributeValue<int>("rank"),
                    FilteringAttributes = step.GetAttributeValue<string>("filteringattributes")?.Split(','),
                    EventHandler = (string)step.GetAttributeValue<AliasedValue>("plugintype.name")?.Value,
                    IsAsynchronous = step.GetAttributeValue<OptionSetValue>("mode")?.Value == 1,
                    PrimaryEntity = (string)step.GetAttributeValue<AliasedValue>("filter.primaryobjecttypecode")?.Value,
                    SecondaryEntity = (string)step.GetAttributeValue<AliasedValue>("filter.secondaryobjecttypecode")?.Value,
                };
                if (stepDescription.SecondaryEntity != null && stepDescription.SecondaryEntity != "none")
                {
                    allStepsHaveBeenLoaded = false;
                    continue;
                    //throw new NotImplementedException("SecondaryEntity in step is not implemented!");
                }

                QueryExpression queryImages = new QueryExpression("sdkmessageprocessingstepimage");
                queryImages.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, step.Id);
                queryImages.ColumnSet = new ColumnSet("attributes", "entityalias", "imagetype");
                var images = this.InnerServiceProxy.RetrieveMultiple(queryImages).Entities;
                foreach (var image in images)
                {
                    stepDescription.Images.Add(new PluginStepImage()
                    {
                        EntityAlias = image.GetAttributeValue<string>("entityalias"),
                        Attributes = image.GetAttributeValue<string>("attributes")?.Split(','),
                        ImageType = image.GetAttributeValue<OptionSetValue>("imagetype").Value
                    });
                }
                if (!this.PluginSteps.TryGetValue(stepDescription.MessageName, out var list))
                {
                    list = new List<PluginStepDescription>();
                    this.PluginSteps[stepDescription.MessageName] = list;
                }
                list.Add(stepDescription);
                if (stepDescription.IsAsynchronous)
                {
                    this.AsynchronousSteps.Add(stepDescription);
                }
            }
            return allStepsHaveBeenLoaded;
        }

        public OrganizationServiceWithEmulatedPlugins CreateNewProxy()
        {
            return CreateNewProxy(Guid.Empty, null);
        }

        public OrganizationServiceWithEmulatedPlugins CreateNewProxy(Guid userId)
        {
            return CreateNewProxy(userId, null);
        }

        internal OrganizationServiceWithEmulatedPlugins CreateNewProxy(Guid userId, EmulatedPluginContext context)
        {
            var service = this.ProxyFactory(userId);
            return new OrganizationServiceWithEmulatedPlugins(service, this, context, this.EmulatorOptions);
        }

        public void ReenableAsyncSteps()
        {
            SetAsyncStepsState(true);
        }

        public void DisableAyncSteps()
        {
            SetAsyncStepsState(false);
            //TODO : il reste un layer unmanaged si les étapes étaient managed
        }

        private void SetAsyncStepsState(bool enabled)
        {
            foreach (var stepDescription in this.AsynchronousSteps)
            {
                Entity step = new Entity("sdkmessageprocessingstep", stepDescription.Id);
                step["statecode"] = new OptionSetValue(enabled ? 0 : 1);
                this.InnerServiceProxy.Update(step);
            }
        }


    }
}
