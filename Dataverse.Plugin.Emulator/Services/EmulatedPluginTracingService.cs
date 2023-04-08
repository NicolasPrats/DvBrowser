using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class EmulatedPluginTracingService
        : ITracingService
    {

        public EmulatedPluginTracingService(ExecutionTreeNode currentExecutionTreeNode)
        {
            this.CurrentExecutionTreeNode = currentExecutionTreeNode;
        }

        public ExecutionTreeNode CurrentExecutionTreeNode { get; }

        public void Trace(string format, params object[] args)
        {
            if (args != null && args.Length != 0)
            {
                this.CurrentExecutionTreeNode?.Trace.AppendFormat(format, args);
                this.CurrentExecutionTreeNode?.Trace.AppendLine();
            }
            else
            {
                this.CurrentExecutionTreeNode?.Trace.AppendLine(format);
            }            
        }
    }
}