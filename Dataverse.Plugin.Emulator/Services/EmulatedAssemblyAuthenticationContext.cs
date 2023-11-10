using System;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class EmulatedAssemblyAuthenticationContext
        : IAssemblyAuthenticationContext2
    {
        public string AcquireToken(string authority, string resource, AuthenticationType authenticationType)
        {
            throw new NotSupportedException();
        }

        public bool ResolveAuthorityAndResourceFromChallengeUri(Uri aadChallengeUri, out string authority, out string resource)
        {
            throw new NotSupportedException();
        }
    }
}
