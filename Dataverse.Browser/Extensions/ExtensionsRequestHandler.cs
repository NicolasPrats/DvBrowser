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
        public const string FakeIdentifier = "dvbrowser_fakewr_";

        private BrowserContext Context { get; }
        private Dictionary<string, MemoryStream> WebResources { get; } = new Dictionary<string, MemoryStream>();


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
            foreach (XmlNode webresourceNode in webresourceNodes)
            {
                var name = webresourceNode.SelectSingleNode("Name").InnerText;
                var fileName = webresourceNode.SelectSingleNode("FileName").InnerText;
                if (fileName.StartsWith("/"))
                    fileName = fileName.Substring(1);
                using (var fileStream = archive.GetEntry(fileName).Open())
                {
                    var memoryStream = new MemoryStream();
                    fileStream.CopyTo(memoryStream);
                    this.WebResources[name] = memoryStream;
                }

            }
        }

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            int index = request.Url.IndexOf(FakeIdentifier);
            if (index == -1)
                return null;
            string wrName = request.Url.Substring(index + FakeIdentifier.Length + 1);
            index = wrName.IndexOf("?");
            if (index != -1)
            {
                wrName = wrName.Substring(0, index);
            }
            if (this.WebResources.TryGetValue(wrName, out var webresource))
            {
                return new ExtensionsResourceRequestHandler(webresource);
            }
            return null;
        }
    }
}
