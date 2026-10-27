using System;
using System.Collections.Generic;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// Sends commands at least GapMs apart. Some NEC displays (e.g. the PA804UL) reply in 16-byte chunks about
	/// 100 ms apart and join a partial command with the next one, so back-to-back commands get merged or lost.
	/// Uses no Essentials types, so the tests can run it.
	/// </summary>
	public class NecCommandQueue
	{
		private readonly object _lock = new object();
		private readonly Queue<string> _pending = new Queue<string>();
		private readonly long _gapMs;
		private readonly Action<string> _send;
		private readonly Func<long, Action, IDisposable> _startTimer;
		private readonly Action<string, Exception> _logError;
		private IDisposable _gapTimer;
		private int _generation;

		/// <param name="gapMs">Minimum time between commands</param>
		/// <param name="send">Writes one command to the device</param>
		/// <param name="startTimer">Starts a one-shot timer that runs the action after the given time</param>
		/// <param name="logError">Reports a command that failed to send from the timer thread</param>
		public NecCommandQueue(long gapMs, Action<string> send, Func<long, Action, IDisposable> startTimer, Action<string, Exception> logError)
		{
			_gapMs = gapMs;
			_send = send;
			_startTimer = startTimer;
			_logError = logError;
		}

		/// <summary>
		/// Number of commands waiting for the gap to pass
		/// </summary>
		public int PendingCount
		{
			get { lock (_lock) return _pending.Count; }
		}

		/// <summary>
		/// Sends the command now if the last one went out at least GapMs ago, otherwise queues it.
		/// </summary>
		public void Enqueue(string command)
		{
			lock (_lock)
			{
				if (_gapTimer != null)
				{
					_pending.Enqueue(command);
					return;
				}

				SendAndWait(command);
			}
		}

		/// <summary>
		/// Drops queued commands, e.g. when the connection drops.
		/// </summary>
		public void Clear()
		{
			lock (_lock)
			{
				_pending.Clear();
			}
		}

		private void GapElapsed(int generation)
		{
			lock (_lock)
			{
				// A callback from a timer that has since been replaced does nothing.
				if (generation != _generation || _gapTimer == null)
					return;

				_gapTimer.Dispose();
				_gapTimer = null;

				if (_pending.Count == 0)
					return;

				var command = _pending.Dequeue();
				try
				{
					SendAndWait(command);
				}
				catch (Exception ex)
				{
					// The gap timer is already running, so the rest of the queue still drains.
					_logError(command, ex);
				}
			}
		}

		// Called under _lock. The gap timer starts before the write so a failed write still waits out the gap.
		private void SendAndWait(string command)
		{
			var generation = ++_generation;
			_gapTimer = _startTimer(_gapMs, () => GapElapsed(generation));
			_send(command);
		}
	}
}
