﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Codeaddicts.libArgument;
using Iodine.Compiler;
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

            // Set the culture
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

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

            // Check file
            var filename = Path.GetFullPath (args [0]);
            if (!File.Exists (filename)) {
                Console.WriteLine ("Error: Invalid filename.");
                Environment.Exit (1);
            }

            IodineModule module;
            IodineObject obj;

            // Create engine
            var engine = new IodineEngine ();

            // Compile and invoke module
            // engine.TryCompileModule (File.ReadAllText (filename), out module);
            // engine.TryInvokeModule (module, out obj);
            var code = SourceUnit.CreateFromFile (filename);

            // Run analyzer
            Analyzer.Create (code).Run ();

            // Compile module
            engine.TryIodineOperation (() => code.Compile (engine.Context), out module);
            engine.TryIodineOperation (() => engine.Context.Invoke (module, new IodineObject [0]), out obj);

            // Invoke main
            var iodineArgs = args.Skip (1).Select (s => new IodineString (s));
            engine.TryInvokeModuleAttribute (module, "main", iodineArgs.ToArray ());

            // Enter shell
            var shell = new Shell (engine);
            shell.Run (showLogo: false);
        }
	}
}
