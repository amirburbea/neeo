using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class UniqueNameGeneratorTests
{
    [Theory]
    [InlineData("", "3bc15c8aae3e4124dd409035f32ea2fd6835efc9")]
    [InlineData("1234", "b3b24bf88506f9c55e4c1fe23eba7d5322c2448b")]
    [InlineData("abcde", "4b6dc09db4ad69cbe7f17f48c1be3cad5d1e9ff8")]
    public void Generate_should_create_consistent_output_per_input(string input, string expectedOutput)
    {
        const string prefix = "-"; // Defaults to hostname when not specified, but tests need to be runnable on other machines.
        Assert.Equal(expectedOutput, UniqueNameGenerator.Generate(input, prefix));
    }
}