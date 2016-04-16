using System;
using Codeaddicts.libArgument;

namespace iosh {
	
	public class Options {

		[Argument ("--fc", "/fc")]
		public string ForegroundString;

		[Argument ("--bc", "/bc")]
		public string BackgroundString;

		[Switch ("--enable-syntax-coloring", "/enable-syntax-coloring")]
		public bool EnableSyntaxHighlighting;

		[Argument ("--lib", "/lib")]
		public string[] IncludeFolders;

		public ConsoleColor ForegroundColor {
			get { return GetColor (ForegroundString, Console.ForegroundColor); }
		}

		public ConsoleColor BackgroundColor {
			get { return GetColor (BackgroundString, Console.BackgroundColor); }
		}

		static ConsoleColor GetColor (string str, ConsoleColor fallback) {
			ConsoleColor color;
			if (!Enum.TryParse (str, true, out color))
				color = fallback;
			return color;
		}

        readonly public static Options Default;

        static Options () {
            Default = new Options ();
        }
	}
}

