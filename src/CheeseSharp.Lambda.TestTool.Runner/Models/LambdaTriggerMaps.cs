﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public class LambdaTriggerMaps
    {
        public LambdaTriggerMaps(IEnumerable<LambdaTriggerMap> maps)
        {
            Maps = Invariants.IsNotNullElseThrow(
                maps, 
                $"{nameof(LambdaTriggerMaps)} - {nameof(maps)}").ToList();
        }

        public IReadOnlyList<LambdaTriggerMap> Maps { get; }

        public IReadOnlyList<LambdaTriggerMap> FindByTriggerType(TriggerType triggerType) 
            => this.Maps.Where(i => i.FunctionResource.FunctionResorceType.Equals(triggerType)).ToList();
    }
}
