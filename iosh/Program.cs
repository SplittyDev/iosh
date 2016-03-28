using System;

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

			// Create and run the REPL shell
			var shell = new Shell ();
			shell.Run ();
		}
	}
}
