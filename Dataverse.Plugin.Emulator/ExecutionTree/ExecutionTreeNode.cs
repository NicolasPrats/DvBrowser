using System.Collections.Generic;
using System.Text;

namespace Dataverse.Plugin.Emulator.ExecutionTree
{
    public class ExecutionTreeNode
    {
        public List<ExecutionTreeNode> ChildNodes { get; } = new List<ExecutionTreeNode>();
        internal ExecutionTreeNode(string title, ExecutionTreeNodeType type)
        {
            this.Title = title;
            this.Type = type;
        }

        public ExecutionTreeNode()
        {
        }

        public string Title { get; internal set; }
        internal StringBuilder Trace { get; } = new StringBuilder();
        public ExecutionTreeNodeType Type { get; internal set; }
        public string GetTrace()
        {
            return this.Trace.ToString();
        }
    }


}
