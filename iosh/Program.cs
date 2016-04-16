using System;
using System.IO;
using Codeaddicts.libArgument;

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
            if (args.Length == 1) {
                var filename = args [0];
                ExecuteScript (filename);
            }

			// Parse command-line options
			var options = ArgumentParser.Parse<Options> (args);

			// Create and run the REPL shell
			var shell = new Shell (options);
			shell.Run ();
		}

        static void ExecuteScript (string filename) {
            filename = string.Format ("{0}{1}", filename, filename.EndsWith (".id", StringComparison.Ordinal) ? string.Empty : ".id");
            if (!File.Exists (filename)) {
                Console.WriteLine ("Error: Invalid filename.");
                Environment.Exit (1);
            }
            var engine = new IodineEngine ();
            engine.Compile (File.ReadAllText (filename));
            var shell = new Shell (engine);
            shell.Run ();
        }
	}
}
