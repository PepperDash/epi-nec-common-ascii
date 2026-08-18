using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace EpiNecCommonAscii.Tests;

public class FactoryMetadataTests
{
    // Matches the pinned prerelease this plugin actually targets - NOT a bare "3.0.0", which
    // doesn't exist as a shipped Essentials version.
    private const string ExpectedMinimumEssentialsFrameworkVersion = "3.0.0-rc.1";

    [Theory]
    [InlineData("NecCommonAsciiDevicePluginFactory")]
    public void Factory_Source_Sets_MinimumEssentialsFrameworkVersion(string factoryName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryName);
        source.Should().NotBeNull($"source for {factoryName} should be found under src/");

        source!.Should().Contain(
            $"MinimumEssentialsFrameworkVersion  = \"{ExpectedMinimumEssentialsFrameworkVersion}\"");
    }

    [Theory]
    [InlineData("NecCommonAsciiDevicePluginFactory")]
    public void Factory_Source_Sets_TypeNames(string factoryName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryName);
        source.Should().NotBeNull();
        source!.Should().Contain("TypeNames = new List<string>");
    }

    [Theory]
    [InlineData("NecCommonAsciiDevicePluginFactory", "NecCommonAscii")]
    [InlineData("NecCommonAsciiDevicePluginFactory", "nec common ascii")]
    [InlineData("NecCommonAsciiDevicePluginFactory", "nec ascii projector")]
    public void Factory_Source_Contains_TypeName(string factoryName, string typeName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryName);
        source.Should().NotBeNull();
        source!.Should().Contain($"\"{typeName}\"");
    }

    [Fact]
    public void No_Duplicate_TypeNames_Across_Factory_Sources()
    {
        var factoryNames = new[] { "NecCommonAsciiDevicePluginFactory" };
        var allTypeNames = new List<string>();

        foreach (var factoryName in factoryNames)
        {
            var source = AssemblyFixture.FindSourceForClass(factoryName);
            source.Should().NotBeNull();

            var match = Regex.Match(source!, @"TypeNames\s*=\s*new List<string>\s*\(\)\s*\{([^}]*)\}");
            match.Success.Should().BeTrue($"{factoryName} should assign TypeNames in its constructor");

            var names = Regex.Matches(match.Groups[1].Value, "\"([^\"]+)\"")
                .Select(m => m.Groups[1].Value);
            allTypeNames.AddRange(names);
        }

        allTypeNames.Should().OnlyHaveUniqueItems();
    }
}
