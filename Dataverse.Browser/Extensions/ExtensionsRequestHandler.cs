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

        private Dictionary<string, byte[]> WebResources { get; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public ExtensionsRequestHandler()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Backward compatible lookup:
            // - "Extensions" relative to the app base directory
            // - "Extensions" relative to current working directory (legacy)
            // - zipped solutions copied next to the EXE (current csproj behavior)
            var zipFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var baseExtensions = Path.Combine(baseDir, "Extensions");
            if (Directory.Exists(baseExtensions))
            {
                foreach (var file in Directory.GetFiles(baseExtensions, "*.zip"))
                {
                    zipFiles.Add(file);
                }
            }

            if (Directory.Exists("Extensions"))
            {
                foreach (var file in Directory.GetFiles("Extensions", "*.zip"))
                {
                    zipFiles.Add(Path.GetFullPath(file));
                }
            }

            if (Directory.Exists(baseDir))
            {
                foreach (var file in Directory.GetFiles(baseDir, "*.zip"))
                {
                    zipFiles.Add(file);
                }
            }

            foreach (var zipFile in zipFiles)
            {
                try
                {
                    LoadWebResources(zipFile);
                }
                catch
                {
                    // Best effort: ignore bad zip files.
                }
            }

            System.Diagnostics.Trace.WriteLine($"[ExtensionsRequestHandler] Loaded {this.WebResources.Count} webresource(s) from {zipFiles.Count} zip file(s). BaseDir='{baseDir}'");
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
                    using (var memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        this.WebResources[NormalizeWebResourceName(name)] = memoryStream.ToArray();
                    }
                }

            }
        }

        public bool TryHandle(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2Environment environment)
        {
            if (e?.Request?.Uri == null)
                return false;

            Uri uri;
            if (Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out uri))
            {
                // Let Dataverse render the webresource host page (main.aspx wrapper).
                if (uri.AbsolutePath.EndsWith("/main.aspx", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var requestUri = e.Request.Uri;
            if (uri == null)
                return false;

            // Only handle actual webresource content loads.
            // UCI has other endpoints that may include the fake name inside query payloads; do not hijack them.
            if (uri.AbsolutePath.IndexOf("/webresources/", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            // Match fake webresources in the URL path: /.../webresources/dvbrowser_fakewr_/gp_/...
            var path = uri.AbsolutePath;
            int index = path.IndexOf(FakeIdentifier, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
                return false;

            string wrName = path.Substring(index + FakeIdentifier.Length);

            wrName = NormalizeWebResourceName(wrName);

            byte[] content;
            if (this.WebResources.TryGetValue(wrName, out content))
            {
                var headers = $"Content-Type: {GetContentType(wrName)}\r\n";
                // Dataverse main.aspx sometimes URL-encodes the webresource name into a single path segment,
                // which breaks relative resource loading (js/, css/). For DRB, inject a <base> tag so
                // relative assets resolve under our fake webresource prefix.
                if (IsRestBuilderEntryPoint(wrName) && IsHtml(wrName))
                {
                    try
                    {
                        var injected = TryInjectBaseHref(content, requestUri);
                        if (injected != null)
                        {
                            content = injected;
                        }
                    }
                    catch
                    {
                        // best effort
                    }
                }

                // WebView2 may read the stream asynchronously; do not reuse a shared MemoryStream instance.
                e.Response = environment.CreateWebResourceResponse(new MemoryStream(content, writable: false), 200, "OK", headers);
                if (IsRestBuilderEntryPoint(wrName))
                {
                    System.Diagnostics.Trace.WriteLine($"[ExtensionsRequestHandler] Served DRB entrypoint from zip: '{wrName}'");
                }
                return true;
            }

            System.Diagnostics.Trace.WriteLine($"[ExtensionsRequestHandler] Webresource not found: '{wrName}' (loaded: {this.WebResources.Count}) uri='{requestUri}'");
            return false;
        }

        private static bool IsRestBuilderEntryPoint(string wrName)
        {
            // Current packaged solution entrypoint.
            return string.Equals(wrName, "gp_/drb/drb_index.htm", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(wrName, "gp_/drb/drb_index.html", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHtml(string wrName)
        {
            var ext = Path.GetExtension(wrName);
            return string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] TryInjectBaseHref(byte[] htmlBytes, string requestUri)
        {
            if (htmlBytes == null || htmlBytes.Length == 0 || string.IsNullOrEmpty(requestUri))
                return null;

            // Determine the /WebResources/ base prefix from the actual request URL so this works on any org path.
            var idxWebResources = requestUri.IndexOf("/WebResources/", StringComparison.OrdinalIgnoreCase);
            if (idxWebResources < 0)
                return null;

            var basePrefix = requestUri.Substring(0, idxWebResources + "/WebResources/".Length);
            var baseHref = basePrefix + FakeIdentifier + "/gp_/drb/";

            string html;
            try
            {
                html = System.Text.Encoding.UTF8.GetString(htmlBytes);
            }
            catch
            {
                return null;
            }

            // Only inject once.
            if (html.IndexOf("<base ", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;

            var headIndex = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
            if (headIndex < 0)
                return null;

            var headClose = html.IndexOf(">", headIndex, StringComparison.OrdinalIgnoreCase);
            if (headClose < 0)
                return null;

            // Also provide a small compatibility shim: some UCI navigations host the webresource in a nested iframe
            // where `parent.Xrm` is undefined but `top.Xrm` exists. DRB checks `parent.Xrm`, so map it when possible.
            var injection =
                "\r\n    <base href=\"" + baseHref + "\">" +
                "\r\n    <script>" +
                "try{if(typeof parent!=='undefined'&&typeof parent.Xrm==='undefined'&&typeof top!=='undefined'&&typeof top.Xrm!=='undefined'){parent.Xrm=top.Xrm;}}catch(e){}" +
                "</script>";
            var updated = html.Insert(headClose + 1, injection);
            return System.Text.Encoding.UTF8.GetBytes(updated);
        }

        private static string NormalizeWebResourceName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var n = name.Trim();
            try
            {
                n = Uri.UnescapeDataString(n);
            }
            catch
            {
                // ignore bad escape sequences
            }

            while (n.StartsWith("/", StringComparison.Ordinal))
            {
                n = n.Substring(1);
            }

            // Normalize separators
            n = n.Replace('\\', '/');
            return n;
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
                case ".ico":
                    return "image/x-icon";
                case ".woff":
                    return "font/woff";
                case ".woff2":
                    return "font/woff2";
                case ".ttf":
                    return "font/ttf";
                case ".eot":
                    return "application/vnd.ms-fontobject";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
