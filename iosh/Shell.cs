using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Iodine.Compiler;
using Iodine.Runtime;

namespace iosh {

    /// <summary>
    /// Iodine REPL Shell.
    /// </summary>
    public class Shell {

        /// <summary>
        /// The max recursion depth.
        /// </summary>
        const int MaxRecursionDepth = 3;

        /// <summary>
        /// The max list display length.
        /// </summary>
        const int MaxListDisplayLength = 32;

		/// <summary>
		/// The prompt.
		/// </summary>
		readonly Prompt prompt;

		/// <summary>
		/// The iodine engine.
		/// </summary>
		readonly IodineEngine engine;

		/// <summary>
		/// The command line options.
		/// </summary>
		readonly Options CommandLineOptions;

		/// <summary>
		/// The foreground color.
		/// </summary>
		readonly ConsoleColor Foreground;

		/// <summary>
		/// The background color.
		/// </summary>
		readonly ConsoleColor Background;

        /// <summary>
        /// Whether exiting the shall was requested.
        /// </summary>
        bool ExitRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:iosh.Shell"/> class.
        /// </summary>
        Shell () {
            
            // Set colors
            ColoredString.Fallback = Foreground;

            // Create the default prompt
            prompt = new Prompt ("λ");

            // Create the iodine engine
            engine = new IodineEngine ();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shell"/> class.
        /// </summary>
        public Shell (Options options) : this () {
            CommandLineOptions = options;

            // Set colors
            Foreground = options.ForegroundColor;
            Background = options.BackgroundColor;

			// Add search path if requested
			if (options.IncludeFolders != null)
				engine.IncludeFolders (options.IncludeFolders);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:iosh.Shell"/> class.
        /// </summary>
        /// <param name="engine">Engine.</param>
        public Shell (IodineEngine engine) : this (Options.Default) {
            this.engine = engine;
        }

		/// <summary>
		/// Run the REPL shell.
		/// </summary>
		public void Run (bool showLogo = true) {

			// Set the culture
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			// Set the console output encoding to UTF-8
			// This is needed in order to properly display the lambda prompt
			Console.OutputEncoding = Encoding.UTF8;

			// Set colors
			Console.ForegroundColor = Foreground;
			Console.BackgroundColor = Background;

			// Print the assembly version
			var version = Assembly.GetEntryAssembly ().GetName ().Version;
			var iodineversion = typeof(IodineContext).Assembly.GetName ().Version;
            if (showLogo)
                Console.WriteLine ("Iosh v{0} (Iodine v{1})", version.ToString (3), iodineversion.ToString (3));

            // Enter the REPL
            while (!ExitRequested) {

                try {
                    RunIteration ();
                } catch (Exception e) {
                    Console.WriteLine ("Ye dun fuk'd up.");
                    Console.WriteLine (e.StackTrace);
                }
            }
		}

		/// <summary>
		/// Runs one REP iteration.
		/// Needs major refactoring.
		/// </summary>
		void RunIteration () {

			// Prepare stuff
			IodineObject rawvalue = null;
			Console.Write (prompt);

			// Read all lines of the source
			prompt.Push ("|");
			var source = ReadStatements ();
			prompt.Pop ();

			// Skip empty sources
			if (source.Length == 0)
				return;

			// Clear the screen if requested
			switch (source) {
			case ":c":
            case ":clear":
				Console.Clear ();
				return;
            case ":exit":
                ExitRequested = true;
                return;
			}

            // Compile the source unit
            IodineModule _;
            if (!engine.TryCompile (source, out _, out rawvalue)) {
                return;
            }

			// Print the result
			if (rawvalue != null) {
				if (WriteStringRepresentation (rawvalue))
					Console.WriteLine ();
			}
		}

		string ReadStatements () {
			string line;
			var accum = new StringBuilder ();
			var matcher = new LineContinuationRule ();
			while (matcher.Match ((line = ReadLineEx ()))) {
				SendKeys.SendWait (string.Empty.PadLeft (matcher.indent, ' '));
				Console.Write (prompt);
				accum.AppendFormat (" {0}", line.Trim ());
			}
			accum.AppendFormat (" {0}", line.Trim ());
			return accum.ToString ().Trim ();
		}

		string ReadLineEx () {

			// Native read line
			if (!CommandLineOptions.EnableSyntaxHighlighting)
				return Console.ReadLine ();
			
			Console.ForegroundColor = Foreground;
			var accum = new StringBuilder ();
			var accumcw = new StringBuilder ();
			int total = 0;
			int tcurr = 0;
			char stringchr = '\0';
			bool leave = false;
			bool instring = false;
			bool escaping = false;
			while (!leave) {
				var key = Console.ReadKey (intercept: true);
				var chr = key.KeyChar;
				switch (key.Key) {
				case ConsoleKey.Backspace:
					accum.Length = Math.Max (0, accum.Length - 1);
					accumcw.Length = Math.Max (0, accumcw.Length - 1);
					if (tcurr > 0) {
						if (Console.CursorLeft == 0) {
							Console.CursorTop--;
                            Console.CursorLeft = Math.Min (Console.BufferWidth, Console.WindowWidth);
                        }
						total = Math.Max (0, total - 1);
						tcurr = Math.Max (0, tcurr - 1);
						Console.Write ("\b \b");
					}
					break;
				case ConsoleKey.Enter:
					Console.Write ('\n');
					leave = true;
					break;
				case ConsoleKey.LeftArrow:
					if (Console.CursorLeft == prompt.Length && tcurr > 0)
						Console.Write (string.Empty.PadLeft (prompt.Length, '\b'));
					else
						Console.CursorLeft = Math.Max (prompt.Length, Console.CursorLeft - 1);
					tcurr = Math.Min (0, tcurr - 1);
					break;
				case ConsoleKey.RightArrow:
					if (Console.CursorLeft == Console.WindowWidth && tcurr < total + 2) {
						Console.CursorTop++;
						Console.CursorLeft = 0;
					} else
							Console.CursorLeft = Math.Min (Console.CursorLeft + 1, Math.Min (Console.WindowWidth, accum.Length + 2));
						break;
				default:
					total++;
					tcurr++;
					Console.Write (chr);
					accum.Append (chr);
					if (char.IsLetter (chr))
						accumcw.Append (chr);
					else if (!escaping && (chr == '"' || chr == '\'')) {
						if (instring && chr != stringchr) {
							break;
						} else
							stringchr = chr == '"' ? '"' : '\'';
						if (!instring) {
							Console.ForegroundColor = ConsoleColor.Green;
							Console.Write ("\b{0}", stringchr);
							instring = true;
						} else if (instring && chr == stringchr) {
							Console.ForegroundColor = Foreground;
							instring = false;
							stringchr = '\0';
						}
					} else if (char.IsDigit (chr)) {
						Console.Write ("\b");
						ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", chr));
						accumcw.Clear ();
					} else {
						escaping = !escaping && chr == '\\';
						accumcw.Clear ();
					}
					break;
				}
				if (IodineConstants.Keywords.Contains (accumcw.ToString ())) {
					if (Console.CursorLeft >= prompt.Length + accumcw.Length) {
						Console.CursorLeft -= accumcw.Length;
						ConsoleHelper.Write ("{0}", string.Format ("cyan/{0}", accumcw));
					}
				}
			}
			return accum.ToString ();
		}

		bool WriteStringRepresentation (IodineObject obj, int depth = 0) {

            if (depth > MaxRecursionDepth) {
                return false;
            }

            // Test if the typedef is undefined
            if (obj.TypeDef == null && obj != null) {
                var type = obj.ToString ();
                ConsoleHelper.Write ("{0}", string.Format ("cyan/[Type: {0}]", type));
                return true;
            }

            // Test if the object is undefined
            if (obj == null && obj.TypeDef == null) {
                ConsoleHelper.Write ("{0}", "red/Error: The object or its typedef are undefined");
                return true;
            }

            var value = obj.ToString ();
			switch (obj.TypeDef.ToString ()) {
			case "Null":
				//ConsoleHelper.Write ("{0}", "gray/null");
				return false;
			case "Bool":
				ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", value.ToLowerInvariant ()));
				break;
			case "Int":
			case "Float":
				ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", value));
				break;
			case "Tuple":
				var tpl = obj as IodineTuple;
				ConsoleHelper.Write ("{0}", "cyan/[Tuple: ");
				Console.Write ("( ");
				for (var i = 0; i < tpl.Objects.Count (); i++) {
					if (i > 0)
						Console.Write (", ");
                    if (i > MaxListDisplayLength) {
                        ConsoleHelper.Write ("{0}", "magenta/...");
                        break;
                    }
                    if (Console.CursorLeft > (Console.WindowWidth * 0.2)) {
                        Console.Write ("\n  ");
                    }
					WriteStringRepresentation (tpl.Objects [i], depth + 1);
				}
				Console.Write (" )");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "List":
				var lst = obj as IodineList;
				if (lst.Objects.Count == 0) {
					ConsoleHelper.Write ("{0}", "cyan/[List: ");
					ConsoleHelper.Write ("{0}", "magenta/(empty)");
					ConsoleHelper.Write ("{0}", "cyan/]");
					break;
				}
				ConsoleHelper.Write ("{0}", "cyan/[List: ");
				Console.Write ("[ ");
				for (var i = 0; i < lst.Objects.Count; i++) {
					if (i > 0)
						Console.Write (", ");
                    if (i > MaxListDisplayLength) {
                        ConsoleHelper.Write ("{0}", "magenta/...");
                        break;
                    }
                    if (Console.CursorLeft > (Console.WindowWidth * 0.2)) {
                        Console.Write ("\n  ");
                    }
					WriteStringRepresentation (lst.Objects [i], depth + 1);
				}
				Console.Write (" ]");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "Bytes":
				var bytes = obj as IodineBytes;
				Console.Write ("[ ");
				for (var i = 0; i < bytes.Value.Length; i++) {
					if (i > 0)
						Console.Write (", ");
                    if (i > MaxListDisplayLength) {
                        ConsoleHelper.Write ("{0}", "magenta/...");
                        break;
                    }
                    if (Console.CursorLeft > (Console.WindowWidth * 0.2)) {
                        Console.Write ("\n  ");
                    }
					ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", bytes.Value [i]));
				}
				Console.Write (" ]");
				break;
			case "HashMap":
				var map = obj as IodineDictionary;
				var keys = map.Keys.Reverse ().ToArray ();
				Console.Write ("{ ");
				for (var i = 0; i < keys.Length; i++) {
					if (i > 0)
						Console.Write (", ");
                    if (i > MaxListDisplayLength) {
                        ConsoleHelper.Write ("{0}", "magenta/...");
                        break;
                    }
                    if (Console.CursorLeft > (Console.WindowWidth * 0.2)) {
                        Console.Write ("\n  ");
                    }
					var key = keys [i];
					var val = map.Get (key);
					WriteStringRepresentation (key, depth + 1);
					Console.Write (" = ");
					WriteStringRepresentation (val, depth + 1);
				}
				Console.Write (" }");
				break;
			case "Str":
				var str = value.Replace ("'", @"\'");
				ConsoleHelper.Write ("{0}", string.Format ("green/'{0}'", str));
				break;
			case "Closure":
				ConsoleHelper.Write ("{0}", "cyan/[Function ");
				ConsoleHelper.Write ("{0}", "magenta/(closure)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "Method":
				var method = obj as IodineMethod;
				ConsoleHelper.Write ("{0}", "cyan/[Function: ");
				Console.Write (method.Name);
				if (method.ParameterCount > 0) {
					Console.Write (" (");
					for (var i = 0; i < method.ParameterCount; i++) {
						var arg = method.Parameters.ElementAt (i);
						if (i > 0)
							Console.Write (", ");
						ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", arg.Key));
					}
					Console.Write (")");
				}
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "Builtin":
				var internalfunc = obj as BuiltinMethodCallback;
				var internalfuncname = internalfunc.Callback.Method.Name.ToLowerInvariant ();
				ConsoleHelper.Write ("{0}", "cyan/[Function: ");
				ConsoleHelper.Write ("{0}", string.Format ("cyan/{0} ", internalfuncname));
				ConsoleHelper.Write ("{0}", "magenta/(builtin)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "BoundMethod":
				var instancefunc = obj as IodineBoundMethod;
				ConsoleHelper.Write ("{0}", "cyan/[Function: ");
				Console.Write ("{0} ", instancefunc.Method.Name);
				if (instancefunc.Method.ParameterCount > 0) {
					Console.Write ("(");
					for (var i = 0; i < instancefunc.Method.ParameterCount; i++) {
						var arg = instancefunc.Method.Parameters.ElementAt (i);
						if (i > 0)
							Console.Write (", ");
						ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", arg.Key));
					}
					Console.Write (") ");
				}
                ConsoleHelper.Write ("{0}", "magenta/(bound)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "Module":
                // Don't recursively list modules
                if (depth > 0) {
                    return true;
                }
				var module = obj as IodineModule;
				ConsoleHelper.Write ("{0}", "cyan/[Module: ");
				Console.Write (module.Name);
				if (module.ExistsInGlobalNamespace)
					ConsoleHelper.Write ("{0}", "magenta/ (global)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				for (var i = 0; i < module.Attributes.Count; i++) {
					var attr = module.Attributes.ElementAt (i);
					if (attr.Value == null || attr.Value.TypeDef == null)
						continue;
					Console.WriteLine ();
					Console.Write ("{0}: ", attr.Key);
					WriteStringRepresentation (attr.Value, depth + 1);
				}
				break;
			case "Generator":
				var generator = obj as IodineGenerator;
				var generatorfields = generator.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
				var generatorbasemethod = generatorfields.First (field => field.Name == "baseMethod").GetValue (generator);
				var generatormethodname = string.Empty;
				var generatormethod = generatorbasemethod as IodineMethod;
				if (generatormethod != null)
					generatormethodname = generatormethod.Name;
				var generatorinstancemethod = generatorbasemethod as IodineBoundMethod;
				if (generatorinstancemethod != null)
					generatormethodname = generatorinstancemethod.Method.Name;
				ConsoleHelper.Write ("{0}", "cyan/[Iterator");
				if (generatormethodname == string.Empty)
					ConsoleHelper.Write ("{0}", "magenta/ (generator, anonymous)");
				else {
					ConsoleHelper.Write ("{0}", "cyan/: ");
					Console.Write ("{0}", generatormethodname);
					ConsoleHelper.Write ("{0}", "magenta/ (generator)");
				}
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "RangeIterator":
				var range = obj as IodineRange;
				var rangefields = range.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
				var rangemin = rangefields.First (field => field.Name == "min").GetValue (range);
				var rangeend = rangefields.First (field => field.Name == "end").GetValue (range);
				var rangestep = rangefields.First (field => field.Name == "step").GetValue (range);
				ConsoleHelper.Write ("{0}", "cyan/[Iterator: Range (");
				Console.Write ("min: ");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0} ", rangemin));
				Console.Write ("end: ");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0} ", rangeend));
				Console.Write ("step: ");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", rangestep));
				ConsoleHelper.Write ("{0}", "cyan/)]");
				break;
			case "Property":
				var property = obj as IodineProperty;
				ConsoleHelper.Write ("{0}", string.Format ("cyan/[Property "));
				if (property.Getter != null && !(property.Getter is IodineNull)
					&& property.Setter != null && !(property.Setter is IodineNull))
					ConsoleHelper.Write ("{0}", "magenta/(get, set)");
				else if (property.Getter != null && !(property.Getter is IodineNull))
					ConsoleHelper.Write ("{0}", "magenta/(get)");
				else if (property.Setter != null && !(property.Setter is IodineNull))
					ConsoleHelper.Write ("{0}", "magenta/(set)");
				else
					ConsoleHelper.Write ("{0}", "magenta/(null)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			default:
				var iodineClass = obj as IodineClass;
				if (iodineClass != null) {
					var attrcount = iodineClass.Attributes.Count;
					ConsoleHelper.Write ("{0}", string.Format ("cyan/[Class: {0}]", iodineClass.Name));
					if (obj.Attributes.All (attr => attr.Key.StartsWith("__", StringComparison.Ordinal)))
						break;
					Console.WriteLine ();
					ConsoleHelper.Write ("{0}", string.Format ("darkgray/# begin class {0}", iodineClass.Name));
					for (var i = 0; i < attrcount; i++) {
						var attr = iodineClass.Attributes.ElementAt (i);
						if (attr.Value == null || attr.Value.TypeDef == null)
							continue;
						if (attr.Key.StartsWith("__", StringComparison.Ordinal))
							continue;
						Console.WriteLine ();
						Console.Write ("{0}: ", attr.Key);
						WriteStringRepresentation (attr.Value, depth + 1);
					}
					Console.WriteLine ();
					ConsoleHelper.Write ("{0}", string.Format ("darkgray/# end class {0}", iodineClass.Name));
					break;
				}
				ConsoleHelper.Write ("{0}", string.Format ("cyan/[Typedef: {0}]", obj.TypeDef.Name));
				if (obj.Attributes.All (attr => attr.Key.StartsWith("__", StringComparison.Ordinal)))
					break;
				Console.WriteLine ();
				ConsoleHelper.Write ("{0}", string.Format ("darkgray/# begin type {0}", obj.TypeDef.Name));
				for (var i = 0; i < obj.Attributes.Count; i++) {
					var attr = obj.Attributes.ElementAt (i);
					if (attr.Value == null || attr.Value.TypeDef == null)
						continue;
					if (attr.Key.StartsWith("__", StringComparison.Ordinal))
						continue;
					Console.WriteLine ();
					Console.Write ("{0}: ", attr.Key);
					WriteStringRepresentation (attr.Value, depth + 1);
				}
				Console.WriteLine ();
				ConsoleHelper.Write ("{0}", string.Format ("darkgray/# end type {0}", obj.TypeDef.Name));
				break;
			}

			return true;
		}
	}
}

