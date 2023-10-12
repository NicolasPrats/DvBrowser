using System;
using Dataverse.Plugin.Emulator.Context;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal interface IStepTriggered
    {
        PluginStepDescription StepDescription { get; set; }

        OrganizationRequest OrganizationRequest { get; set; }
        OrganizationResponse OrganizationResponse { get; }

        void FillContext(EmulatedPluginContext newContext);
        void GenerateImages(int imageType, Func<OrganizationRequest, OrganizationResponse> innerExecute);
        void SetOrganizationResponse(OrganizationResponse response);
    }
}
