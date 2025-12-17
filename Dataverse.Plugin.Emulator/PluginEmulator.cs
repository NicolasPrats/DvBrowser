using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Timers;
using Dataverse.Plugin.Emulator.Context;
using Dataverse.Plugin.Emulator.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Plugin.Emulator.Steps
{
    public class PluginEmulator
    {

        public EmulatorOptions EmulatorOptions { get; } = new EmulatorOptions();
        private IOrganizationService InnerServiceProxy { get; }
        internal DataCache DataCache { get; }

        private Func<Guid, IOrganizationService> ProxyFactory { get; }
        internal Dictionary<string, List<PluginStepDescription>> PluginSteps { get; } = new Dictionary<string, List<PluginStepDescription>>(StringComparer.OrdinalIgnoreCase);
        private List<PluginStepDescription> AsynchronousSteps { get; } = new List<PluginStepDescription>();

        internal PluginInstanceCache PluginCache { get; } = new PluginInstanceCache();

        // Hot reload infrastructure
        private string originalPluginPath;
        private string cacheDirectory;
        private FileSystemWatcher fileWatcher;
        private Timer reloadDebounceTimer;
        private bool isReloading = false;

        public event EventHandler<PluginReloadEventArgs> PluginAssemblyReloaded;

        private static bool assemblyResolverRegistered = false;

        public PluginEmulator(Func<Guid, IOrganizationService> proxyFactory)
        {
            this.ProxyFactory = proxyFactory ?? throw new ArgumentNullException(nameof(proxyFactory));
            this.InnerServiceProxy = this.ProxyFactory(Guid.Empty);
            this.DataCache = new DataCache(this.InnerServiceProxy);

            // Ensure plugin assemblies can resolve Xrm dependencies to the versions already loaded by the host.
            if (!assemblyResolverRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    if (args.Name.StartsWith("Microsoft.Xrm.Sdk", StringComparison.OrdinalIgnoreCase))
                    {
                        return typeof(IOrganizationService).Assembly;
                    }
                    if (args.Name.StartsWith("Microsoft.Crm.Sdk.Proxy", StringComparison.OrdinalIgnoreCase))
                    {
                        return AppDomain.CurrentDomain.GetAssemblies()
                            .FirstOrDefault(a => a.FullName.StartsWith("Microsoft.Crm.Sdk.Proxy", StringComparison.OrdinalIgnoreCase));
                    }
                    return null;
                };
                assemblyResolverRegistered = true;
            }
        }

        public bool AddPluginAssembly(string pluginPath)
        {
            bool allStepsHaveBeenLoaded = true;
            int loadedSteps = 0;

            if (pluginPath == null)
            {
                throw new ArgumentNullException(nameof(pluginPath));
            }

            // Store the original path for hot reload
            this.originalPluginPath = pluginPath;

            // Load assembly from bytes to completely avoid locking files
            // Use FileShare.ReadWrite to allow rebuilds while DvBrowser is running
            byte[] assemblyBytes = ReadFileWithoutLocking(pluginPath);
            byte[] pdbBytes = null;
            string pdbPath = Path.ChangeExtension(pluginPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                try
                {
                    pdbBytes = ReadFileWithoutLocking(pdbPath);
                }
                catch (IOException)
                {
                    // PDB might be locked or in use, continue without it
                }
            }

            Assembly assembly = pdbBytes != null 
                ? Assembly.Load(assemblyBytes, pdbBytes) 
                : Assembly.Load(assemblyBytes);

            QueryExpression querySteps = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("stage", "rank", "filteringattributes", "eventhandlertypecode", "configuration", "impersonatinguserid", "mode", "sdkmessageprocessingstepsecureconfigid")
            };

            //Plugin Type
            LinkEntity linkToPluginType = new LinkEntity("sdkmessageprocessingstep", "plugintype", "plugintypeid", "plugintypeid", JoinOperator.Inner)
            {
                Columns = new ColumnSet("name"),
                EntityAlias = "plugintype"
            };
            //TODO : inclure la configuration et la secure config
            querySteps.LinkEntities.Add(linkToPluginType);
            //Assembly
            LinkEntity linkToAssembly = new LinkEntity("plugintype", "pluginassembly", "pluginassemblyid", "pluginassemblyid", JoinOperator.Inner);
            linkToAssembly.LinkCriteria.AddCondition("name", ConditionOperator.Equal, assembly.GetName().Name);
            linkToPluginType.LinkEntities.Add(linkToAssembly);
            //Message
            LinkEntity linkToMessage = new LinkEntity("sdkmessageprocessingstep", "sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.Inner)
            {
                Columns = new ColumnSet("name"),
                EntityAlias = "sdkmessage"
            };
            querySteps.LinkEntities.Add(linkToMessage);
            //Filter
            LinkEntity linkToFilter = new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter)
            {
                Columns = new ColumnSet("primaryobjecttypecode", "secondaryobjecttypecode"),
                EntityAlias = "filter"
            };
            querySteps.LinkEntities.Add(linkToFilter);

            var result = this.InnerServiceProxy.RetrieveMultiple(querySteps);

            foreach (var step in result.Entities)
            {

                var pluginTypeName = step.GetAttributeValue<AliasedValue>("plugintype.name")?.Value;
                var messageName = step.GetAttributeValue<AliasedValue>("sdkmessage.name")?.Value;

                if (step.Contains("configuration")
                    || step.Contains("sdkmessageprocessingstepsecureconfigid"))
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping step {pluginTypeName} on message {messageName} because it uses configuration or secure configuration (not supported).");
                    allStepsHaveBeenLoaded = false;
                    continue;
                    //throw new NotImplementedException("configuration in step is not implemented!");
                }

                if (step.Contains("impersonatinguserid") && step.GetAttributeValue<EntityReference>("impersonatinguserid").Id != Guid.Empty)
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping step {pluginTypeName} on message {messageName} because it uses impersonation (not supported).");
                    allStepsHaveBeenLoaded = false;
                    continue;
                    //throw new NotImplementedException("Impersonation in step is not implemented!");
                }

                var stepDescription = new PluginStepDescription()
                {
                    Id = step.Id,
                    Assembly = assembly,
                    MessageName = (string)step.GetAttributeValue<AliasedValue>("sdkmessage.name")?.Value,
                    Stage = step.GetAttributeValue<OptionSetValue>("stage").Value,
                    Rank = step.GetAttributeValue<int>("rank"),
                    FilteringAttributes = step.GetAttributeValue<string>("filteringattributes")?
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim().ToLowerInvariant())
                        .ToArray(),
                    EventHandler = (string)step.GetAttributeValue<AliasedValue>("plugintype.name")?.Value,
                    IsAsynchronous = step.GetAttributeValue<OptionSetValue>("mode")?.Value == 1,
                    PrimaryEntity = (string)step.GetAttributeValue<AliasedValue>("filter.primaryobjecttypecode")?.Value,
                    SecondaryEntity = (string)step.GetAttributeValue<AliasedValue>("filter.secondaryobjecttypecode")?.Value,
                };
                if (stepDescription.SecondaryEntity != null && stepDescription.SecondaryEntity != "none")
                {
                    System.Diagnostics.Trace.WriteLine($"Skipping step {stepDescription.EventHandler} on message {stepDescription.MessageName} because it has a secondary entity ({stepDescription.SecondaryEntity}) which is not supported.");
                    allStepsHaveBeenLoaded = false;
                    continue;
                    //throw new NotImplementedException("SecondaryEntity in step is not implemented!");
                }

                QueryExpression queryImages = new QueryExpression("sdkmessageprocessingstepimage");
                queryImages.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, step.Id);
                queryImages.ColumnSet = new ColumnSet("attributes", "entityalias", "imagetype");
                var images = this.InnerServiceProxy.RetrieveMultiple(queryImages).Entities;
                foreach (var image in images)
                {
                    stepDescription.Images.Add(new PluginStepImage()
                    {
                        EntityAlias = image.GetAttributeValue<string>("entityalias"),
                        Attributes = image.GetAttributeValue<string>("attributes")?.Split(','),
                        ImageType = image.GetAttributeValue<OptionSetValue>("imagetype").Value
                    });
                }
                if (!this.PluginSteps.TryGetValue(stepDescription.MessageName, out var list))
                {
                    list = new List<PluginStepDescription>();
                    this.PluginSteps[stepDescription.MessageName] = list;
                }
                list.Add(stepDescription);
                if (stepDescription.IsAsynchronous)
                {
                    this.AsynchronousSteps.Add(stepDescription);
                }
                loadedSteps++;
            }

            // Setup file watcher for hot reload (only if not already reloading)
            if (!isReloading)
            {
                SetupFileWatcher();
            }

            return allStepsHaveBeenLoaded;
        }

        public OrganizationServiceWithEmulatedPlugins CreateNewProxy()
        {
            return CreateNewProxy(Guid.Empty, null);
        }

        public OrganizationServiceWithEmulatedPlugins CreateNewProxy(Guid userId)
        {
            return CreateNewProxy(userId, null);
        }

        internal OrganizationServiceWithEmulatedPlugins CreateNewProxy(Guid userId, EmulatedPluginContext context)
        {
            var service = this.ProxyFactory(userId);
            return new OrganizationServiceWithEmulatedPlugins(service, this, context, this.EmulatorOptions);
        }

        /// <summary>
        /// Gets the path to the lock file that tracks disabled async steps
        /// </summary>
        private string GetAsyncStepsLockFilePath()
        {
            if (string.IsNullOrEmpty(this.cacheDirectory))
                return null;
            return Path.Combine(this.cacheDirectory, "disabled_async_steps.lock");
        }

        /// <summary>
        /// Re-enables async steps that were previously disabled by this application
        /// </summary>
        public void ReenableAsyncSteps()
        {
            string lockFilePath = GetAsyncStepsLockFilePath();
            if (!string.IsNullOrEmpty(lockFilePath) && File.Exists(lockFilePath))
            {
                try
                {
                    // Read the lock file to get the specific steps we disabled
                    var disabledStepIds = ReadLockFile(lockFilePath);
                    if (disabledStepIds != null && disabledStepIds.Count > 0)
                    {
                        SetAsyncStepsStateByIds(disabledStepIds, true);
                    }
                    // Delete the lock file after successfully re-enabling
                    File.Delete(lockFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[PluginEmulator] Error re-enabling async steps from lock file: {ex.Message}");
                    // Try the fallback method
                    SetAsyncStepsState(true);
                }
            }
            else
            {
                // Fallback: re-enable all steps in AsynchronousSteps collection
                SetAsyncStepsState(true);
            }
        }

        /// <summary>
        /// Disables async steps and creates a lock file to track which steps were disabled
        /// </summary>
        public void DisableAyncSteps()
        {
            var disabledStepIds = new List<Guid>();
            foreach (var stepDescription in this.AsynchronousSteps)
            {
                disabledStepIds.Add(stepDescription.Id);
            }

            SetAsyncStepsState(false);

            // Create lock file with the list of disabled step IDs
            string lockFilePath = GetAsyncStepsLockFilePath();
            if (!string.IsNullOrEmpty(lockFilePath))
            {
                try
                {
                    WriteLockFile(lockFilePath, disabledStepIds);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[PluginEmulator] Warning: Could not create lock file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks for orphaned lock file from previous crash and offers to recover
        /// </summary>
        public static List<Guid> CheckForOrphanedLockFile(string cacheDirectory)
        {
            if (string.IsNullOrEmpty(cacheDirectory))
                return null;

            string lockFilePath = Path.Combine(cacheDirectory, "disabled_async_steps.lock");
            if (!File.Exists(lockFilePath))
                return null;

            try
            {
                return ReadLockFile(lockFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[PluginEmulator] Error reading orphaned lock file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Re-enables specific async steps by their IDs (used for crash recovery)
        /// </summary>
        public void ReenableAsyncStepsByIds(List<Guid> stepIds)
        {
            if (stepIds == null || stepIds.Count == 0)
                return;

            SetAsyncStepsStateByIds(stepIds, true);

            // Delete the lock file after recovery
            string lockFilePath = GetAsyncStepsLockFilePath();
            if (!string.IsNullOrEmpty(lockFilePath) && File.Exists(lockFilePath))
            {
                try
                {
                    File.Delete(lockFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[PluginEmulator] Warning: Could not delete lock file: {ex.Message}");
                }
            }
        }

        private void SetAsyncStepsState(bool enabled)
        {
            foreach (var stepDescription in this.AsynchronousSteps)
            {
                Entity step = new Entity("sdkmessageprocessingstep", stepDescription.Id);
                step["statecode"] = new OptionSetValue(enabled ? 0 : 1);
                this.InnerServiceProxy.Update(step);
            }
        }

        private void SetAsyncStepsStateByIds(List<Guid> stepIds, bool enabled)
        {
            foreach (var stepId in stepIds)
            {
                try
                {
                    Entity step = new Entity("sdkmessageprocessingstep", stepId);
                    step["statecode"] = new OptionSetValue(enabled ? 0 : 1);
                    this.InnerServiceProxy.Update(step);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[PluginEmulator] Error updating step {stepId}: {ex.Message}");
                }
            }
        }

        private void WriteLockFile(string filePath, List<Guid> stepIds)
        {
            var lines = new List<string>
            {
                $"# DvBrowser Disabled Async Steps Lock File",
                $"# Created: {DateTime.UtcNow:O}",
                $"# Count: {stepIds.Count}"
            };
            lines.AddRange(stepIds.Select(id => id.ToString()));
            File.WriteAllLines(filePath, lines);
        }

        private static List<Guid> ReadLockFile(string filePath)
        {
            var stepIds = new List<Guid>();
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                if (Guid.TryParse(line.Trim(), out Guid stepId))
                {
                    stepIds.Add(stepId);
                }
            }
            return stepIds;
        }

        internal OrganizationResponse InnerExecute(OrganizationRequest request)
        {
            request.Parameters.Add("BypassCustomPluginExecution", true);
            return this.InnerServiceProxy.Execute(request);
        }

        /// <summary>
        /// Sets the cache directory for shadow copies. Should be called before AddPluginAssembly.
        /// </summary>
        public void SetCacheDirectory(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
            CleanupOldShadowCopies();
        }

        /// <summary>
        /// Cleans up old shadow copy directories from previous sessions.
        /// </summary>
        private void CleanupOldShadowCopies()
        {
            if (string.IsNullOrEmpty(cacheDirectory))
                return;

            string shadowRoot = Path.Combine(cacheDirectory, "PluginShadows");
            if (Directory.Exists(shadowRoot))
            {
                try
                {
                    Directory.Delete(shadowRoot, recursive: true);
                }
                catch
                {
                    // Some files might be locked, ignore
                }
            }
        }

        /// <summary>
        /// Copies the plugin assembly to a shadow location to avoid file locks.
        /// </summary>
        private string CopyToShadowLocation(string originalPath)
        {
            if (string.IsNullOrEmpty(cacheDirectory))
            {
                // If no cache directory set, load directly (backward compatibility)
                return originalPath;
            }

            // Create unique temp directory
            string shadowDir = Path.Combine(
                cacheDirectory,
                "PluginShadows",
                Guid.NewGuid().ToString()
            );
            Directory.CreateDirectory(shadowDir);

            // Copy DLL
            string shadowDll = Path.Combine(shadowDir, Path.GetFileName(originalPath));
            File.Copy(originalPath, shadowDll, overwrite: true);

            // Copy PDB for debugging support
            string pdbPath = Path.ChangeExtension(originalPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                string shadowPdb = Path.ChangeExtension(shadowDll, ".pdb");
                File.Copy(pdbPath, shadowPdb, overwrite: true);
            }

            Trace.WriteLine($"[PluginEmulator] Copied plugin to shadow location: {shadowDll}");
            return shadowDll;
        }

        /// <summary>
        /// Reads a file without locking it, allowing other processes to write/delete it.
        /// </summary>
        private static byte[] ReadFileWithoutLocking(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        /// <summary>
        /// Sets up a FileSystemWatcher to monitor the plugin assembly for changes.
        /// </summary>
        private void SetupFileWatcher()
        {
            if (string.IsNullOrEmpty(originalPluginPath) || !File.Exists(originalPluginPath))
                return;

            // Clean up existing watcher if any
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }

            string directory = Path.GetDirectoryName(originalPluginPath);
            string fileName = Path.GetFileName(originalPluginPath);

            fileWatcher = new FileSystemWatcher(directory, fileName);
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            fileWatcher.Changed += OnPluginFileChanged;
            fileWatcher.EnableRaisingEvents = true;

            // Debounce timer (VS builds can trigger multiple events)
            reloadDebounceTimer = new Timer(2000); // 2 seconds
            reloadDebounceTimer.Elapsed += (s, e) => ReloadPluginAssembly();
            reloadDebounceTimer.AutoReset = false;

            Trace.WriteLine($"[PluginEmulator] File watcher enabled for: {originalPluginPath}");
        }

        /// <summary>
        /// Handles file change events with debouncing.
        /// </summary>
        private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
        {
            Trace.WriteLine($"[PluginEmulator] Plugin file changed: {e.FullPath}");
            
            // Restart debounce timer
            reloadDebounceTimer.Stop();
            reloadDebounceTimer.Start();
        }

        /// <summary>
        /// Reloads the plugin assembly after detecting file changes.
        /// </summary>
        private void ReloadPluginAssembly()
        {
            if (isReloading)
            {
                Trace.WriteLine("[PluginEmulator] Reload already in progress, skipping...");
                return;
            }

            isReloading = true;

            try
            {
                Trace.WriteLine("[PluginEmulator] Starting plugin reload...");

                // Temporarily disable file watcher during reload
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = false;
                }

                // Wait for file to be fully written (build might still be writing)
                if (!WaitForFileUnlock(originalPluginPath, TimeSpan.FromSeconds(10)))
                {
                    PluginAssemblyReloaded?.Invoke(this, new PluginReloadEventArgs
                    {
                        Success = false,
                        Error = new IOException("File is locked"),
                        ReloadTime = DateTime.Now,
                        AssemblyPath = originalPluginPath
                    });
                    return;
                }

                // Clear all caches
                PluginCache.Clear();
                PluginSteps.Clear();
                AsynchronousSteps.Clear();

                // Re-load the plugin assembly
                bool success = AddPluginAssembly(originalPluginPath);

                // Notify success
                PluginAssemblyReloaded?.Invoke(this, new PluginReloadEventArgs
                {
                    Success = success,
                    ReloadTime = DateTime.Now,
                    AssemblyPath = originalPluginPath
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[PluginEmulator] Plugin reload failed: {ex}");
                PluginAssemblyReloaded?.Invoke(this, new PluginReloadEventArgs
                {
                    Success = false,
                    Error = ex,
                    ReloadTime = DateTime.Now,
                    AssemblyPath = originalPluginPath
                });
            }
            finally
            {
                isReloading = false;

                // Re-enable file watcher after reload
                if (fileWatcher != null)
                {
                    fileWatcher.EnableRaisingEvents = true;
                }
            }
        }

        /// <summary>
        /// Waits for a file to be unlocked by other processes.
        /// </summary>
        private bool WaitForFileUnlock(string path, TimeSpan timeout)
        {
            DateTime start = DateTime.Now;
            while (DateTime.Now - start < timeout)
            {
                try
                {
                    // Try to open with exclusive access
                    using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true; // Successfully opened exclusively
                    }
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            return false;
        }

    }
}
