using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal class StepTriggered
    {
        public PluginStepDescription StepDescription { get; set; }

        public OrganizationRequest OrganizationRequest { get; set; }
        public OrganizationResponse OrganizationResponse { get; set; }
        public EntityReference TargetReference { get; set; }
        public EntityImageCollection PreImages { get; internal set; }
        public EntityImageCollection PostImages { get; internal set; }
    }
}
