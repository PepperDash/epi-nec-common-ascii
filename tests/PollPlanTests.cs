using FluentAssertions;
using Xunit;

namespace EpiNecCommonAscii.Tests;

public class PollPlanTests
{
    [Theory]
    [InlineData(0, "power", "shutter")]
    [InlineData(1, "power", "shutter", "input")]
    [InlineData(2, "power", "shutter", "usage light hours")]
    [InlineData(3, "power", "shutter", "input")]
    public void Power_On_Polls_Shutter_Each_Tick(int state, params string[] expected)
    {
        NecPollPlan.GetCommands(state, powerIsOn: true, pollShutter: true).Should().Equal(expected);
    }

    [Theory]
    [InlineData(0, "power")]
    [InlineData(1, "power", "input")]
    [InlineData(2, "power", "usage light hours")]
    [InlineData(3, "power", "input")]
    public void Power_Off_Skips_Shutter(int state, params string[] expected)
    {
        NecPollPlan.GetCommands(state, powerIsOn: false, pollShutter: true).Should().Equal(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PollShutter_False_Never_Polls_Shutter(bool powerIsOn)
    {
        for (var state = 0; state < NecPollPlan.StateCount; state++)
            NecPollPlan.GetCommands(state, powerIsOn, pollShutter: false).Should().NotContain("shutter");
    }

    [Fact]
    public void Each_Cycle_Polls_Input_Twice_And_Lamp_Hours_Once()
    {
        var all = Enumerable.Range(0, NecPollPlan.StateCount)
            .SelectMany(s => NecPollPlan.GetCommands(s, true, true)).ToList();

        all.Count(c => c == "input").Should().Be(2);
        all.Count(c => c == "usage light hours").Should().Be(1);
        all.Count(c => c == "power").Should().Be(NecPollPlan.StateCount);
    }
}
