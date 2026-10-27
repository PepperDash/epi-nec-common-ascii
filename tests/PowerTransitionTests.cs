using FluentAssertions;
using Xunit;

namespace EpiNecCommonAscii.Tests;

public class PowerTransitionTests
{
    [Fact]
    public void Poll_Interval_Leaves_Room_For_A_Chunked_Reply()
    {
        // A PA804UL power reply takes up to about 300 ms to arrive in full.
        NecPowerTransition.PollIntervalMs.Should().BeGreaterThanOrEqualTo(1000);
    }

    [Theory]
    [InlineData(0u, 45000)]        // not configured
    [InlineData(3000u, 45000)]     // shorter than a real PA804UL power on (~26 s)
    [InlineData(45000u, 45000)]
    [InlineData(60000u, 60000)]    // a slower display can ask for longer
    [InlineData(uint.MaxValue, int.MaxValue)]
    public void Timeout_Is_Configured_Time_But_At_Least_45_s(uint configured, int expected)
    {
        NecPowerTransition.GetTimeoutMs(configured).Should().Be(expected);
    }
}
