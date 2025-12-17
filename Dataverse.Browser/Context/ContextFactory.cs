using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Xml;
using Dataverse.Browser.Configuration;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Utils;
using Microsoft.OData.Edm.Csdl;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace Dataverse.Browser.Context
{
    public class ContextFactory
    {
        //TODO allow ui to stop the process
        public event EventHandler<string> OnNewProgress;
        public event EventHandler<Exception> OnError;
        public event EventHandler OnFinished;
        private BrowserContext Context { get; set; }

        public ContextFactory(EnvironnementConfiguration selectedEnvironment)
        {
            this.SelectedEnvironment = selectedEnvironment ?? throw new ArgumentNullException(nameof(selectedEnvironment));
        }

        public EnvironnementConfiguration SelectedEnvironment { get; }

        private void NotifyProgress(string progress)
        {
            OnNewProgress?.Invoke(this, progress);
        }

        public BrowserContext GetContext()
        {
            return this.Context;
        }

        public void CreateContext()
        {
            try
            {
                NotifyProgress("Initializing traces");
                TraceControlSettings.TraceLevel = SourceLevels.All;
                Trace.AutoFlush = true;
                string logPath = Path.Combine(this.SelectedEnvironment.GetWorkingDirectory(), "log.txt");
                // Capture both CrmServiceClient traces and our own progress traces in the same file.
                const string BrowserTraceListenerName = "DvBrowserLog";
                var listener = new TextWriterTraceListener(logPath) { Name = BrowserTraceListenerName };
                TraceControlSettings.AddTraceListener(listener);
                if (!Trace.Listeners.OfType<TextWriterTraceListener>().Any(l => string.Equals(l.Name, BrowserTraceListenerName, StringComparison.OrdinalIgnoreCase)))
                {
                    Trace.Listeners.Add(listener);
                }
                OnNewProgress += ContextFactory_OnNewProgress;
                OnError += ContextFactory_OnError;


                string connectionString = GetConnectionString(false);
                NotifyProgress("Connecting to crm: " + connectionString);
                var client = new CrmServiceClient(connectionString);
                if (!client.IsReady)
                {
                    //Le auto ne semble par marcher tout le temps
                    connectionString = GetConnectionString(true);
                    NotifyProgress("Connecting to crm: " + connectionString);
                    client = new CrmServiceClient(connectionString);
                }

                if (client.LastCrmException != null)
                {
                    NotifyProgress("Unable to establish connection");
                    OnError?.Invoke(this, client.LastCrmException);
                    return;

                }

                if (!client.IsReady)
                {
                    NotifyProgress("Unable to establish connection");
                    OnError?.Invoke(this, new ApplicationException("Unknown error"));
                    return;
                }

                // Check for orphaned lock file from previous crash
                string workingDirectory = this.SelectedEnvironment.GetWorkingDirectory();
                var orphanedStepIds = Dataverse.Plugin.Emulator.Steps.PluginEmulator.CheckForOrphanedLockFile(workingDirectory);
                if (orphanedStepIds != null && orphanedStepIds.Count > 0)
                {
                    NotifyProgress($"Found {orphanedStepIds.Count} async steps that were disabled by a previous session");
                    var recoveryResult = System.Windows.MessageBox.Show(
                        $"DvBrowser detected that {orphanedStepIds.Count} asynchronous plugin steps were disabled by a previous session that did not close properly.\n\n" +
                        "Would you like to re-enable them now?\n\n" +
                        "Click YES to re-enable the steps.\n" +
                        "Click NO to leave them disabled (you can re-enable them manually later).",
                        "Recover Disabled Async Steps",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (recoveryResult == MessageBoxResult.Yes)
                    {
                        NotifyProgress("Re-enabling async steps from previous session...");
                        try
                        {
                            // Create a temporary emulator just to re-enable the steps
                            var tempEmulator = new Dataverse.Plugin.Emulator.Steps.PluginEmulator((callerId) =>
                            {
                                var svc = new CrmServiceClient(GetConnectionString(false));
                                if (!svc.IsReady)
                                    svc = new CrmServiceClient(GetConnectionString(true));
                                svc.CallerId = callerId;
                                return svc;
                            });
                            tempEmulator.SetCacheDirectory(workingDirectory);
                            tempEmulator.ReenableAsyncStepsByIds(orphanedStepIds);
                            NotifyProgress("Successfully re-enabled async steps");
                        }
                        catch (Exception ex)
                        {
                            NotifyProgress("Warning: Could not re-enable async steps: " + ex.Message);
                            System.Windows.MessageBox.Show(
                                $"Failed to re-enable async steps: {ex.Message}\n\n" +
                                "You may need to re-enable them manually in your Dataverse environment.",
                                "Recovery Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                }

                var emulator = InitializePluginEmulator(this.SelectedEnvironment, connectionString);

                NotifyProgress("Initializing metadata cache...");
                string cachePath = this.SelectedEnvironment.GetWorkingDirectory();
                MetadataCache metadataCache = new MetadataCache(client, cachePath, forceRefresh: false);
                NotifyProgress("Creating context...");
                BrowserContext context = new BrowserContext
                {
                    Host = this.SelectedEnvironment.DataverseHost,
                    CachePath = this.SelectedEnvironment.GetWorkingDirectory(),
                    CrmServiceClient = client,
                    HttpClient = new HttpClient(),
                    MetadataCache = metadataCache,
                    PluginsEmulator = emulator,
                    ProxyWithEmulator = emulator.CreateNewProxy()
                };

                NotifyProgress("Downloading CSDL...");
                HttpRequestMessage downloadCsdlMessage = new HttpRequestMessage(HttpMethod.Get, $"{context.WebApiBaseUrl}$metadata");
                context.AddAuthorizationHeaders(downloadCsdlMessage);

                var result = context.HttpClient.SendAsync(downloadCsdlMessage).Result;
                using (var stream = result.Content.ReadAsStreamAsync().Result)
                {
                    NotifyProgress("Parsing CSDL...");
                    context.Model = CsdlReader.Parse(XmlReader.Create(stream));
                }


                UpdateLogo(context);
                this.Context = context;
                OnFinished?.Invoke(this, EventArgs.Empty);
                return;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }

        private void UpdateLogo(BrowserContext context)
        {
            NotifyProgress("Loading logo...");
            try
            {
                HttpRequestMessage downloadThemeMessage = new HttpRequestMessage(HttpMethod.Get, $"{context.WebApiBaseUrl}/themes?$orderby=isdefaulttheme desc&$select=navbarbackgroundcolor&$expand=logoimage($select=content,name)&$top=1");
                context.AddAuthorizationHeaders(downloadThemeMessage);
                var result = context.HttpClient.SendAsync(downloadThemeMessage).Result;
                ThemeResponse theme;
                using (var stream = result.Content.ReadAsStreamAsync().Result)
                {
                    theme = JsonSerializer.Deserialize<ThemeResponse>(stream);
                }
                GenerateLogoFromTheme(theme);
            }
            catch
            {
                this.SelectedEnvironment.LogoPath = null;
            }


        }

        private void GenerateLogoFromTheme(ThemeResponse theme)
        {
            var content = theme?.value.FirstOrDefault()?.logoimage?.content;
            if (content == null)
            {
                return;
            }
            var logoStream = new MemoryStream(Convert.FromBase64String(content));
            var bitmap = new Bitmap(logoStream);
            try
            {
                int maxDim = bitmap.Height > bitmap.Width ? bitmap.Height : bitmap.Width;
                if (maxDim > 100)
                {
                    bitmap = ResizeImage(bitmap, maxDim);
                }

                bitmap = CenterAndRemoveTransparentBg(bitmap, theme.value[0].navbarbackgroundcolor);
                string logoPath = Path.Combine(this.SelectedEnvironment.GetWorkingDirectory(), "logoenv.bmp");
                bitmap.Save(logoPath, ImageFormat.Bmp);
                this.SelectedEnvironment.LogoPath = logoPath;
            }
            finally
            {
                bitmap.Dispose();
            }


        }

        private static Bitmap CenterAndRemoveTransparentBg(Bitmap bitmap, string backgroundcolor)
        {
            Bitmap target = new Bitmap(100, 100);
            Graphics g = Graphics.FromImage(target);
            var pen = new Pen(ColorTranslator.FromHtml(backgroundcolor), 200);
            g.DrawRectangle(pen, 0, 0, target.Width, target.Height);
            g.DrawImage(bitmap, (target.Width - bitmap.Width) / 2, (target.Height - bitmap.Height) / 2);
            g.Dispose();
            bitmap.Dispose();
            bitmap = target;
            return bitmap;
        }

        private static Bitmap ResizeImage(Bitmap bitmap, int maxDim)
        {
            var ratio = maxDim / 100.0;
            var newBitmap = new Bitmap(bitmap, (int)(bitmap.Width / ratio), (int)(bitmap.Height / ratio));
            bitmap.Dispose();
            bitmap = newBitmap;
            return bitmap;
        }

        private class ThemeResponse
        {
            internal class LogoImage
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
                public string content { get; set; }
            }
            internal class Theme
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
                public string navbarbackgroundcolor { get; set; }
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
                public LogoImage logoimage { get; set; }
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
            public Theme[] value { get; set; }
        }

        private void ContextFactory_OnError(object sender, Exception e)
        {
            Trace.Write(e.ToString());
        }

        private void ContextFactory_OnNewProgress(object sender, string status)
        {
            Trace.Write(status);
        }



        private string GetConnectionString(bool forceLogin)
        {
            string hostname = this.SelectedEnvironment.DataverseHost;
            string loginPrompt = "Auto";
            string tokenPath = Path.Combine(this.SelectedEnvironment.GetWorkingDirectory(), "token.dat");
            if (forceLogin)
            {
                loginPrompt = "Always";
                if (File.Exists(tokenPath))
                {
                    File.Delete(tokenPath);
                }
            }
            return $"AuthType=OAuth;Url=https://{hostname};AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;TokenCacheStorePath={tokenPath};LoginPrompt={loginPrompt};RequireNewInstance=true;";
        }

        private PluginEmulator InitializePluginEmulator(EnvironnementConfiguration environnementConfiguration, string connectionString)
        {
            NotifyProgress("Instantiating PluginEmulator");
            var emulator = new PluginEmulator((callerId) =>
            {
                var svc = new CrmServiceClient(connectionString)
                {
                    CallerId = callerId
                };
                if (svc.LastCrmException != null)
                {
                    throw new ApplicationException("Unable to connect", svc.LastCrmException);
                }
                svc.BypassPluginExecution = true;
                return (IOrganizationService)svc.OrganizationWebProxyClient ?? svc.OrganizationServiceProxy;
            }
            );

            // Set cache directory for shadow copies
            emulator.SetCacheDirectory(environnementConfiguration.GetWorkingDirectory());

            foreach (var pluginPath in environnementConfiguration.PluginAssemblies)
            {
                NotifyProgress("Loading plugins: " + pluginPath);
                bool allStepsLoaded = emulator.AddPluginAssembly(pluginPath);
                if (!allStepsLoaded)
                {
                    // Just log that some steps were skipped - don't show dialog
                    // Steps with unsupported features (configuration, impersonation) are already logged during load
                    System.Diagnostics.Trace.WriteLine("[PluginEmulator] Some steps were skipped due to unsupported features (configuration/impersonation)");
                }
            }
            if (environnementConfiguration.DisableAsyncSteps)
            {
                NotifyProgress("Disabling async steps");
                try
                {
                    emulator.SetCacheDirectory(environnementConfiguration.GetWorkingDirectory());
                    emulator.DisableAyncSteps();
                }
                catch (Exception ex)
                {
                    // Keep going even if the org refuses the update (e.g. network/firewall/timeouts).
                    NotifyProgress("Could not disable async steps: " + ex.Message);
                }
            }
            return emulator;
        }
    }
}
