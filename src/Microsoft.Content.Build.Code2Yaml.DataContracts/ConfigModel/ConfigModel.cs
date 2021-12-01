namespace Microsoft.Content.Build.Code2Yaml.DataContracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    using Microsoft.Content.Build.Code2Yaml.Constants;

    [Serializable]
    public class ConfigModel
    {
        [JsonProperty(Constants.InputPaths, Required = Required.DisallowNull)]
        public List<string> InputPaths { get; set; }

        [JsonProperty(Constants.OutputPath, Required = Required.DisallowNull)]
        public string OutputPath { get; set; }

        [JsonProperty(Constants.Language)]
        public string Language { get; set; } = "java";

        [JsonProperty(Constants.Assembly)]
        public string Assembly { get; set; }

        [JsonProperty(Constants.GenerateTocMDFile)]
        public bool GenerateTocMDFile { get; set; } = false;

        [JsonProperty(Constants.ExcludePaths)]
        public List<string> ExcludePaths { get; set; }

        [JsonProperty(Constants.ExcludeTypes)]
        public List<string> ExcludeTypes { get; set; }

        [JsonProperty(Constants.ServiceMapping)]
        public ServiceMappingConfig ServiceMappingConfig { get; set; }

        [JsonProperty(Constants.DoxygenTemplateFile)]
        public string DoxygenTemplateFile { get; set; }

        [JsonProperty("repo_remap")]
        public Dictionary<string, string> RepoRemap { get; set; }

        public int DoxygenTimeout { get; set; }  = 300000; // InMilliseconds 5 minutes
    }
}
