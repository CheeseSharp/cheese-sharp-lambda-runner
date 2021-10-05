using System.Collections.Generic;
using System.Linq;

namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public class LambdaTriggerMap
    {
        public LambdaTriggerMap(
            string functionResorce, 
            IEnumerable<LambdaTriggerTarget> lambdaTriggerTargets)
        {
            FunctionResource = 
                Invariants.IsNotNullAndNotEmptyElseThrow(
                    functionResorce, 
                    $"{nameof(LambdaTriggerMap)} - {nameof(functionResorce)}");
            LambdaTriggerTargets = 
                Invariants.IsNotNullElseThrow(
                    lambdaTriggerTargets,
                    $"{nameof(LambdaTriggerMap)} - {nameof(lambdaTriggerTargets)}").ToList();
        }

        public string FunctionResource { get; }

        public IReadOnlyList<LambdaTriggerTarget> LambdaTriggerTargets { get;  }
    }
}
