using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

		/// <summary>
		/// The iodine engine.
		/// </summary>
		readonly IodineEngine engine;

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

			// Create the iodine engine
			engine = new IodineEngine (context);
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
			var accum = new StringBuilder ();

			// Print the prompt
			Console.Write (prompt);

			// Push the | prompt for multi-line statements
			prompt.Push ("|");

			// Match the openBracketRule for line continuation
			var openBracketRule = new OpenBracketRule ();
			while (openBracketRule.Match ((line = Console.ReadLine ()))) {
				Console.Write (prompt);
				accum.AppendFormat (" {0}", line.Trim ());
			}

			// Append the first/last line of the source
			accum.AppendFormat (" {0}", line.Trim ());

			// Restore the prompt
			prompt.Pop ();

			// Trim excess spaces and newlines
			var source = accum.ToString ().Trim ();

			// Don't do anything if the source is empty
			if (source.Length == 0)
				return;

			// Do the real thing
			try {
				// TODO: Provide a function to do that in a clean way
				source = source.Replace (@"\", @"\\").Replace ("\"", "\\\"");
				source = string.Format ("print (eval (\"{0}\"))", source);
				// TODO: Automate this
				using (var ms = new MemoryStream ()) {
					var cout = Console.Out;
					using (var writer = new StreamWriter (ms))
					using (var reader = new StreamReader (ms)) {
						Console.SetOut (writer);
						try {
							engine.DoString (source);
							writer.Flush ();
						} finally {
							ms.Seek (0, SeekOrigin.Begin);
							Console.SetOut (cout);
						}
						var text = reader.ReadToEnd ().Trim ();
						// TODO: Create rules and matchers for that stuff
						switch (text) {
						// Builtin functions
						// TODO: Create InternalMethod rule
						case "InternalMethod": {
								// TODO: Provide a function to print something
								// TODO: in another color and restore it afterwards
								var c = Console.ForegroundColor;
								Console.ForegroundColor = ConsoleColor.Cyan;
								Console.WriteLine ("[Function: Builtin]");
								Console.ForegroundColor = c;
								break;
							}
						// Normal stuff
						// TODO: Create more rules
						default: {
								// Test if the output is a number
								long testnum;
								if (long.TryParse (text, out testnum)) {
									// TODO: Provide a function to print something
									// TODO: in another color and restore it afterwards
									var c = Console.ForegroundColor;
									Console.ForegroundColor = ConsoleColor.Yellow;
									Console.WriteLine ("{0}", testnum);
									Console.ForegroundColor = c;
								}
								// Just print the output otherwise
								else {
									Console.WriteLine (text);
								}
								break;
							}
						}
					}
				}
			} catch (UnhandledIodineExceptionException e) {
				// TODO: Find another way to print the stack in a clean way
				Console.WriteLine (e.OriginalException.GetAttribute ("message"));
				e.PrintStack ();
			} catch (SyntaxException e) {
				foreach (var error in e.ErrorLog) {
					var location = error.Location;
					Console.WriteLine ("[{0}: {1}] Error: {2}", location.Line, location.Column, error.Text);
				}
			} catch (Exception e) {
				Console.WriteLine ("The Iodine engine just crashed :(");
				Console.WriteLine ("Reason: {0}", e.Message);
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

