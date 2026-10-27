using System;
using System.Collections.Generic;
using System.Linq;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// Input names, numbers and port positions. Uses no Essentials types, so the tests can run it.
	/// </summary>
	public static class NecInputMap
	{
		/// <summary>
		/// Every input the plugin supports, in port order, with its default label
		/// </summary>
		public static readonly IReadOnlyList<KeyValuePair<string, string>> AllInputs = new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string>(NecAsciiInputs.HdmiIn1, "HDMI 1"),
			new KeyValuePair<string, string>(NecAsciiInputs.HdmiIn2, "HDMI 2"),
			new KeyValuePair<string, string>(NecAsciiInputs.RgbIn1, "RGB"),
			new KeyValuePair<string, string>(NecAsciiInputs.HdbaseT1, "HDBaseT")
		};

		/// <summary>
		/// The inputs exposed when the config lists none. HDBaseT is exposed only when the config lists it.
		/// </summary>
		public static readonly IReadOnlyList<KeyValuePair<string, string>> DefaultInputs = AllInputs
			.Where(i => i.Key != NecAsciiInputs.HdbaseT1)
			.ToList();

		/// <summary>
		/// The plugin's number for an input (hdmi1 = 1, computer = 2, hdmi2 = 3, hdbaset = 4), from either the
		/// projector's name or the routing name. 0 when unknown.
		/// </summary>
		public static int GetInputNumber(string name)
		{
			// Replies arrive lower-cased, but InputSelect passes routing names such as hdmiIn1,
			// so compare without case.
			switch ((name ?? string.Empty).ToLowerInvariant())
			{
				case "hdmi1":
				case "hdmiin1":
					return 1;
				case "hdmi2":
				case "hdmiin2":
					return 3;
				case "computer":
				case "rgbin1":
					return 2;
				case "hdbaset":
				case "hdbaset1":
					return 4;
				default:
					return 0;
			}
		}

		/// <summary>
		/// The inputs to expose, in the fixed order hdmiIn1, hdmiIn2, rgbIn1, hdbaseT1, with their labels.
		/// DefaultInputs when the config lists none, or none it lists are known. Unknown keys are added to
		/// unknownKeys.
		/// </summary>
		public static List<KeyValuePair<string, string>> GetExposedInputs(IEnumerable<NecInputConfig> configured, ICollection<string> unknownKeys)
		{
			var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var input in (configured ?? Enumerable.Empty<NecInputConfig>()).Where(i => i != null))
			{
				if (!AllInputs.Any(i => string.Equals(i.Key, input.Key, StringComparison.OrdinalIgnoreCase)))
				{
					unknownKeys.Add(input.Key);
					continue;
				}

				labels[input.Key] = input.Name;
			}

			if (labels.Count == 0)
				return DefaultInputs.ToList();

			return AllInputs
				.Where(i => labels.ContainsKey(i.Key))
				.Select(i => new KeyValuePair<string, string>(i.Key, string.IsNullOrEmpty(labels[i.Key]) ? i.Value : labels[i.Key]))
				.ToList();
		}

		/// <summary>
		/// 1-based position of the input among the exposed ports, matching the bridge's analog InputSelect join.
		/// 0 when no input is selected or it isn't exposed.
		/// </summary>
		public static int GetPortNumber(IEnumerable<string> portKeys, int inputNumber)
		{
			if (inputNumber == 0)
				return 0;

			var position = 0;
			foreach (var key in portKeys)
			{
				position++;
				if (GetInputNumber(key) == inputNumber)
					return position;
			}

			return 0;
		}
	}
}
