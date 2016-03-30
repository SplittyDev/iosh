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
				{ "eval", helpEval },
				{ "filter", helpFilter },
				{ "len", helpLen },
				{ "map", helpMap },
				{ "reduce", helpReduce },
				{ "range", helpRange },
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
			var func = args [0] as BuiltinMethodCallback;
			if (str != null) {
				var key = str.Value.ToLowerInvariant ();
				if (helpdict.ContainsKey (key))
					helpdict [key] ();
				else
					Console.WriteLine ("No documentation found for '{0}'.", str.Value);
			} else if (func != null) {
				var key = func.Callback.Method.Name.ToLowerInvariant ();
				if (helpdict.ContainsKey (key))
					helpdict [key] ();
				else
					Console.WriteLine ("No documentation found for '{0}'.", func.Callback.Method.Name);
			} else {
				vm.RaiseException (new IodineTypeException ("Str or Method"));
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
			args (variadic ("object"), "The object to be printed.");
		}

		static void helpInput () {
			func ("input", optional ("prompt"));
			begindoc ();
			doc ("Reads from the standard input stream.");
			doc ("Optionally displays the specified prompt.");
			beginargs ();
			args (optional ("prompt"), "The prompt to be displayed.");
		}

		static void helpInvoke () {
			func ("invoke", arg ("callable"), optional ("dict"));
			begindoc ();
			doc ("Invokes the specified callable under a new Iodine context.");
			doc ("Optionally uses the specified dict as the instance's global symbol table.");
			beginargs ();
			args (arg ("callable"), "The callable to be invoked.");
			args (optional ("dict"), "The global symbol table to be used.");
		}

		static void helpEval () {
			func ("eval", arg ("source"));
			begindoc ();
			doc ("Evaluates a string of Iodine source code.");
			beginargs ();
			args (arg ("source"), "The source code to be evaluated.");
		}

		static void helpFilter () {
			func ("filter", arg ("iterable"), arg ("callable"));
			begindoc ();
			doc (
				"Iterates over the specified iterable, passing the result",
				"of each iteration to the specified callable.",
				"If the callable returns true, the result is appended to a list",
				"that is returned to the caller."
			);
			beginargs ();
			args (arg ("iterable"), "The iterable to be iterated over.");
			args (arg ("callable"), "The callable to be used for filtering.");
		}

		static void helpLen () {
			func ("len", arg ("object"));
			begindoc ();
			doc (
				"Returns the length of the specified object.",
				"If the object does not implement __len__,",
				"an AttributeNotFoundException is raised."
			);
			beginargs ();
			args (arg ("object"), "The object whose length is to be determined.");
		}

		static void helpMap () {
			func ("map", arg ("iterable"), arg ("callable"));
			begindoc ();
			doc (
				"Iterates over the specified iterable, passing the result",
				"of each iteration to the specified callable.",
				"The result of the specified callable is added to a list",
				"that is returned to the caller."
			);
			beginargs ();
			args (arg ("iterable"), "The iterable to be iterated over.");
			args (arg ("callable"), "The callable to be used for mapping.");
		}

		static void helpReduce () {
			func ("reduce", arg ("iterable"), arg ("callable"), optional ("default"));
			begindoc ();
			doc (
				"Reduces all members of the specified iterable by applying the",
				"specified callable to each item left to right.",
				"The callable passed to reduce receives two arguments,",
				"the first one being the result of the last call to it and the",
				"second one being the current item from the iterable."
			);
			beginargs ();
			args (arg ("iterable"), "The iterable to be iterated over.");
			args (arg ("callable"), "The callable to be used for reducing.");
			args (optional ("default"), string.Empty);
		}

		static void helpRange () {
			func ("range", arg ("n"));
			begindoc ();
			doc (
				"Returns an iterable sequence containing [n] items,",
				"starting with 0 and incrementing by 1, until [n] is reached."
			);
			beginargs ();
			args (arg ("n"), "The number of iterations");
			br ();
			func ("range", arg ("start"), arg ("end"));
			begindoc ();
			doc (
				"Returns an iterable sequence containing ([end] - [start]) items,",
				"starting with [start] and incrementing by 1, until [end] is reached."
			);
			beginargs ();
			args (arg ("start"), "The first number in the sequence.");
			args (arg ("end"), "The last number in the sequence.");
			br ();
			func ("range", arg ("start"), arg ("end"), arg ("step"));
			begindoc ();
			doc (
				"Returns an iterable sequence containing (([end] - [start]) / [step]) items,",
				"starting with [start] and increasing by [step], until [end] is reached."
			);
			beginargs ();
			args (arg ("start"), "The first number in the sequence.");
			args (arg ("end"), "The last number in the sequence.");
			args (arg ("step"), "By how much the current number increases every step to reach [end].");
		}

		static void br () {
			Console.WriteLine ();
		}

		static void func (string name, params Action[] arguments) {
			ConsoleHelper.Write ("{0}", "cyan/func");
			Console.Write (" {0} (", name);
			for (var i = 0; i < arguments.Length; i++) {
				if (i > 0)
					Console.Write (", ");
				arguments [i] ();
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

		static void args (Action elem, string description) {
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

