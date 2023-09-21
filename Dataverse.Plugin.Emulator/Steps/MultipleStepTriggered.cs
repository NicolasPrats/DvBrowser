using System;
using System.Linq;
using Dataverse.Plugin.Emulator.Context;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal class MultipleStepTriggered
        : IStepTriggered
    {
        public PluginStepDescription StepDescription { get; set; }

        private OrganizationRequest _organizationRequest;
        public OrganizationRequest OrganizationRequest
        {
            get
            {
                return this._organizationRequest;
            }
            set
            {
                this._organizationRequest = value;
                switch (this._organizationRequest)
                {
                    case CreateMultipleRequest createMultipleRequest:
                        this.Targets = createMultipleRequest.Targets;
                        break;
                    case UpdateMultipleRequest updateMultipleRequest:
                        this.Targets = updateMultipleRequest.Targets;
                        break;
                    case UpsertMultipleRequest upsertMultipleRequest:
                        this.Targets = upsertMultipleRequest.Targets;
                        break;
                    default:
                        throw new InvalidOperationException("MultipleSteps should be used only for XXXMultiple requests, not for " + this._organizationRequest?.RequestName);
                }
            }
        }


        public OrganizationResponse OrganizationResponse { get; }
        public EntityCollection Targets { get; private set; }

        public EntityImageCollection[] PreEntityImagesCollection { get; internal set; }
        public EntityImageCollection[] PostEntityImagesCollection { get; internal set; }

        public void FillContext(EmulatedPluginContext newContext)
        {
            newContext.PreEntityImagesCollection = this.PreEntityImagesCollection;
            newContext.PostEntityImagesCollection = this.PostEntityImagesCollection;
        }

        public void GenerateImages(int imageType, Func<OrganizationRequest, OrganizationResponse> innerExecute)
        {

            var imagesCollection = new EntityImageCollection[this.Targets.Entities.Count];
            foreach (var image in this.StepDescription.Images.Where(i => i.ImageType == imageType || i.ImageType == 2))
            {
                for (int i = 0; i < imagesCollection.Length; i++)
                {
                    imagesCollection[i] = GenerateImages(image, innerExecute, this.Targets[i]);
                }
            }

            if (imageType == 0)
            {
                this.PreEntityImagesCollection = imagesCollection;
            }
            else
            {
                this.PostEntityImagesCollection = imagesCollection;
            }
        }

        private EntityImageCollection GenerateImages(PluginStepImage image, Func<OrganizationRequest, OrganizationResponse> innerExecute, Entity target)
        {
            var images = new EntityImageCollection();
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
                Target = target.ToEntityReference()
            };
            var record = ((RetrieveResponse)innerExecute(retrieveRequest)).Entity;
            images[image.EntityAlias] = record;
        }

        public void SetOrganizationResponse(OrganizationResponse response)
        {
            throw new NotImplementedException();
        }
    }
}
