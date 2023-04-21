using System.Collections.Generic;

namespace Dataverse.Plugin.Emulator.ExecutionTree
{
    public class TriggeredStep
    {
        public string StepName { get; }
        public List<ExecutionTreeNodeType> ExecutedMessages { get; } = new List<ExecutionTreeNodeType>();

        internal TriggeredStep(string stepName)
        {
            this.StepName = stepName;
        }
    }
}
