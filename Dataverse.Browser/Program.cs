using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
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
                ClearLogFiles();
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

        private static void ClearLogFiles()
        {
            try
            {
                var appDataPath = ConfigurationManager.GetApplicationDataPath();
                if (!Directory.Exists(appDataPath))
                {
                    return;
                }

                var logFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in Directory.EnumerateFiles(appDataPath, "*.log", SearchOption.AllDirectories))
                {
                    logFiles.Add(file);
                }
                foreach (var file in Directory.EnumerateFiles(appDataPath, "log.txt", SearchOption.AllDirectories))
                {
                    logFiles.Add(file);
                }

                foreach (var logFile in logFiles)
                {
                    TryDeleteOrClear(logFile);
                }
            }
            catch
            {
                // ignore cleanup failures
            }
        }

        private static void TryDeleteOrClear(string logFile)
        {
            try
            {
                File.Delete(logFile);
            }
            catch (IOException)
            {
                TryTruncate(logFile);
            }
            catch (UnauthorizedAccessException)
            {
                TryTruncate(logFile);
            }
            catch
            {
                // ignore cleanup failures
            }
        }

        private static void TryTruncate(string logFile)
        {
            try
            {
                File.WriteAllText(logFile, string.Empty);
            }
            catch
            {
                // ignore cleanup failures
            }
        }

        private static void StartBrowser(BrowserContext context)
        {
            Application.Run(new UI.BrowserWindow(context));
        }
    }
}
