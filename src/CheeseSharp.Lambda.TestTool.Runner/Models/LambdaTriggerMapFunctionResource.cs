using System;

namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public enum TriggerType { simple, cron, sqs}

    public class LambdaTriggerMapFunctionResource
    {
        public LambdaTriggerMapFunctionResource(
            string functionResorce,
            string functionResorceTypeName)
            : this(functionResorce, (TriggerType)Enum.Parse(typeof(TriggerType), functionResorceTypeName))
        { 
        }

        public LambdaTriggerMapFunctionResource(
            string functionResorce,
            TriggerType functionResorceType)
        {
            FunctionResource = 
                Invariants.IsNotNullAndNotEmptyElseThrow(
                    functionResorce, 
                    $"{nameof(LambdaTriggerMap)} - {nameof(functionResorce)}");
            FunctionResorceType = 
                Invariants.IsNotNullElseThrow(
                    functionResorceType,
                    $"{nameof(TriggerType)} - {nameof(functionResorceType)}");
        }

        public string FunctionResource { get; }

        public TriggerType FunctionResorceType { get;  }
    }
}
