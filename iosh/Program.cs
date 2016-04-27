using System;
using System.IO;
using System.Linq;
using System.Text;
using Codeaddicts.libArgument;
using Iodine.Runtime;

namespace iosh {

	/// <summary>
	/// Main class.
	/// </summary>
	class MainClass {

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		public static void Main (string[] args) {

            // Set the console output encoding to UTF-8
            Console.OutputEncoding = Encoding.UTF8;

            // Invoke script directly
            if (args.Length == 1 && args [0].EndsWith (".id", StringComparison.Ordinal)) {
                Interpret (args);
            }

			// Parse command-line options
			var options = ArgumentParser.Parse<Options> (args);

			// Create and run the REPL shell
			var shell = new Shell (options);
			shell.Run ();
		}

        static void Interpret (string[] args) {
            var filename = Path.GetFullPath (args [0]);
            if (!File.Exists (filename)) {
                Console.WriteLine ("Error: Invalid filename.");
                Environment.Exit (1);
            }
            IodineModule module;
            IodineObject obj;
            var engine = new IodineEngine ();
            engine.TryCompile (File.ReadAllText (filename), out module, out obj);
            if (Representer.WriteStringRepresentation (obj))
                Console.WriteLine ();
            engine.TryInvokeModule (module);
            var iodineArgs = args.Skip (1).Select (s => new IodineString (s));
            engine.TryInvokeModuleAttribute (module, "main", iodineArgs.ToArray ());
            var shell = new Shell (engine);
            shell.Run (showLogo: false);
        }
	}
}
