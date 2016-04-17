using System;
using System.IO;
using System.Linq;
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
            var engine = new IodineEngine ();
            var result = engine.Compile (File.ReadAllText (filename));
            var iodineArgs = args.Skip (1).Select (s => new IodineString (s));
            if (result.HasAttribute ("main"))
                result.Invoke (engine.VirtualMachine, iodineArgs.ToArray ());
            var shell = new Shell (engine);
            shell.Run ();
        }
	}
}
