using System;
using Codeaddicts.libArgument.Attributes;

namespace iosh {
	
	public class Options {

		[Argument ("--fc", "/fc")]
		public string ForegroundString;

		[Argument ("--bc", "/bc")]
		public string BackgroundString;

		[Switch ("--enable-syntax-coloring")]
		public bool EnableSyntaxHighlighting;

		public ConsoleColor ForegroundColor {
			get { return GetColor (ForegroundString, Console.ForegroundColor); }
		}

		public ConsoleColor BackgroundColor {
			get { return GetColor (BackgroundString, Console.BackgroundColor); }
		}

		static ConsoleColor GetColor (string str, ConsoleColor fallback) {
			ConsoleColor color;
			if (!Enum.TryParse<ConsoleColor> (str, true, out color))
				color = fallback;
			return color;
		}
	}
}

