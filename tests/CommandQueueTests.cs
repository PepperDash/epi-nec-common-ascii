using FluentAssertions;
using Xunit;

namespace EpiNecCommonAscii.Tests;

/// <summary>
/// NecCommandQueue with a fake timer standing in for CTimer, so the gap can be ended on demand.
/// </summary>
public class CommandQueueTests
{
    private sealed class FakeTimer : IDisposable
    {
        private readonly Action _elapsed;
        public FakeTimer(long dueTimeMs, Action elapsed) { DueTimeMs = dueTimeMs; _elapsed = elapsed; }
        public long DueTimeMs { get; }
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
        // A real CTimer callback can already be running when the timer is replaced, so firing ignores Disposed.
        public void Fire() => _elapsed();
    }

    private readonly List<string> _sent = new();
    private readonly List<FakeTimer> _timers = new();
    private readonly List<string> _errors = new();
    private readonly NecCommandQueue _queue;
    private Action<string>? _sendOverride;

    public CommandQueueTests()
    {
        _queue = new NecCommandQueue(
            300,
            text => { if (_sendOverride != null) _sendOverride(text); else _sent.Add(text); },
            (ms, elapsed) => { var t = new FakeTimer(ms, elapsed); _timers.Add(t); return t; },
            (text, ex) => _errors.Add($"{text}: {ex.Message}"));
    }

    [Fact]
    public void First_Command_Goes_Out_Immediately_And_Starts_The_Gap()
    {
        _queue.Enqueue("power");

        _sent.Should().Equal("power");
        _timers.Should().ContainSingle().Which.DueTimeMs.Should().Be(300);
    }

    [Fact]
    public void Poll_Burst_Goes_Out_One_Per_Gap()
    {
        _queue.Enqueue("power");
        _queue.Enqueue("shutter");
        _queue.Enqueue("input");

        _sent.Should().Equal("power");
        _queue.PendingCount.Should().Be(2);

        _timers[0].Fire();
        _sent.Should().Equal("power", "shutter");

        _timers[1].Fire();
        _sent.Should().Equal("power", "shutter", "input");
        _queue.PendingCount.Should().Be(0);
    }

    [Fact]
    public void Gap_With_Nothing_Queued_Leaves_The_Queue_Idle()
    {
        _queue.Enqueue("power");
        _timers[0].Fire();

        _timers.Should().ContainSingle();
        _timers[0].Disposed.Should().BeTrue();

        _queue.Enqueue("input");
        _sent.Should().Equal("power", "input");
        _timers.Should().HaveCount(2);
    }

    [Fact]
    public void Stale_Gap_Callback_Is_Ignored()
    {
        _queue.Enqueue("power");
        _queue.Enqueue("shutter");
        _timers[0].Fire();       // sends shutter and starts the second gap
        _queue.Enqueue("input");
        _timers[0].Fire();       // the first timer's callback runs again late

        _sent.Should().Equal("power", "shutter");
        _queue.PendingCount.Should().Be(1);
    }

    [Fact]
    public void Clear_Drops_Pending_Commands()
    {
        _queue.Enqueue("power");
        _queue.Enqueue("shutter");
        _queue.Clear();
        _timers[0].Fire();

        _sent.Should().Equal("power");
    }

    [Fact]
    public void Failed_Send_From_The_Timer_Is_Logged_And_The_Queue_Keeps_Draining()
    {
        _queue.Enqueue("power");
        _queue.Enqueue("shutter");
        _queue.Enqueue("input");

        _sendOverride = text => throw new InvalidOperationException("port closed");
        _timers[0].Fire();
        _sendOverride = null;

        _errors.Should().ContainSingle().Which.Should().Be("shutter: port closed");

        _timers[1].Fire();
        _sent.Should().Equal("power", "input");
    }
}
