using System;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public const int MaxRecursionDepth = 3;

        /// <summary>
        /// The max list display length.
        /// </summary>
        public const int MaxListDisplayLength = 64;

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
        /// Whether exiting the shell was requested.
        /// </summary>
        bool ExitRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shell"/> class.
        /// </summary>
        public Shell (Options options) {
            CommandLineOptions = options;
            
            // Create the default prompt
            prompt = new Prompt ("λ");

            // Create the Iodine engine
            IodineEngine.UseStableStdlib =! options.NoStdlib;
            engine = new IodineEngine ();

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

            // Print the assembly version
            var version = Assembly.GetEntryAssembly ().GetName ().Version;
            var iodineversion = typeof (IodineContext).Assembly.GetName ().Version;
            if (showLogo)
                WriteLine ("Iosh v{0} (Iodine v{1})", version.ToString (3), iodineversion.ToString (3));

            // Enter the REPL
            while (!ExitRequested) {

                try {
                    RunIteration ();
                } catch (Exception e) {
                    WriteLinec (Magenta, "\n*** ", "Ye dun fuk'd up.");
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
            case ":q":
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
                if (Representer.WriteStringRepresentation (rawvalue))
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
            var foregroundColor = ForegroundColor;
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
                            ForegroundColor = Green;
                            Write ("\b{0}", stringchr);
                            instring = true;
                        } else if (instring && chr == stringchr) {
                            ForegroundColor = foregroundColor;
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
    }
}