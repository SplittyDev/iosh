using System;
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

			// Parse command-line options
			var options = ArgumentParser.Parse<Options> (args);

			// Create and run the REPL shell
			var shell = new Shell (options);
			shell.Run ();
		}
	}
}
