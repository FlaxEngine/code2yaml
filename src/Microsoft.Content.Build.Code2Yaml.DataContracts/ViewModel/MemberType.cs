namespace Microsoft.Content.Build.Code2Yaml.DataContracts
{
    using System;

    [Serializable]
    public enum MemberType
    {
        Default,
        Toc,
        Assembly,
        Namespace,
        Class,
        Interface,
        Struct,
        Delegate,
        Enum,
        Field,
        Property,
        Event,
        Constructor,
        Method,
        Operator,
        Container,
        AttachedEvent,
        AttachedProperty
    }

    [Flags]
    public enum AccessLevel
    {
        None = 0,
        Private = 1,
        Public = 2,
        Protected = 4,
        Static = 8,
        Virtual = 16,
        Package = 32,
        NotAccessible = Private | Package,
    }
}
