using System;
using System.Linq;

namespace iosh {

	/// <summary>
	/// Console helper.
	/// </summary>
	public static class ConsoleHelper {

		public static void WriteLine (string format, params ColoredString[] args) {
			var c = Console.ForegroundColor;
			Console.WriteLine (format, args);
			Console.ForegroundColor = c;
		}
	}

	public class ColoredString {

		public ConsoleColor Color;
		string value;

		public ColoredString (string str) {
			ParseColorString (str);
		}

		void ParseColorString (string str) {
			var colors = Enum.GetNames (typeof(ConsoleColor));
			var match = colors.FirstOrDefault (c => str.StartsWith (string.Format ("{0}/", c.ToLowerInvariant ())));
			if (match != null) {
				Color = (ConsoleColor)Enum.Parse (typeof(ConsoleColor), str.Substring (0, match.Length), true);
				value = str.Substring (match.Length + 1);
			} else {
				Color = ConsoleColor.Gray;
				value = str;
			}
		}

		public static implicit operator ColoredString (string str) {
			return new ColoredString (str);
		}

		public static implicit operator string (ColoredString str) {
			return str.ToString ();
		}

		public override string ToString () {
			Console.ForegroundColor = Color;
			return value;
		}
	}
}

