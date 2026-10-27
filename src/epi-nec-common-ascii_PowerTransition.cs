using System;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// Timing of the power poll that runs after power on or power off. Uses no Essentials types, so the tests
	/// can run it.
	/// </summary>
	/// <remarks>
	/// A XP-A824U-W takes about 26 s to power on: about 3 s of "power off", about 15 s of "error:busy", about 5 s
	/// of "power warming", then "power on". Its replies arrive in 16-byte chunks about 100 ms apart.
	/// </remarks>
	public static class NecPowerTransition
	{
		/// <summary>
		/// Time between power queries while the power state changes
		/// </summary>
		public const int PollIntervalMs = 1000;

		/// <summary>
		/// How long to keep polling when the config doesn't ask for longer
		/// </summary>
		public const int DefaultTimeoutMs = 45000;

		/// <summary>
		/// The poll timeout: the configured warming or cooling time, but never less than DefaultTimeoutMs.
		/// </summary>
		public static int GetTimeoutMs(uint configuredTimeMs)
		{
			return (int)Math.Min(int.MaxValue, Math.Max(DefaultTimeoutMs, configuredTimeMs));
		}
	}
}
