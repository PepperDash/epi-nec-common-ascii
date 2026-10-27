using System.Collections.Generic;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// The queries sent on each comm monitor tick. Uses no Essentials types, so the tests can run it.
	/// </summary>
	public static class NecPollPlan
	{
		/// <summary>
		/// Number of poll states; the input and lamp hours queries each go out once per cycle.
		/// </summary>
		public const int StateCount = 4;

		/// <summary>
		/// The queries for a poll state, in send order.
		/// </summary>
		/// <param name="pollState">0 to StateCount - 1</param>
		/// <param name="powerIsOn">The shutter query is skipped while power is off: some displays answer
		/// "error:command" to it in standby.</param>
		/// <param name="pollShutter">False for displays that have no shutter</param>
		public static List<string> GetCommands(int pollState, bool powerIsOn, bool pollShutter)
		{
			var commands = new List<string> { "power" };

			if (pollShutter && powerIsOn)
				commands.Add("shutter");

			switch (pollState)
			{
				case 1:
					commands.Add("input");
					break;
				case 3:
					commands.Add("usage light hours");
					break;
			}

			return commands;
		}
	}
}
