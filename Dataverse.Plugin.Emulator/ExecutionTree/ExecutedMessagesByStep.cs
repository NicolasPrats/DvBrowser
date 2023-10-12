using System.Collections.Generic;

namespace Dataverse.Plugin.Emulator.ExecutionTree
{
    public class ExecutedMessagesByStep
    {
        public string StepName { get; }
        public List<ExecutionTreeNodeType> ExecutedMessages { get; } = new List<ExecutionTreeNodeType>();

        internal ExecutedMessagesByStep(string stepName)
        {
            this.StepName = stepName;
        }
    }
}
