using System.Text.Json;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class JsonDirectSerializationAttributeTests
{
    /// <summary>
    /// Empty interface with attribute will serialize as the object type.
    /// </summary>
    [JsonDirectSerialization<IWithAttribute>]
    private interface IWithAttribute
    {
    }

    /// <summary>
    /// Empty interface without attribute will serialize as &quot;{}&quot;.
    /// </summary>
    private interface IWithoutAttribute
    {
    }

    [Fact]
    public void Should_serialize_as_GetType_with_attribute()
    {
        string text = JsonSerializer.Serialize((IWithAttribute)Foo.Instance, JsonSerialization.Options);
        Assert.Equal("{\"name\":\"Foo\"}", text);
    }

    [Fact]
    public void Should_serialize_as_T_without_attribute()
    {
        string text = JsonSerializer.Serialize((IWithoutAttribute)Foo.Instance, JsonSerialization.Options);
        Assert.Equal("{}", text);
    }

    private sealed class Foo : IWithAttribute, IWithoutAttribute
    {
        public static readonly Foo Instance = new();

        public string Name { get; } = "Foo";
    }
}
