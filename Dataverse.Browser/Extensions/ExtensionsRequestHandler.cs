using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Microsoft.Web.WebView2.Core;

namespace Dataverse.Browser.Requests
{
    internal class ExtensionsRequestHandler
    {
        public const string FakeIdentifier = "dvbrowser_fakewr_";

        private Dictionary<string, MemoryStream> WebResources { get; } = new Dictionary<string, MemoryStream>();

        public ExtensionsRequestHandler()
        {
            if (!Directory.Exists("Extensions"))
            {
                return;
            }
            foreach (var zipFile in Directory.GetFiles("Extensions"))
            {
                LoadWebResources(zipFile);
            }
        }

        private void LoadWebResources(string zipFile)
        {
            using (var archive = ZipFile.OpenRead(zipFile))
            {
                var customisations = archive.Entries.FirstOrDefault(e => e.FullName.Equals("customizations.xml", StringComparison.OrdinalIgnoreCase));
                if (customisations == null)
                {
                    return;
                }
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
                    memoryStream.Position = 0;
                    this.WebResources[name] = memoryStream;
                }

            }
        }

        public bool TryHandle(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2Environment environment)
        {
            int index = e.Request.Uri.IndexOf(FakeIdentifier, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
                return false;
            string wrName = e.Request.Uri.Substring(index + FakeIdentifier.Length + 1);
            index = wrName.IndexOf("?", StringComparison.Ordinal);
            if (index != -1)
            {
                wrName = wrName.Substring(0, index);
            }
            if (this.WebResources.TryGetValue(wrName, out var webresource))
            {
                webresource.Position = 0;
                var headers = $"Content-Type: {GetContentType(wrName)}\r\n";
                e.Response = environment.CreateWebResourceResponse(webresource, 200, "OK", headers);
                return true;
            }
            return false;
        }

        private static string GetContentType(string name)
        {
            var ext = Path.GetExtension(name)?.ToLowerInvariant();
            switch (ext)
            {
                case ".html":
                case ".htm":
                    return "text/html";
                case ".js":
                    return "application/javascript";
                case ".css":
                    return "text/css";
                case ".json":
                    return "application/json";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".svg":
                    return "image/svg+xml";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
