using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Iodine.Compiler;
using Iodine.Runtime;
using static iosh.ConsoleHelper;
using static System.Console;
using static System.ConsoleColor;

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
        const int MaxListDisplayLength = 64;

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
            OutputEncoding = Encoding.UTF8;
            ForegroundColor = Foreground;
            BackgroundColor = Background;

			// Print the assembly version
			var version = Assembly.GetEntryAssembly ().GetName ().Version;
			var iodineversion = typeof(IodineContext).Assembly.GetName ().Version;
            if (showLogo)
                WriteLine ("Iosh v{0} (Iodine v{1})", version.ToString (3), iodineversion.ToString (3));

            // Enter the REPL
            while (!ExitRequested) {

                try {
                    RunIteration ();
                } catch (Exception e) {
                    WriteLinec (Red, "\n*** ", "Ye dun fuk'd up.");
                    WriteLinec ("    Reason: ", e.Message);
                    WriteLine (e.StackTrace);
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
            Write (prompt);

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
                Clear ();
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
                    WriteLine ();
			}
		}

		string ReadStatements () {
			string line;
			var accum = new StringBuilder ();
			var matcher = new LineContinuationRule ();
			while (matcher.Match ((line = ReadLineEx ()))) {
				SendKeys.SendWait (string.Empty.PadLeft (matcher.indent, ' '));
                Write (prompt);
				accum.AppendFormat (" {0}", line.Trim ());
			}
			accum.AppendFormat (" {0}", line.Trim ());
			return accum.ToString ().Trim ();
		}

		string ReadLineEx () {

			// Native read line
			if (!CommandLineOptions.EnableSyntaxHighlighting)
				return ReadLine ();
            ForegroundColor = Foreground;
			var accum = new StringBuilder ();
			var accumcw = new StringBuilder ();
			int total = 0;
			int tcurr = 0;
			char stringchr = '\0';
			bool leave = false;
			bool instring = false;
			bool escaping = false;
			while (!leave) {
				var key = ReadKey (intercept: true);
				var chr = key.KeyChar;
				switch (key.Key) {
				case ConsoleKey.Backspace:
					accum.Length = Math.Max (0, accum.Length - 1);
					accumcw.Length = Math.Max (0, accumcw.Length - 1);
					if (tcurr > 0) {
						if (CursorLeft == 0) {
                            CursorTop--;
                            CursorLeft = Math.Min (BufferWidth, WindowWidth);
                        }
						total = Math.Max (0, total - 1);
						tcurr = Math.Max (0, tcurr - 1);
                        Write ("\b \b");
					}
					break;
				case ConsoleKey.Enter:
                    Write ('\n');
					leave = true;
					break;
				case ConsoleKey.LeftArrow:
					if (CursorLeft == prompt.Length && tcurr > 0)
                        Write (string.Empty.PadLeft (prompt.Length, '\b'));
					else
                        CursorLeft = Math.Max (prompt.Length, CursorLeft - 1);
					tcurr = Math.Min (0, tcurr - 1);
					break;
				case ConsoleKey.RightArrow:
					if (CursorLeft == WindowWidth && tcurr < total + 2) {
                        CursorTop++;
                        CursorLeft = 0;
					} else
                        CursorLeft = Math.Min (CursorLeft + 1, Math.Min (WindowWidth, accum.Length + 2));
						break;
				default:
					total++;
					tcurr++;
                    Write (chr);
					accum.Append (chr);
					if (char.IsLetter (chr))
						accumcw.Append (chr);
					else if (!escaping && (chr == '"' || chr == '\'')) {
						if (instring && chr != stringchr) {
							break;
						} else
							stringchr = chr == '"' ? '"' : '\'';
						if (!instring) {
                            ForegroundColor = ConsoleColor.Green;
                            Write ("\b{0}", stringchr);
							instring = true;
						} else if (instring && chr == stringchr) {
                            ForegroundColor = Foreground;
							instring = false;
							stringchr = '\0';
						}
					} else if (char.IsDigit (chr)) {
                        Write ("\b");
                        Writec (Yellow, chr);
						accumcw.Clear ();
					} else {
						escaping = !escaping && chr == '\\';
						accumcw.Clear ();
					}
					break;
				}
				if (IodineConstants.Keywords.Contains (accumcw.ToString ())) {
					if (CursorLeft >= prompt.Length + accumcw.Length) {
                        CursorLeft -= accumcw.Length;
                        Writec (Cyan, accumcw);
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
                Writec (Cyan, "[Type: ", null, type, Cyan, "]");
                return true;
            }

            // Test if the object is undefined
            if (obj == null && obj.TypeDef == null) {
                Writec (Red, "Error: The object or its typedef are undefined");
                return true;
            }

            var value = obj.ToString ();
            switch (obj.GetType ().Name) {
			case "IodineNull":
				//ConsoleHelper.Write ("{0}", "gray/null");
				return false;
			case "IodineBool":
                Writec (DarkYellow, value.ToLowerInvariant ());
				break;
			case "IodineInteger":
			case "IodineFloat":
                Writec (DarkYellow, value);
				break;
			case "IodineTuple":
				var tpl = obj as IodineTuple;
                Writec (Cyan, "[Tuple: ");
                Write ("( ");
				for (var i = 0; i < tpl.Objects.Count (); i++) {
					if (i > 0)
                        Write (", ");
                    if (i > MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
					WriteStringRepresentation (tpl.Objects [i], depth + 1);
				}
                Write (" )");
                Writec (Cyan, "]");
				break;
			case "IodineList":
				var lst = obj as IodineList;
				if (lst.Objects.Count == 0) {
                    Writec (Cyan, "[List: ", Magenta, "(empty)", Cyan, "]");
					break;
				}
                Writec (Cyan, "[List: ");
                Write ("[ ");
				for (var i = 0; i < lst.Objects.Count; i++) {
					if (i > 0)
                        Write (", ");
                    if (i > MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
					WriteStringRepresentation (lst.Objects [i], depth + 1);
				}
                Write (" ]");
                Writec (Cyan, "]");
				break;
			case "IodineBytes":
				var bytes = obj as IodineBytes;
                Write ("[ ");
				for (var i = 0; i < bytes.Value.Length; i++) {
					if (i > 0)
                        Write (", ");
                    if (i > MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
                    Writec (DarkYellow, bytes.Value [i]);
				}
                Write (" ]");
				break;
			case "IodineDictionary":
				var map = obj as IodineDictionary;
				var keys = map.Keys.Reverse ().ToArray ();
                Write ("{ ");
				for (var i = 0; i < keys.Length; i++) {
					if (i > 0)
                        Write (", ");
                    if (i > MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
					var key = keys [i];
					var val = map.Get (key);
					WriteStringRepresentation (key, depth + 1);
                    Write (" = ");
					WriteStringRepresentation (val, depth + 1);
				}
                Write (" }");
				break;
			case "IodineString":
				var str = value.Replace ("'", @"\'");
                Writec (Green, "'", str, "'");
				break;
			case "IodineClosure":
                Writec (Cyan, "[Function ", Magenta, "(closure)", Cyan, "]");
				break;
			case "MethodBuilder":
				var method = obj as IodineMethod;
                Writec (Cyan, "[Function: ");
                Write (method.Name);
				if (method.ParameterCount > 0) {
                    Write (" (");
					for (var i = 0; i < method.Parameters.Count; i++) {
						var arg = method.Parameters.ElementAt (i);
						if (i > 0)
                            Write (", ");
                        Writec (Yellow, arg.Key);
					}
                    Write (")");
				}
                Writec (Cyan, "]");
				break;
			case "BuiltinMethodCallback":
				var internalfunc = obj as BuiltinMethodCallback;
				var internalfuncname = internalfunc.Callback.Method.Name.ToLowerInvariant ();
                Writec (Cyan, "[Function: ", internalfuncname, Magenta, " (builtin)", Cyan, "]");
				break;
			case "IodineBoundMethod":
				var instancefunc = obj as IodineBoundMethod;
                Writec (Cyan, "[Function: ");
                Write ("{0} ", instancefunc.Method.Name);
				if (instancefunc.Method.ParameterCount > 0) {
                    Write ("(");
					for (var i = 0; i < instancefunc.Method.ParameterCount; i++) {
						var arg = instancefunc.Method.Parameters.ElementAt (i);
						if (i > 0)
                            Write (", ");
                        Writec (Yellow, arg.Key);
					}
                    Write (") ");
				}
                Writec (Magenta, "(bound)", Cyan, "]");
				break;
			case "ModuleBuilder":
                // Don't recursively list modules
                if (depth > 0) {
                    return true;
                }
				var module = obj as IodineModule;
                Writec (Cyan, "[Module: ");
                Write (module.Name);
                if (module.ExistsInGlobalNamespace)
                    Writec (Magenta, " (global)");
                Writec (Cyan, "]");
				for (var i = 0; i < module.Attributes.Count; i++) {
					var attr = module.Attributes.ElementAt (i);
					if (attr.Value == null || attr.Value.TypeDef == null)
						continue;
                    WriteLine ();
                    Write ("{0}: ", attr.Key);
					WriteStringRepresentation (attr.Value, depth + 1);
				}
				break;
			case "IodineGenerator":
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
                Writec (Cyan, "[Iterator");
                if (generatormethodname == string.Empty)
                    Writec (Magenta, " (generator, anonymous)");
				else {
                    Writec (Cyan, ": ", null, generatormethodname, Magenta, " (generator)");
				}
                Writec (Cyan, "]");
				break;
			case "IodineRange":
				var range = obj as IodineRange;
				var rangefields = range.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
				var rangemin = rangefields.First (field => field.Name == "min").GetValue (range);
				var rangeend = rangefields.First (field => field.Name == "end").GetValue (range);
				var rangestep = rangefields.First (field => field.Name == "step").GetValue (range);
                Writec (Cyan, "[Iterator: Range (", null, "min: ", Yellow, rangemin, null, " end: ", Yellow, rangeend, null, " step: ", Yellow, rangestep, Cyan, ")]");
				break;
			case "IodineProperty":
				var property = obj as IodineProperty;
                Writec (Cyan, "[Property ");
                if (property.Getter != null && !(property.Getter is IodineNull)
                    && property.Setter != null && !(property.Setter is IodineNull))
                    Writec (Magenta, "(get, set)");
                else if (property.Getter != null && !(property.Getter is IodineNull))
                    Writec (Magenta, "(get)");
                else if (property.Setter != null && !(property.Setter is IodineNull))
                    Writec (Magenta, "(set)");
                else
                    Writec (Magenta, "(null)");
                Writec (Cyan, "]");
				break;
            case "IodineTrait":
                var iodineTrait = (IodineTrait)obj;
                WriteLinec (Cyan, "[Trait: ", null, iodineTrait.Name);
                WriteLinec ("   Prototypes: ", Cyan, "[");
                for (var i = 0; i < iodineTrait.RequiredMethods.Count; i++) {
                    var sig = iodineTrait.RequiredMethods [i];
                    Write ("      ");
                    WriteStringRepresentation (sig, depth);
                    if (i < iodineTrait.RequiredMethods.Count - 1)
                        WriteLine ();
                }
                Writec (Cyan, "\n   ]", Cyan, "\n]");
                break;
            case "IodineContract":
                var iodineContract = (IodineContract)obj;
                WriteLinec (Cyan, "[Contract: ", null, iodineContract.Name);
                WriteLinec ("   Prototypes: ", Cyan, "[");
                for (var i = 0; i < iodineContract.RequiredMethods.Count; i++) {
                    var sig = iodineContract.RequiredMethods [i];
                    Write ("      ");
                    WriteStringRepresentation (sig, depth);
                    if (i < iodineContract.RequiredMethods.Count - 1)
                        WriteLine ();
                }
                Writec (Cyan, "\n   ]", Cyan, "\n]");
                break;
            case "ClassBuilder":
            case "IodineClass":
                var iodineClass = (IodineClass)obj;
                var attrcount = iodineClass.Attributes.Count;
                var hasattrs = !obj.Attributes.All (attr => attr.Key.StartsWith ("__", StringComparison.Ordinal));
                if (hasattrs)
                    WriteLinec (DarkGray, "# begin class ", iodineClass.Name);
                Writec (Cyan, "[Class: ", null, iodineClass.Name);
                if (iodineClass.Interfaces.Count > 0) {
                    Writec (" implements ");
                    for (var i = 0; i < iodineClass.Interfaces.Count; i++) {
                        if (i > 0)
                            Write (", ");
                        Writec (Cyan, iodineClass.Interfaces [i].Name);
                    }
                }
                Writec (Cyan, "]");
                if (!hasattrs)
                    break;
                for (var i = 0; i < attrcount; i++) {
                    var attr = iodineClass.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    if (attr.Key.StartsWith ("__", StringComparison.Ordinal))
                        continue;
                    WriteLine ();
                    Write ("{0}: ", attr.Key);
                    WriteStringRepresentation (attr.Value, depth + 1);
                }
                WriteLine ();
                Writec (DarkGray, "# end class ", iodineClass.Name);
                break;
			default:
                if (obj is IodineTypeDefinition) {
                    Writec (Cyan, "[Type: ", null, ((IodineTypeDefinition)obj).Name, Cyan, "]");
                } else {
                    Writec (Cyan, "[Typedef: ", obj.TypeDef.Name, Magenta, " (CLR: ", obj.GetType ().Name, ")", Cyan, "]");
                }
				if (obj.Attributes.All (attr => attr.Key.StartsWith("__", StringComparison.Ordinal)))
					break;
                WriteLine ();
                Writec (DarkGray, "# begin type ", obj.TypeDef.Name);
				for (var i = 0; i < obj.Attributes.Count; i++) {
					var attr = obj.Attributes.ElementAt (i);
					if (attr.Value == null || attr.Value.TypeDef == null)
						continue;
					if (attr.Key.StartsWith("__", StringComparison.Ordinal))
						continue;
                    WriteLine ();
                    Write ("{0}: ", attr.Key);
					WriteStringRepresentation (attr.Value, depth + 1);
				}
                WriteLine ();
                Writec (DarkGray, "# end type ", obj.TypeDef.Name);
				break;
			}

			return true;
		}
	}
}

