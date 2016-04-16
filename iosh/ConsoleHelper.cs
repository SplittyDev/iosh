using System;
using System.Linq;

namespace iosh {

	/// <summary>
	/// Console helper.
	/// </summary>
	public static class ConsoleHelper {

		public static void Write (string format, params ColoredString[] args) {
			var c = Console.ForegroundColor;
			Console.Write (format, args);
			Console.ForegroundColor = c;
		}

		public static void WriteLine (string format, params ColoredString[] args) {
			var c = Console.ForegroundColor;
			Console.Write (format, args);
			Console.ForegroundColor = c;
			Console.WriteLine ();
		}
	}

	public class ColoredString {

		public static ConsoleColor Fallback;

		public ConsoleColor Color;
		string value;

		public ColoredString (string str) {
			ParseColorString (str);
		}

		void ParseColorString (string str) {
			var colors = Enum.GetNames (typeof(ConsoleColor));
			var match = colors.FirstOrDefault (c => str.StartsWith (string.Format ("{0}/", c.ToLowerInvariant ()), StringComparison.Ordinal));
			if (match != null) {
				Color = (ConsoleColor)Enum.Parse (typeof(ConsoleColor), str.Substring (0, match.Length), true);
				value = str.Substring (match.Length + 1);
			} else {
				Color = Fallback;
				value = str;
			}
		}

		public static implicit operator ColoredString (string str) {
			return new ColoredString (str);
		}

		public override string ToString () {
			Console.ForegroundColor = Color;
			return value;
		}
	}
}

