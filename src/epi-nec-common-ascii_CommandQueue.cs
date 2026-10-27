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
		// Queued commands, oldest first. IsQuery marks commands queued with skipIfPending.
		private readonly List<KeyValuePair<string, bool>> _pending = new List<KeyValuePair<string, bool>>();
		private readonly long _gapMs;
		private readonly Action<string> _send;
		private readonly Func<long, Action, IDisposable> _startTimer;
		private readonly Action<string, Exception> _logError;
		private IDisposable _gapTimer;
		private int _generation;
		// True while a command is being written; a command enqueued from inside the write is queued.
		private bool _sending;

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
		/// <param name="command">The command, without the delimiter</param>
		/// <param name="skipIfPending">For queries: drop the command if the same one is already waiting with no
		/// control command queued after it, so a poll that runs faster than the gap can't build a backlog that
		/// delays other commands. A query queued after a control command (e.g. freeze off, then freeze) is kept,
		/// since the earlier one would read the state from before that control. Not for control commands,
		/// where a repeat (e.g. lens start, stop, start) matters.</param>
		public void Enqueue(string command, bool skipIfPending = false)
		{
			lock (_lock)
			{
				if (_gapTimer != null || _sending)
				{
					if (!skipIfPending || !IsPendingSinceLastControl(command))
						_pending.Add(new KeyValuePair<string, bool>(command, skipIfPending));
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

				var command = _pending[0].Key;
				_pending.RemoveAt(0);
				try
				{
					SendAndWait(command);
				}
				catch (Exception ex)
				{
					// The gap timer started anyway, so the rest of the queue still drains.
					_logError(command, ex);
				}
			}
		}

		// Called under _lock. True when the query is already queued after the last queued control command.
		private bool IsPendingSinceLastControl(string query)
		{
			for (var i = _pending.Count - 1; i >= 0; i--)
			{
				if (!_pending[i].Value)
					return false;
				if (_pending[i].Key == query)
					return true;
			}

			return false;
		}

		// Called under _lock. The gap starts when the write returns, so a slow write still gets the full gap
		// after it, and a failed write still waits it out.
		private void SendAndWait(string command)
		{
			_sending = true;
			try
			{
				_send(command);
			}
			finally
			{
				_sending = false;
				var generation = ++_generation;
				_gapTimer = _startTimer(_gapMs, () => GapElapsed(generation));
			}
		}
	}
}
