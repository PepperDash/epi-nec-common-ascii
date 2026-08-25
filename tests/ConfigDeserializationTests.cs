using FluentAssertions;
using Xunit;

namespace EpiNecCommonAscii.Tests;

public class ConfigDeserializationTests
{
    private static Type GetConfigType(string name) =>
        AssemblyFixture.PluginAssembly.GetTypes().Single(t => t.Name == name);

    [Fact]
    public void Config_Class_Exists()
    {
        AssemblyFixture.PluginAssembly.GetTypes()
            .Should().Contain(t => t.Name == "NecCommonAsciiDeviceConfigObject");
    }

    [Fact]
    public void Config_Has_Parameterless_Constructor()
    {
        var type = GetConfigType("NecCommonAsciiDeviceConfigObject");
        type.GetConstructor(Type.EmptyTypes).Should().NotBeNull();
    }

    [Theory]
    [InlineData("Control", "control")]
    [InlineData("PollTimeMs", "pollTimeMs")]
    [InlineData("WarningTimeoutMs", "warningTimeoutMs")]
    [InlineData("ErrorTimeoutMs", "errorTimeoutMs")]
    [InlineData("WarmingTimeMs", "warmingTimeMs")]
    [InlineData("CoolingTimeMs", "coolingTimeMs")]
    public void Config_Property_Has_JsonPropertyAttribute(string propertyName, string jsonName)
    {
        var type = GetConfigType("NecCommonAsciiDeviceConfigObject");
        var property = type.GetProperty(propertyName);
        property.Should().NotBeNull($"config should declare property {propertyName}");

        var hasAttribute = property!.CustomAttributes.Any(a =>
            a.AttributeType.Name == "JsonPropertyAttribute"
            && a.ConstructorArguments.Any(arg =>
                string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal)));

        hasAttribute.Should().BeTrue(
            $"NecCommonAsciiDeviceConfigObject.{propertyName} should have [JsonProperty(\"{jsonName}\")]");
    }
}
