using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iosh {

	/// <summary>
	/// Console helper.
	/// </summary>
	public static class ConsoleHelper {

        const int IndentBlockSize = 3;
        internal static int indent;
        static bool suppressindent;
        static bool indentfrozen;
        static bool logging;
        static bool silent;
        static Stack<int> indents;
        static Stack<bool> suppresses;
        static Stack<bool> freezes;
        static StringBuilder log;

        static ConsoleHelper () {
            indents = new Stack<int> ();
            suppresses = new Stack<bool> ();
            freezes = new Stack<bool> ();
            log = new StringBuilder ();
        }

        public static void Silence () {
            silent = true;
        }

        public static void Unsilence () {
            silent = false;
        }

        public static void StartLog () {
            log.Clear ();
            logging = true;
        }

        public static string GetLog () {
            return log.ToString ();
        }

        public static void StopLog () {
            logging = false;
        }

        public static void FreezeIndent () {
            indentfrozen = true;
        }

        public static void UnfreezeIndent () {
            indentfrozen = false;
        }

        public static void PushFreeze () {
            freezes.Push (indentfrozen);
        }

        public static void PopFreeze () {
            indentfrozen = freezes.Pop ();
        }

        public static void Indent () {
            if (suppressindent)
                return;
            indent += IndentBlockSize;
        }

        public static void Unindent () {
            if (suppressindent)
                return;
            indent -= IndentBlockSize;
        }

        public static void SuppressIndent () {
            suppressindent = true;
        }

        public static void AllowIndent () {
            suppressindent = false;
        }

        public static void PushIndentState () {
            suppresses.Push (suppressindent);
        }

        public static bool PeekIndentState () {
            return suppresses.Peek ();
        }

        public static void PopIndentState () {
            suppressindent = suppresses.Pop ();
        }

        public static void PushIndent () {
            indents.Push (indent);
        }

        public static void NoIndent () {
            indent = 0;
        }

        public static void PopIndent () {
            indent = indents.Pop ();
        }

        public static void Writec (params object [] args) {
            if (!indentfrozen) {
                var indentstr = string.Empty.PadLeft (indent, ' ');
                if (logging)
                    log.Append (indentstr);
                if (!silent)
                    Console.Write (indentstr);
            }
            var c = Console.ForegroundColor;
            foreach (var arg in args) {
                if (arg is ConsoleColor) {
                    Console.ForegroundColor = (ConsoleColor)arg;
                    continue;
                }
                if (arg == null) {
                    Console.ForegroundColor = c;
                    continue;
                }
                if (logging)
                    log.Append (arg);
                if (!silent)
                    Console.Write (arg);
            }
            Console.ForegroundColor = c;
        }

        public static void Writecn (params object [] args) {
            FreezeIndent ();
            Writec (args);
            UnfreezeIndent ();
        }

        public static void WriteLinec (params object [] args) {
            Writec (args);
            Writec ("\n");
        }

        public static void WriteLinecn (params object [] args) {
            Writecn (args);
            Writecn ("\n");
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

