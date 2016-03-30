using System;
using Iodine.Runtime;
using System.Collections.Generic;

namespace iosh {

	[IodineBuiltinModule ("__iosh_help__")]
	public class HelpModule : IodineModule {

		readonly Dictionary<string, Action> helpdict;
		
		public HelpModule () : base ("__iosh_help__") {
			helpdict = new Dictionary<string, Action> {
				{ "print", helpPrint },
				{ "input", helpInput },
				{ "invoke", helpInvoke },
			};
			SetAttribute ("help", new BuiltinMethodCallback (help, null));
			ExistsInGlobalNamespace = true;
		}

		IodineObject help (VirtualMachine vm, IodineObject self, IodineObject[] args) {
			if (args.Length != 1) {
				vm.RaiseException (new IodineArgumentException (1));
				return IodineNull.Instance;
			}
			var str = args [0] as IodineString;
			if (str != null) {
				if (helpdict.ContainsKey (str.Value.ToLowerInvariant ()))
					helpdict [str.Value.ToLowerInvariant ()] ();
				else
					Console.WriteLine ("No documentation found for '{0}'.", str.Value);
			} else {
				vm.RaiseException (new IodineTypeException ("Str"));
				return IodineNull.Instance;
			}
			return IodineNull.Instance;
		}

		static void helpPrint () {
			func ("print", variadic ("object"));
			begindoc ();
			doc ("Prints the string representation of any object.");
			doc ("Appends a newline character to the output.");
			beginargs ();
			argdoc (variadic ("object"), "The object to be printed.");
		}

		static void helpInput () {
			func ("input", optional ("prompt"));
			begindoc ();
			doc ("Reads from the standard input stream.");
			doc ("Optionally displays the specified prompt.");
			beginargs ();
			argdoc (optional ("prompt"), "The prompt to be displayed.");
		}

		static void helpInvoke () {
			func ("invoke", arg ("function"), optional ("dict"));
			begindoc ();
			doc ("Invokes the specified function under a new Iodine context.");
			doc ("Optionally uses the specified dict as the instance's global symbol table.");
			beginargs ();
			argdoc (arg ("function"), "The function to be invoked.");
			argdoc (optional ("dict"), "The global symbol table to be used.");
		}

		static void func (string name, params Action[] args) {
			ConsoleHelper.Write ("{0}", "cyan/func");
			Console.Write (" {0} (", name);
			for (var i = 0; i < args.Length; i++) {
				if (i > 0)
					Console.Write (", ");
				args [i] ();
			}
			Console.Write (")");
			Console.WriteLine ();
		}

		static void begindoc () {
			Console.WriteLine ("Documentation:");
		}

		static void beginargs () {
			Console.WriteLine ("Arguments:");
		}

		static void argdoc (Action elem, string description) {
			Console.Write ("   ");
			elem ();
			Console.Write (": ");
			Console.WriteLine (description);
		}

		static Action arg (string name) {
			return new Action (() => ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", name)));
		}

		static Action optional (string name) {
			return new Action (() => {
				ConsoleHelper.Write ("{0}", "magenta/[");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", name));
				ConsoleHelper.Write ("{0}", "magenta/]");
			});
		}

		static Action variadic (string name) {
			return new Action (() => arg (string.Format ("{0}*", name)) ());
		}

		static void doc (params string[] args) {
			foreach (var str in args)
				ConsoleHelper.WriteLine ("   {0}", string.Format ("green/{0}", str));
		}
	}
}

