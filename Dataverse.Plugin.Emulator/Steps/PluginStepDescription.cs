using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dataverse.Plugin.Emulator.Steps
{
    internal class PluginStepDescription
    {
        public Guid Id { get; set; }
        public Assembly Assembly { get; set; }
        public string MessageName { get; set; }
        public int Stage { get; set; }
        public int Rank { get; set; }
        public string[] FilteringAttributes { get; set; }
        public string EventHandler { get; set; }
        public bool IsAsynchronous { get; set; }
        public string PrimaryEntity { get; set; }
        public string SecondaryEntity { get; set; }
        public string Configuration { get; set; } //TODO
        public string SecureConfiguration { get; set; }//TODO

        public List<PluginStepImage> Images { get; } = new List<PluginStepImage>();
    }
}
