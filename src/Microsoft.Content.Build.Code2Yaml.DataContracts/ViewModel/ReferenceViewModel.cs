namespace Microsoft.Content.Build.Code2Yaml.DataContracts
{
    using System;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ReferenceViewModel
    {
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "commentId")]
        public string CommentId { get; set; }

        [YamlMember(Alias = "parent")]
        public string Parent { get; set; }

        [YamlMember(Alias = "definition")]
        public string Definition { get; set; }

        [YamlMember(Alias = "isExternal")]
        public bool? IsExternal { get; set; }

        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "nameWithType")]
        public string NameWithType { get; set; }

        [YamlMember(Alias = "fullName")]
        public string FullName { get; set; }
    }
}
