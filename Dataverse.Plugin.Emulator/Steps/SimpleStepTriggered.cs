using System;
using System.Linq;
using Dataverse.Plugin.Emulator.Context;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal class SimpleStepTriggered
        : IStepTriggered
    {
        public PluginStepDescription StepDescription { get; set; }

        public OrganizationRequest OrganizationRequest { get; set; }
        public OrganizationResponse OrganizationResponse { get; private set; }
        public EntityReference TargetReference { get; set; }
        public EntityImageCollection PreImages { get; internal set; }
        public EntityImageCollection PostImages { get; internal set; }

        public void FillContext(EmulatedPluginContext newContext)
        {
            newContext.PrimaryEntityId = this.TargetReference?.Id ?? Guid.Empty;
            newContext.PostEntityImages = this.PostImages;
            newContext.PreEntityImages = this.PreImages;
        }

        public void GenerateImages(int imageType, Func<OrganizationRequest, OrganizationResponse> innerExecute)
        {
            var images = new EntityImageCollection();
            foreach (var image in this.StepDescription.Images.Where(i => i.ImageType == imageType || i.ImageType == 2))
            {
                if (this.TargetReference == null)
                {
                    throw new NotSupportedException("Images are supported only for messages with targets!");
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
                RetrieveRequest retrieveRequest = new RetrieveRequest()
                {
                    ColumnSet = columns,
                    Target = this.TargetReference
                };
                var record = ((RetrieveResponse)innerExecute(retrieveRequest)).Entity;
                images[image.EntityAlias] = record;
            }
            if (imageType == 0)
            {
                this.PreImages = images;
            }
            else
            {
                this.PostImages = images;
            }
        }

        public void SetOrganizationResponse(OrganizationResponse response)
        {
            this.OrganizationResponse = response;
        }

        public void SetOrganizationResponse(CreateResponse createResponse, Entity updatedTarget)
        {
            SetOrganizationResponse(createResponse);
            this.TargetReference.Id = createResponse.id;
            var target = ((CreateRequest)this.OrganizationRequest).Target;
            OverwriteTarget(target, updatedTarget);
        }



        public void SetOrganizationResponse(CreateMultipleResponse createMultipleResponse, EntityCollection updatedTargets)
        {
            SetOrganizationResponse(createMultipleResponse);
            if (!this.OrganizationRequest.Parameters.TryGetValue("DvBrowserTargetIndex", out var targetIndex))
            {
                throw new NotSupportedException("Expected FakeRequest with DvBrowserTargetIndex");
            }
            var target = ((CreateRequest)this.OrganizationRequest).Target;
            OverwriteTarget(target, updatedTargets[(int)targetIndex]);
        }

        private void OverwriteTarget(Entity currentTarget, Entity updatedTarget)
        {
            currentTarget.Id = updatedTarget.Id;
            foreach (var attribute in updatedTarget.Attributes)
            {
                currentTarget[attribute.Key] = attribute.Value;
            }
        }
    }
}
