using System.Collections.Generic;
using System.Linq;

namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public class LambdaTriggerMap
    {
        public LambdaTriggerMap(
            LambdaTriggerMapFunctionResource functionResorce, 
            IEnumerable<LambdaTriggerTarget> lambdaTriggerTargets)
        {
            FunctionResource = 
                Invariants.IsNotNullElseThrow(
                    functionResorce, 
                    $"{nameof(LambdaTriggerMap)} - {nameof(functionResorce)}");
            LambdaTriggerTargets = 
                Invariants.IsNotNullElseThrow(
                    lambdaTriggerTargets,
                    $"{nameof(LambdaTriggerMap)} - {nameof(lambdaTriggerTargets)}").ToList();
        }

        public LambdaTriggerMapFunctionResource FunctionResource { get; }

        public IReadOnlyList<LambdaTriggerTarget> LambdaTriggerTargets { get;  }
    }
}
