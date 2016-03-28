using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Iodine.Compiler;
using Iodine.Interop;
using Iodine.Runtime;

namespace iosh {

	/// <summary>
	/// Iodine REPL Shell.
	/// </summary>
	public class Shell {

		/// <summary>
		/// The prompt.
		/// </summary>
		readonly Prompt prompt;

		/// <summary>
		/// The iodine context.
		/// </summary>
		readonly IodineContext context;

		string lastValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="iosh.Shell"/> class.
		/// </summary>
		public Shell () {

			// Create the default prompt
			prompt = new Prompt ("λ");

			// Create the iodine context
			context = new IodineContext {
				AllowBuiltins = true
			};

			lastValue = string.Empty;
		}

		/// <summary>
		/// Run the REPL shell.
		/// </summary>
		public void Run () {

			// Set the console output encoding to UTF-8
			// This is needed in order to properly display the lambda prompt
			Console.OutputEncoding = Encoding.UTF8;

			// Print the assembly version
			var version = Assembly.GetEntryAssembly ().GetName ().Version;
			Console.WriteLine ("iosh v{0}", version.ToString (3));

			// Enter the REPL
			while (true)
				RunIteration ();
		}

		/// <summary>
		/// Runs one REP iteration.
		/// Needs major refactoring.
		/// </summary>
		void RunIteration () {

			// Prepare variables
			string line;
			string value = string.Empty;
			var accum = new StringBuilder ();

			// Print the prompt
			Console.Write (prompt);

			// Read all lines of the source
			prompt.Push ("|");
			var openBracketRule = new OpenBracketRule ();
			while (openBracketRule.Match ((line = Console.ReadLine ()))) {
				Console.Write (prompt);
				accum.AppendFormat (" {0}", line.Trim ());
			}
			accum.AppendFormat (" {0}", line.Trim ());
			prompt.Pop ();

			// Skip empty sources
			var source = accum.ToString ().Trim ();
			if (source.Length == 0)
				return;

			// Compile the source unit
			try {
				var unit = SourceUnit.CreateFromSource (source);
				var result = unit.Compile (context);
				value = context.Invoke (result, new IodineObject[0]).ToString ();
			} catch (UnhandledIodineExceptionException e) {
				Console.WriteLine (e.OriginalException.GetAttribute ("message"));
				e.PrintStack ();
			} catch (SyntaxException e) {
				foreach (var error in e.ErrorLog) {
					var location = error.Location;
					Console.WriteLine ("[{0}: {1}] Error: {2}", location.Line, location.Column, error.Text);
				}
				e.ErrorLog.Clear ();
			} catch (Exception e) {
				Console.WriteLine ("{0}", e.Message);
			}

			// Skip empty return values
			if (value == string.Empty)
				return;

			// Test if the result is a builtin function
			if (value == "InternalMethod")
				ConsoleHelper.WriteLine ("{0}", "cyan/[Function: Bound]");

			// Test if the result is an anonymous function
			else if (value == "<Anonymous Function>")
				ConsoleHelper.WriteLine ("{0}", "cyan/[Function]");

			// Test if the result is a user-defined function
			else if (Regex.IsMatch (value, "<function\\s([a-z0-9]*)>", RegexOptions.IgnoreCase)) {
				var func = Regex.Match (value, "<function\\s([a-z0-9]*)>", RegexOptions.IgnoreCase).Groups [1];
				ConsoleHelper.WriteLine ("{0}", string.Format ("cyan/[Function: {0}]", func));
			}

			// Test if the result is a number
			else if (value.StartsWith ("-") || value.All (c => char.IsDigit (c) || ".,".Contains (c)))
				ConsoleHelper.WriteLine ("{0}", string.Format ("darkyellow/{0}", value));

			// Test if the result is null
			else if (value == "null")
				ConsoleHelper.WriteLine ("{0}", string.Format ("darkgray/({0})", value));

			// Test if the result is a string
			else if (value.Any (c => char.IsLetterOrDigit (c))) {
				var str = value.Replace ("'", @"\'");
				ConsoleHelper.WriteLine ("{0}", string.Format ("green/'{0}'", str));
			}
		}

		// TODO: Remove this. Not used anywhere.
		[Obsolete]
		void ShouldContinueLinex (string line) {
			var trimmedLine = line.Trim ();

			/* Uncontinued if test
			 * [Triggers]
			 * if (cond)
			 * [Ignores]
			 * if (cond) stmt;
			 */
			var uncontinuedIfBegin = new [] { "if", "else" };
			var uncontinuedIfEnd = new [] { ";", ")" };
			var uncontinuedIf = true
				&& uncontinuedIfBegin.Any (trimmedLine.StartsWith)
				&& !uncontinuedIfEnd.Any (trimmedLine.EndsWith);
		}
	}
}

