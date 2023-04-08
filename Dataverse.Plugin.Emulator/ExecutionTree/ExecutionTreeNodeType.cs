using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dataverse.Plugin.Emulator.ExecutionTree
{
    public enum ExecutionTreeNodeType {
    
       NotInitialized,
       Message,
       Step,
       InnerOperation
    }
}
