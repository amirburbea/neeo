using System.Text.Json;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class JsonDirectSerializationAttributeTests
{
    [Fact]
    public void Should_Apply_With_Attribute()
    {
        string text = JsonSerializer.Serialize<IWithAttribute>(new Foo(), JsonSerialization.Options);
        Assert.Equal("{\"name\":\"Foo\"}", text);
    }

    [Fact]
    public void Should_Not_Apply_Without_Attribute()
    {
        string text = JsonSerializer.Serialize<IWithoutAttribute>(new Foo(), JsonSerialization.Options);
        Assert.Equal("{}", text);
    }

    private sealed class Foo : IWithAttribute, IWithoutAttribute
    {
        public string Name { get; } = "Foo";
    }

    [JsonDirectSerialization(typeof(IWithAttribute))]
    private interface IWithAttribute
    {
    }

    private interface IWithoutAttribute
    {
    }
}