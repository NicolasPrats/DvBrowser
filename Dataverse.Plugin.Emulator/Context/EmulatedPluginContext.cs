using System;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Context
{

    internal class EmulatedPluginContext
        : IPluginExecutionContext5
    {

        public EmulatedPluginContext() { }

        public ExecutionTreeNode ExecutionTreeRoot { get; internal set; }

        public int Stage { get; internal set; }


        public EmulatedPluginContext ParentContext { get; internal set; }

        IPluginExecutionContext IPluginExecutionContext.ParentContext
        {
            get
            {
                return this.ParentContext;
            }
        }

        public int Mode { get; internal set; }

        public int IsolationMode => 1;

        public int Depth { get; internal set; }

        public string MessageName { get; internal set; }

        public string PrimaryEntityName { get; internal set; }

        public Guid? RequestId { get; internal set; }

        public string SecondaryEntityName { get; internal set; }

        public ParameterCollection InputParameters { get; internal set; }

        public ParameterCollection OutputParameters { get; internal set; }

        public ParameterCollection SharedVariables { get; internal set; }

        public Guid UserId { get; internal set; }

        public Guid InitiatingUserId { get; internal set; }

        public Guid BusinessUnitId { get; internal set; }

        public Guid OrganizationId { get; internal set; }

        public string OrganizationName { get; internal set; }

        public Guid PrimaryEntityId { get; internal set; }

        public EntityImageCollection PreEntityImages { get; internal set; }

        public EntityImageCollection PostEntityImages { get; internal set; }

        public EntityReference OwningExtension { get; internal set; }

        public Guid CorrelationId { get; internal set; }

        public bool IsExecutingOffline => false;

        public bool IsOfflinePlayback => false;

        public bool IsInTransaction { get; internal set; }

        public Guid OperationId => this.CorrelationId; //TODO

        public DateTime OperationCreatedOn { get; internal set; }

        public Guid UserAzureActiveDirectoryObjectId { get; internal set; }

        public Guid InitiatingUserAzureActiveDirectoryObjectId { get; internal set; }

        public Guid InitiatingUserApplicationId => throw new NotImplementedException();

        public Guid PortalsContactId => throw new NotSupportedException();

        public bool IsPortalsClientCall => false;

        public Guid AuthenticatedUserId => throw new NotSupportedException();

        // https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bulk-operations?tabs=sdk
        public EntityImageCollection[] PreEntityImagesCollection => throw new NotImplementedException();

        public EntityImageCollection[] PostEntityImagesCollection => throw new NotImplementedException();

        public string InitiatingUserAgent => "Dataverse Browser";
    }
}
