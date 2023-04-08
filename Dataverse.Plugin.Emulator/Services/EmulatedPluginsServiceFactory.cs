using System;
using Dataverse.Plugin.Emulator.Context;
using Dataverse.Plugin.Emulator.Steps;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class EmulatedPluginsServiceFactory
        : IOrganizationServiceFactory
    {

        public EmulatedPluginsServiceFactory(PluginEmulator manager, EmulatedPluginContext parentContext)
        {
            this.Emulator = manager ?? throw new ArgumentNullException(nameof(manager));
            this.ParentContext = parentContext ?? throw new ArgumentNullException(nameof(parentContext));
        }

        public PluginEmulator Emulator { get; }
        public EmulatedPluginContext ParentContext { get; }

        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            return Emulator.CreateNewProxy(userId ?? Guid.Empty, ParentContext);
        }
    }
}