using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using CefSharp;
using Dataverse.Browser.Context;

namespace Dataverse.Browser.Requests
{
    internal class ExtensionsRequestHandler
        : IRequestHandler
    {
        private BrowserContext Context { get; }
        private Dictionary<string, string> WebResources = new Dictionary<string, string>();


        public ExtensionsRequestHandler(BrowserContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            foreach (var zipFile in Directory.GetFiles("Extensions"))
            {
                LoadWebResources(zipFile);
            }
        }

        private void LoadWebResources(string zipFile)
        {
            using (var archive = System.IO.Compression.ZipFile.OpenRead(zipFile))
            {
                var customisations = archive.Entries.FirstOrDefault(e => e.FullName.ToLowerInvariant() == "customizations.xml");
                using (var stream = customisations.Open())
                {
                    LoadWebResources(archive, stream);
                }
            }

        }

        private void LoadWebResources(ZipArchive archive, Stream custosStream)
        {
            XmlNamespaceManager mgr = new XmlNamespaceManager(new NameTable());
            mgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(custosStream);
            var webresourceNodes = xmlDocument.SelectNodes("//WebResource", mgr);
        }

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return null;
        }
    }
}
