using System;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Services
{
    //TODO : Try to get real values ?
    internal class EmulatedEnvironmentService
        : IEnvironmentService
    {
        public Uri AzureAuthorityHost => new Uri("https://login.microsoftonline.com/");

        public string Geo => "Running locally in DvBrowser";

        public string AzureRegionName => "Running locally in DvBrowser";
    }
}
