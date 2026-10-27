using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace EpiNecCommonAscii.Tests;

// These run plugin code directly. Only types that use no Essentials types can be exercised here.
public class InputConfigConverterTests
{
    private static List<NecInputConfig>? Read(string json) =>
        JsonConvert.DeserializeObject<List<NecInputConfig>>(json);

    [Fact]
    public void String_Entry_Sets_Key_Only()
    {
        var inputs = Read("[\"hdmiIn1\"]")!;
        inputs.Should().ContainSingle();
        inputs[0].Key.Should().Be("hdmiIn1");
        inputs[0].Name.Should().BeNull();
    }

    [Fact]
    public void Object_Entry_Sets_Key_And_Name()
    {
        var inputs = Read("[{\"key\": \"hdmiIn2\", \"name\": \"Laptop\"}]")!;
        inputs[0].Key.Should().Be("hdmiIn2");
        inputs[0].Name.Should().Be("Laptop");
    }

    [Fact]
    public void Mixed_List_Keeps_Order()
    {
        var inputs = Read("[\"hdmiIn1\", {\"key\": \"hdmiIn2\", \"name\": \"Laptop\"}]")!;
        inputs.Select(i => i.Key).Should().Equal("hdmiIn1", "hdmiIn2");
        inputs[1].Name.Should().Be("Laptop");
    }

    [Fact]
    public void Null_Entry_Reads_As_Null()
    {
        Read("[null, \"rgbIn1\"]")!.Should().HaveCount(2).And.Subject.First().Should().BeNull();
    }
}

public class InputMapTests
{
    private static List<KeyValuePair<string, string>> Expose(params NecInputConfig?[]? configured)
    {
        var unknown = new List<string>();
        return NecInputMap.GetExposedInputs(configured!, unknown);
    }

    private static NecInputConfig Input(string key, string? name = null) => new() { Key = key, Name = name };

    [Theory]
    [InlineData("hdmi1", 1)]
    [InlineData("hdmiIn1", 1)]
    [InlineData("computer", 2)]
    [InlineData("rgbIn1", 2)]
    [InlineData("hdmi2", 3)]
    [InlineData("HDMIIN2", 3)]
    [InlineData("hdbaset", 4)]
    [InlineData("hdbaseT1", 4)]
    [InlineData("displayport1", 0)]
    [InlineData(null, 0)]
    public void GetInputNumber_Maps_Projector_And_Routing_Names(string? name, int expected)
    {
        NecInputMap.GetInputNumber(name).Should().Be(expected);
    }

    [Fact]
    public void Missing_Config_Exposes_Default_Inputs_Without_HdbaseT()
    {
        NecInputMap.GetExposedInputs(null!, new List<string>()).Select(i => i.Key)
            .Should().Equal("hdmiIn1", "hdmiIn2", "rgbIn1");
        Expose().Select(i => i.Value).Should().Equal("HDMI 1", "HDMI 2", "RGB");
    }

    [Fact]
    public void Exposed_Inputs_Follow_Port_Order_Not_Config_Order()
    {
        Expose(Input("rgbIn1"), Input("hdmiIn1")).Select(i => i.Key).Should().Equal("hdmiIn1", "rgbIn1");
    }

    [Fact]
    public void Name_Overrides_Label_And_Empty_Name_Keeps_Default()
    {
        var exposed = Expose(Input("hdmiIn1", ""), Input("hdmiIn2", "Laptop"));
        exposed.Should().Equal(
            new KeyValuePair<string, string>("hdmiIn1", "HDMI 1"),
            new KeyValuePair<string, string>("hdmiIn2", "Laptop"));
    }

    [Fact]
    public void Keys_Match_Without_Case_And_Come_Back_Canonical()
    {
        Expose(Input("HDMIIN2")).Select(i => i.Key).Should().Equal("hdmiIn2");
    }

    [Fact]
    public void Unknown_Keys_Are_Reported_And_Skipped()
    {
        var unknown = new List<string>();
        var exposed = NecInputMap.GetExposedInputs(new[] { Input("hdmiIn1"), Input("dvi1"), null! }, unknown);

        exposed.Select(i => i.Key).Should().Equal("hdmiIn1");
        unknown.Should().Equal("dvi1");
    }

    [Fact]
    public void Only_Unknown_Keys_Exposes_All_Inputs()
    {
        var unknown = new List<string>();
        NecInputMap.GetExposedInputs(new[] { Input("dvi1") }, unknown).Should().HaveCount(3);
        unknown.Should().Equal("dvi1");
    }

    // The bridge's analog InputSelect feedback is the port position, not the projector's input number.
    [Theory]
    [InlineData(1, 1)] // hdmi1
    [InlineData(3, 2)] // hdmi2 is port 2 although its input number is 3
    [InlineData(2, 3)] // computer/RGB is port 3 although its input number is 2
    [InlineData(0, 0)]
    public void Port_Number_With_All_Inputs(int inputNumber, int expectedPort)
    {
        NecInputMap.GetPortNumber(new[] { "hdmiIn1", "hdmiIn2", "rgbIn1" }, inputNumber).Should().Be(expectedPort);
    }

    [Fact]
    public void Port_Number_With_Hdmi1_And_Rgb_Only()
    {
        var ports = new[] { "hdmiIn1", "rgbIn1" };
        NecInputMap.GetPortNumber(ports, 2).Should().Be(2);
        NecInputMap.GetPortNumber(ports, 3).Should().Be(0, "hdmi2 isn't exposed");
    }
}

public class HdbaseTInputTests
{
    private static NecInputConfig Input(string key, string? name = null) => new() { Key = key, Name = name };

    [Fact]
    public void HdbaseT_Is_Exposed_Only_When_Listed()
    {
        NecInputMap.GetExposedInputs(null!, new List<string>()).Select(i => i.Key).Should().NotContain("hdbaseT1");

        var unknown = new List<string>();
        NecInputMap.GetExposedInputs(new[] { Input("hdbaseT1") }, unknown)
            .Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("hdbaseT1", "HDBaseT"));
        unknown.Should().BeEmpty();
    }

    [Fact]
    public void HdbaseT_Comes_After_Rgb_In_Port_Order()
    {
        NecInputMap.GetExposedInputs(new[] { Input("hdbaseT1"), Input("rgbIn1"), Input("hdmiIn1"), Input("hdmiIn2") }, new List<string>())
            .Select(i => i.Key).Should().Equal("hdmiIn1", "hdmiIn2", "rgbIn1", "hdbaseT1");
    }

    [Fact]
    public void HdbaseT_Port_Number_Follows_The_Exposed_Ports()
    {
        NecInputMap.GetPortNumber(new[] { "hdmiIn1", "hdmiIn2", "rgbIn1", "hdbaseT1" }, 4).Should().Be(4);
        NecInputMap.GetPortNumber(new[] { "hdmiIn1", "hdmiIn2", "hdbaseT1" }, 4).Should().Be(3);
        NecInputMap.GetPortNumber(new[] { "hdmiIn1", "hdmiIn2" }, 4).Should().Be(0, "hdbaseT1 isn't exposed");
    }

    [Fact]
    public void HdbaseT_Label_Can_Be_Renamed()
    {
        NecInputMap.GetExposedInputs(new[] { Input("hdbaseT1", "Lectern") }, new List<string>())
            .Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string>("hdbaseT1", "Lectern"));
    }
}
