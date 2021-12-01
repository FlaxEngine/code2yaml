namespace Microsoft.Content.Build.Code2Yaml.DataContracts
{
    using System;
    using YamlDotNet.Serialization;

    [Serializable]
    public class LinkInfo
    {
        [YamlMember(Alias = "linkType")]
        public LinkType LinkType { get; set; }

        [YamlMember(Alias = "linkId")]
        public string LinkId { get; set; }
    }

    [Serializable]
    public enum LinkType
    {
        CRef,
        HRef,
    }
}
