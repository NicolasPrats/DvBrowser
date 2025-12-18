using System;
using System.Runtime.InteropServices;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.UI
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class DrbXtbSettings
    {
        private readonly BrowserContext context;

        public DrbXtbSettings(BrowserContext context)
        {
            this.context = context;
        }

        public string Token
        {
            get
            {
                try
                {
                    return this.context?.CrmServiceClient?.CurrentAccessToken ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string Url
        {
            get
            {
                try
                {
                    var host = this.context != null ? this.context.Host : null;
                    if (string.IsNullOrWhiteSpace(host))
                        return string.Empty;
                    return "https://" + host.Trim().TrimEnd('/');
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string Version
        {
            get
            {
                try
                {
                    // Best effort: DRB expects major.minor from this string.
                    var v = this.context?.CrmServiceClient?.ConnectedOrgVersion;
                    if (v != null)
                    {
                        return v.ToString();
                    }
                }
                catch
                {
                    // ignore
                }
                return "9.2.0.0";
            }
        }
    }
}

