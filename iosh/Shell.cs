using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Iodine.Compiler;
using Iodine.Runtime;
using System.Collections;

namespace iosh {

	/// <summary>
	/// Iodine REPL Shell.
	/// </summary>
	public class Shell {

		/// <summary>
		/// The prompt.
		/// </summary>
		readonly Prompt prompt;

		/// <summary>
		/// The iodine context.
		/// </summary>
		readonly IodineContext context;

		/// <summary>
		/// Initializes a new instance of the <see cref="iosh.Shell"/> class.
		/// </summary>
		public Shell () {

			// Create the default prompt
			prompt = new Prompt ("λ");

			// Create the iodine context
			context = new IodineContext {
				AllowBuiltins = true
			};
		}

		/// <summary>
		/// Run the REPL shell.
		/// </summary>
		public void Run () {

			// Set the culture
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			// Set the console output encoding to UTF-8
			// This is needed in order to properly display the lambda prompt
			Console.OutputEncoding = Encoding.UTF8;

			// Print the assembly version
			var version = Assembly.GetEntryAssembly ().GetName ().Version;
			Console.WriteLine ("iosh v{0}", version.ToString (3));

			// Enter the REPL
			while (true)
				RunIteration ();
		}

		/// <summary>
		/// Runs one REP iteration.
		/// Needs major refactoring.
		/// </summary>
		void RunIteration () {

			// Prepare stuff
			IodineObject rawvalue = null;
			Console.Write (prompt);

			// Read all lines of the source
			prompt.Push ("|");
			var source = ReadStatements ();
			prompt.Pop ();

			// Skip empty sources
			if (source.Length == 0)
				return;

			// Clear the screen if requested
			if (source == ":clear") {
				Console.Clear ();
				return;
			}

			// Compile the source unit
			try {
				var unit = SourceUnit.CreateFromSource (source);
				var result = unit.Compile (context);
				rawvalue = context.Invoke (result, new IodineObject[0]);
			} catch (UnhandledIodineExceptionException e) {
				var msg = ((IodineString) e.OriginalException.GetAttribute ("message")).Value;
				Console.WriteLine (msg);
				// e.PrintStack ();
			} catch (SyntaxException e) {
				foreach (var error in e.ErrorLog) {
					var location = error.Location;
					Console.WriteLine ("[{0}: {1}] Error: {2}", location.Line, location.Column, error.Text);
				}
				e.ErrorLog.Clear ();
			} catch (Exception e) {
				Console.WriteLine ("{0}", e.Message);
			}

			// Print the result
			WriteStringRepresentation (rawvalue);
			Console.WriteLine ();
		}

		string ReadStatements () {
			string line;
			var accum = new StringBuilder ();
			var openBracketRule = new OpenBracketRule ();
			while (openBracketRule.Match ((line = Console.ReadLine ()))) {
				SendKeys.SendWait (string.Empty.PadLeft (openBracketRule.indent, ' '));
				Console.Write (prompt);
				accum.AppendFormat (" {0}", line.Trim ());
			}
			accum.AppendFormat (" {0}", line.Trim ());
			return accum.ToString ().Trim ();
		}

		void WriteStringRepresentation (IodineObject obj) {

			// Test if the object or its typedef are undefined
			if (obj == null || obj.TypeDef == null) {
				ConsoleHelper.Write ("{0}", "red/Error: The object or its typedef are undefined");
				return;
			}

			var value = obj.ToString ();
			switch (obj.TypeDef.ToString ()) {
			case "Null":
				ConsoleHelper.Write ("{0}", "gray/null");
				break;
			case "Bool":
				ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", value.ToLowerInvariant ()));
				break;
			case "Int":
			case "Float":
				ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", value));
				break;
			case "Tuple":
				var tpl = obj as IodineTuple;
				ConsoleHelper.Write ("{0}", "cyan/[Tuple: ");
				Console.Write ("( ");
				for (var i = 0; i < tpl.Objects.Count (); i++) {
					if (i > 0)
						Console.Write (", ");
					WriteStringRepresentation (tpl.Objects [i]);
				}
				Console.Write (" )");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "List":
				var lst = obj as IodineList;
				if (lst.Objects.Count == 0) {
					ConsoleHelper.Write ("{0}", "cyan/[List: ");
					ConsoleHelper.Write ("{0}", "magenta/(empty)");
					ConsoleHelper.Write ("{0}", "cyan/]");
					break;
				}
				ConsoleHelper.Write ("{0}", "cyan/[List: ");
				Console.Write ("[ ");
				for (var i = 0; i < lst.Objects.Count; i++) {
					if (i > 0)
						Console.Write (", ");
					WriteStringRepresentation (lst.Objects [i]);
				}
				Console.Write (" ]");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "ByteArray":
				var bytearr = obj as IodineByteArray;
				Console.Write ("[ ");
				for (var i = 0; i < bytearr.Array.Length; i++) {
					if (i > 0)
						Console.Write (", ");
					ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", bytearr.Array [i]));
				}
				Console.Write (" ]");
				break;
			case "Bytes":
				var bytes = obj as IodineBytes;
				Console.Write ("[ ");
				for (var i = 0; i < bytes.Value.Length; i++) {
					if (i > 0)
						Console.Write (", ");
					ConsoleHelper.Write ("{0}", string.Format ("darkyellow/{0}", bytes.Value [i]));
				}
				Console.Write (" ]");
				break;
			case "HashMap":
				var map = obj as IodineHashMap;
				var keys = map.Keys.Reverse ().ToArray ();
				Console.Write ("{ ");
				for (var i = 0; i < keys.Length; i++) {
					if (i > 0)
						Console.Write (", ");
					var key = keys [i];
					var val = map.Get (key);
					WriteStringRepresentation (key);
					Console.Write (" = ");
					WriteStringRepresentation (val);
				}
				Console.Write (" }");
				break;
			case "Str":
				var str = value.Replace ("'", @"\'");
				ConsoleHelper.Write ("{0}", string.Format ("green/'{0}'", str));
				break;
			case "Closure":
				ConsoleHelper.Write ("{0}", "cyan/[Function: Closure]");
				break;
			case "Method":
				var func = Regex.Match (value, "<function\\s([a-z0-9]*)>", RegexOptions.IgnoreCase).Groups [1];
				ConsoleHelper.Write ("{0}", string.Format ("cyan/[Function: {0}]", func));
				break;
			case "Builtin":
				var internalfunc = obj as BuiltinMethodCallback;
				ConsoleHelper.Write ("{0}", "cyan/[Function: ");
				ConsoleHelper.Write ("{0}", string.Format ("cyan/{0} ", internalfunc.Callback.Method.Name));
				ConsoleHelper.Write ("{0}", "magenta/(builtin)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "InstanceMethod":
				var instancefunc = obj as IodineInstanceMethodWrapper;
				ConsoleHelper.Write ("{0}", "cyan/[Function: ");
				ConsoleHelper.Write ("{0}", string.Format ("cyan/{0} ", instancefunc.Method.Name));
				ConsoleHelper.Write ("{0}", "magenta/(bound)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "Module":
				var module = obj as IodineModule;
				ConsoleHelper.Write ("{0}", string.Format ("cyan/[Module: {0}", module.Name));
				if (module.ExistsInGlobalNamespace)
					ConsoleHelper.Write ("{0}", "magenta/ (global)");
				ConsoleHelper.Write ("{0}", "cyan/]");
				for (var i = 0; i < module.Attributes.Count; i++) {
					var attr = module.Attributes.ElementAt (i);
					if (attr.Value == null || attr.Value.TypeDef == null)
						continue;
					Console.WriteLine ();
					Console.Write ("{0}: ", attr.Key);
					WriteStringRepresentation (attr.Value);
				}
				break;
			case "Generator":
				var generator = obj as IodineGenerator;
				var generatorfields = generator.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
				var generatorbasemethod = generatorfields.First (field => field.Name == "baseMethod").GetValue (generator);
				var generatormethodname = string.Empty;
				var generatormethod = generatorbasemethod as IodineMethod;
				if (generatormethod != null)
					generatormethodname = generatormethod.Name;
				var generatorinstancemethod = generatorbasemethod as IodineInstanceMethodWrapper;
				if (generatorinstancemethod != null)
					generatormethodname = generatorinstancemethod.Method.Name;
				ConsoleHelper.Write ("{0}", "cyan/[Iterator");
				if (generatormethodname == string.Empty)
					ConsoleHelper.Write ("{0}", "magenta/ (generator, anonymous)");
				else {
					ConsoleHelper.Write ("{0}", string.Format ("cyan/: {0}", generatormethodname));
					ConsoleHelper.Write ("{0}", "magenta/ (generator)");
				}
				ConsoleHelper.Write ("{0}", "cyan/]");
				break;
			case "RangeIterator":
				var range = obj as IodineRange;
				var rangefields = range.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
				var rangemin = rangefields.First (field => field.Name == "min").GetValue (range);
				var rangeend = rangefields.First (field => field.Name == "end").GetValue (range);
				var rangestep = rangefields.First (field => field.Name == "step").GetValue (range);
				ConsoleHelper.Write ("{0}", "cyan/[Iterator: Range (");
				Console.Write ("min: ");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0} ", rangemin));
				Console.Write ("end: ");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0} ", rangeend));
				Console.Write ("step: ");
				ConsoleHelper.Write ("{0}", string.Format ("yellow/{0}", rangestep));
				ConsoleHelper.Write ("{0}", "cyan/)]");
				break;
			case "TypeDef":
				var typedef = obj as IodineTypeDefinition;
				ConsoleHelper.Write ("{0}", string.Format ("cyan/[Typedef: {0}]", typedef.Name));
				break;
			default:
				var iodineClass = obj as IodineClass;
				if (iodineClass != null) {
					var attrcount = iodineClass.Attributes.Count;
					ConsoleHelper.Write ("{0}", string.Format ("cyan/[Class: {0}]", iodineClass.Name));
					Console.WriteLine ();
					ConsoleHelper.Write ("{0}", string.Format ("darkgray/# begin class {0}", iodineClass.Name));
					for (var i = 0; i < attrcount; i++) {
						var attr = iodineClass.Attributes.ElementAt (i);
						if (attr.Value == null || attr.Value.TypeDef == null)
							continue;
						Console.WriteLine ();
						Console.Write ("{0}: ", attr.Key);
						WriteStringRepresentation (attr.Value);
					}
					Console.WriteLine ();
					ConsoleHelper.Write ("{0}", string.Format ("darkgray/# end class {0}", iodineClass.Name));
					break;
				}
				ConsoleHelper.Write ("{0}", string.Format ("cyan/[Typedef: {0}]", obj.TypeDef.Name));
				Console.WriteLine ();
				ConsoleHelper.Write ("{0}", string.Format ("darkgray/# begin type {0}", obj.TypeDef.Name));
				for (var i = 0; i < obj.Attributes.Count; i++) {
					var attr = obj.Attributes.ElementAt (i);
					if (attr.Value == null || attr.Value.TypeDef == null)
						continue;
					Console.WriteLine ();
					Console.Write ("{0}: ", attr.Key);
					WriteStringRepresentation (attr.Value);
				}
				Console.WriteLine ();
				ConsoleHelper.Write ("{0}", string.Format ("darkgray/# end type {0}", obj.TypeDef.Name));
				break;
			}
		}

		// TODO: Remove this. Not used anywhere.
		[Obsolete]
		void ShouldContinueLinex (string line) {
			var trimmedLine = line.Trim ();

			/* Uncontinued if test
			 * [Triggers]
			 * if (cond)
			 * [Ignores]
			 * if (cond) stmt;
			 */
			var uncontinuedIfBegin = new [] { "if", "else" };
			var uncontinuedIfEnd = new [] { ";", ")" };
			var uncontinuedIf = true
				&& uncontinuedIfBegin.Any (trimmedLine.StartsWith)
				&& !uncontinuedIfEnd.Any (trimmedLine.EndsWith);
		}
	}
}

