using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dataverse.Browser.Configuration
{
    internal class EnvironnementConfiguration
    {
        public string Name { get; set; }
        public string DataverseHost { get; set; }
        public string[] PluginAssemblies { get; set; }
        public StepBehavior StepBehavior { get; set; }
        public Guid Id {get; set; }
    }
}
