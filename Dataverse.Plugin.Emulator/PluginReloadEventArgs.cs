using System;

namespace Dataverse.Plugin.Emulator
{
    public class PluginReloadEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public Exception Error { get; set; }
        public DateTime ReloadTime { get; set; }
        public string AssemblyPath { get; set; }
    }
}
