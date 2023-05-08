using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using AutoUpdaterDotNET;
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
            DataverseContext context = null;
            try
            {
                EnsureApplicationPathExists();
                selectedEnvironment = SelectEnvironment();
                if (selectedEnvironment == null)
                    return;
                context = CreateContext(selectedEnvironment);
                if (context == null)
                    return;

                //var record = new Entity("quote", new Guid("39a0477a-c2d3-ed11-a7c6-0022489bad9d"));
                //record["statecode"] = new OptionSetValue(2);
                //record["statuscode"] = new OptionSetValue(4);
                //UpdateRequest request = new UpdateRequest()
                //{
                //    Target = record
                //};
                //var treenode = new Plugin.Emulator.ExecutionTree.ExecutionTreeNode();
                //context.ProxyForWeb.ExecuteWithTree(request, treenode);
                //context.LastRequests.AddRequest(new Requests.InterceptedWebApiRequest()
                //{
                //    Method = "Fake",
                //    ExecutionTreeRoot = treenode,
                //    ConvertedRequest = request
                //}) ;

                StartBrowser(context);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (selectedEnvironment?.StepBehavior == StepBehavior.DisableAsyncSteps)
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

        private static DataverseContext CreateContext(EnvironnementConfiguration selectedEnvironment)
        {
            var factory = new ContextFactory(selectedEnvironment);
            using (var spinnerForm = new ContextSpinner(factory, selectedEnvironment))
            {
                Application.Run(spinnerForm);
            }
            DataverseContext context = factory.GetContext();
            return context;
        }

        private static EnvironnementConfiguration SelectEnvironment()
        {
            var configuration = ConfigurationManager.LoadConfiguration();
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


        private static void StartBrowser(DataverseContext context)
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


            Application.Run(new UI.Browser(context));
        }
    }
}
