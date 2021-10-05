using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public class LambdaTriggerMapConfigFile
    {
        [JsonPropertyName("function-handler")]
        public string FunctionHandler { get; set; }

        [JsonPropertyName("function-trigger-resource")]
        public string FunctionTriggerResource { get; set; }

        [JsonPropertyName("image-command")]
        public string ImageCommand { get; set; }

        public string ConfigFileLocation { get; set; }

        public string DetermineHandler()
        {
            if (!string.IsNullOrEmpty(this.FunctionHandler))
                return this.FunctionHandler;

            return this.ImageCommand;
        }
    }
}
