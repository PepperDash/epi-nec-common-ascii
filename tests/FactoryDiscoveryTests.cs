using FluentAssertions;
using Xunit;

namespace EpiNecCommonAscii.Tests;

public class FactoryDiscoveryTests
{
    [Fact]
    public void Assembly_Loads_Successfully()
    {
        AssemblyFixture.PluginAssembly.Should().NotBeNull();
    }

    [Fact]
    public void Factory_Count_Is_One()
    {
        AssemblyFixture.FindFactoryTypes().Should().HaveCount(1);
    }

    [Theory]
    [InlineData("NecCommonAsciiDevicePluginFactory")]
    public void Factory_Exists_ByName(string factoryName)
    {
        AssemblyFixture.FindFactoryTypes()
            .Select(t => t.Name)
            .Should().Contain(factoryName);
    }

    [Fact]
    public void Factory_Has_Parameterless_Constructor()
    {
        foreach (var factory in AssemblyFixture.FindFactoryTypes())
        {
            factory.GetConstructor(Type.EmptyTypes)
                .Should().NotBeNull($"{factory.Name} must have a parameterless constructor for plugin discovery");
        }
    }
}
