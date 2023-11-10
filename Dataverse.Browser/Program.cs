using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Dataverse.Browser.Configuration;
using Dataverse.Browser.Context;
using Dataverse.Browser.UI;

namespace Dataverse.Browser
{
    internal static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!IsEditAndContinueAvailable())
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            EnvironnementConfiguration selectedEnvironment = null;
            BrowserContext context = null;
            try
            {
                EnsureApplicationPathExists();
                var configuration = ConfigurationManager.LoadConfiguration();
                selectedEnvironment = SelectEnvironment(configuration);
                if (selectedEnvironment == null)
                    return;
                context = CreateContext(selectedEnvironment);
                ConfigurationManager.SaveConfiguration(configuration);
                if (context == null)
                    return;

                StartBrowser(context);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (selectedEnvironment?.DisableAsyncSteps == true)
                {
                    context?.PluginsEmulator.ReenableAsyncSteps();
                }
            }
        }

        private static bool IsEditAndContinueAvailable()
        {
            string value = Environment.GetEnvironmentVariable("COMPLUS_FORCEENC", EnvironmentVariableTarget.Process);
            if (value == "1" || Debugger.IsAttached)
            {
                return true;
            }
            var currentProcess = Process.GetCurrentProcess();
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                ErrorDialog = true,
                FileName = currentProcess.MainModule.FileName,
                LoadUserProfile = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false
            };
            psi.EnvironmentVariables.Add("COMPLUS_FORCEENC", "1");
            Process.Start(psi);
            return false;
        }

        private static BrowserContext CreateContext(EnvironnementConfiguration selectedEnvironment)
        {
            var factory = new ContextFactory(selectedEnvironment);
            using (var spinnerForm = new ContextSpinner(factory, selectedEnvironment))
            {
                Application.Run(spinnerForm);
            }
            return factory.GetContext();
        }

        private static EnvironnementConfiguration SelectEnvironment(DataverseBrowserConfiguration configuration)
        {
            EnvironnementConfiguration selectedEnvironment;
            using (var picker = new UI.EnvironmentPicker(configuration))
            {
                Application.Run(picker);
                selectedEnvironment = picker.SelectedEnvironment;
            }

            return selectedEnvironment;
        }

        private static void EnsureApplicationPathExists()
        {
            var appDataPath = ConfigurationManager.GetApplicationDataPath();
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }


        private static void StartBrowser(BrowserContext context)
        {
#if ANYCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

            var settings = new CefSettings()
            {
                CachePath = Path.Combine(context.CachePath, "browser"),
                PersistSessionCookies = true,
                LogFile = Path.Combine(context.CachePath, "cefdebug.log"),

            };

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);


            Application.Run(new UI.BrowserWindow(context));
        }
    }
}
