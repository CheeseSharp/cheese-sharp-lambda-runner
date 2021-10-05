using System;

namespace CheeseSharp.Lambda.TestTool.Runner
{
    public static class Invariants
    {
        public static string IsNotNullAndNotEmptyElseThrow(this string @this, string description)
        {
            return string.IsNullOrEmpty(@this)
                ? throw new ArgumentNullException($"{description} is  Null Or Empty")
                : @this;
        }

        public static T IsNotNullElseThrow<T>(this T @this, string description)
        {
            if (@this is null) 
            {
                throw new ArgumentNullException($"{description} is  Null Or Empty");
            } 
            return @this;
        }
    }
}
