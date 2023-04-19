using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CefSharp;
using CefSharp.DevTools.IO;
using CefSharp.WinForms;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Dataverse.Plugin.Emulator.Steps;
using Dataverse.Browser.Context;
using Dataverse.Browser.Properties;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Dataverse.Browser.Configuration;

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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            var appDataPath = ConfigurationManager.GetApplicationDataPath();
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            var configuration = ConfigurationManager.LoadConfiguration();
            EnvironnementConfiguration selectedEnvironment;
            using (var picker = new UI.EnvironmentPicker(configuration))
            {
                Application.Run(picker);
                selectedEnvironment = picker.SelectedEnvironment;
            }
            if (selectedEnvironment == null)
                return;

            DataverseContext context = (new ContextFactory(selectedEnvironment)).CreateContext();

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
            try
            {
                StartBrowser(context);
            }
            finally
            {
                if (selectedEnvironment.StepBehavior == StepBehavior.DisableAsyncSteps)
                {
                    context.PluginsEmulator.ReenableAsyncSteps();
                }
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
