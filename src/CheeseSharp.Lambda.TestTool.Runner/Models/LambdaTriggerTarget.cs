namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public class LambdaTriggerTarget
    {
        public LambdaTriggerTarget(string functionHandler, string configFileLocation)
        {
            FunctionHandler = Invariants.IsNotNullAndNotEmptyElseThrow(
                functionHandler,
                $"{nameof(LambdaTriggerTarget)} - {nameof(functionHandler)}");
            ConfigFileLocation = Invariants.IsNotNullAndNotEmptyElseThrow(
                configFileLocation,
                $"{nameof(LambdaTriggerTarget)} - {nameof(functionHandler)}");
        }

        public string FunctionHandler { get; }

        public string ConfigFileLocation { get; }
    }
}
