using System;
using System.Linq;
using System.Reflection;
using Iodine.Runtime;
using static System.Console;
using static System.ConsoleColor;
using static iosh.ConsoleHelper;

namespace iosh {

    [IodineBuiltinModule ("__iosh_help__")]
    public class HelpModule : IodineModule {

        public HelpModule () : base ("__iosh_help__") {
            ExistsInGlobalNamespace = true;
            SetAttribute ("help", new BuiltinMethodCallback (help, null));
        }

        static IodineObject help (VirtualMachine vm, IodineObject self, IodineObject [] arguments) {
            if (arguments.Length != 1) {
                vm.RaiseException (new IodineArgumentException (1));
                return IodineNull.Instance;
            }
            var str = arguments [0] as IodineString;
            var builtin = arguments [0] as BuiltinMethodCallback;
            var stdfunc = arguments [0] as IodineMethod;
            if (str != null) {
                if (!tryInvokeHelpAction (str.Value))
                    WriteLine ("No documentation found for '{0}'.", str.Value);
            } else if (builtin != null) {
                if (!tryInvokeHelpAction (builtin.Callback.Method.Name))
                    WriteLine ("No documentation found for '{0}'.", builtin.Callback.Method.Name);
            } else if (stdfunc != null) {
                if (!tryInvokeHelpAction (stdfunc.Name)) {
                    WriteLine ("No documentation found for '{0}'.", builtin.Callback.Method.Name);
                }
            } else {
                vm.RaiseException (new IodineTypeException ("Str, IodineMethod or BuiltinMethodCallback"));
                return IodineNull.Instance;
            }
            return IodineNull.Instance;
        }

        static bool tryInvokeHelpAction (string input) {
            var lowerInput = input.ToLowerInvariant ();
            var helpMethods = typeof (HelpModule).GetMethods (0x0
                | BindingFlags.Static
                | BindingFlags.NonPublic
            );
            var helpMethod = helpMethods.FirstOrDefault (m =>
                m.Name.ToLowerInvariant () == string.Format ("help{0}", lowerInput));
            if (helpMethod != default (MethodInfo)) {
                helpMethod.Invoke (null, null);
                return true;
            }
            return false;
        }

        #region __builtin__

        static void helpPrint () {
            var arg0 = variadic ("object");
            func ("print", arg0);
            begindoc ();
            doc ("Prints the string representation of any object.");
            doc ("Appends a newline character to the output.");
            beginargs ();
            arg0 = arg ("object");
            args (arg0, "The object to be printed.");
        }

        static void helpInput () {
            var arg0 = optional ("prompt");
            func ("input", arg0);
            begindoc ();
            doc ("Reads from the standard input stream.");
            doc ("Optionally displays the specified prompt.");
            beginargs ();
            arg0 = arg ("prompt");
            args (arg0, "The prompt to be displayed.");
        }

        static void helpInvoke () {
            var arg0 = arg ("callable");
            var arg1 = optional ("dict");
            func ("invoke", arg0, arg1);
            begindoc ();
            doc ("Invokes the specified callable under a new Iodine context.");
            doc ("Optionally uses the specified dict as the instance's global symbol table.");
            beginargs ();
            arg1 = arg ("dict");
            args (arg0, "The callable to be invoked.");
            args (arg1, "The global symbol table to be used.");
        }

        static void helpEval () {
            var arg0 = arg ("source");
            func ("eval", arg0);
            begindoc ();
            doc ("Evaluates a string of Iodine source code.");
            beginargs ();
            args (arg ("source"), "The source code to be evaluated.");
        }

        static void helpFilter () {
            var arg0 = arg ("iterable");
            var arg1 = arg ("callable");
            func ("filter", arg0, arg1);
            begindoc ();
            doc (
                "Iterates over the specified iterable, passing the result",
                "of each iteration to the specified callable.",
                "If the callable returns true, the result is appended to a list",
                "that is returned to the caller."
            );
            beginargs ();
            args (arg0, "The iterable to be iterated over.");
            args (arg1, "The callable to be used for filtering.");
        }

        static void helpLen () {
            var arg0 = arg ("countable");
            func ("len", arg0);
            begindoc ();
            doc (
                "Returns the length of the specified object.",
                "If the object does not implement __len__,",
                "an AttributeNotFoundException is raised."
            );
            beginargs ();
            args (arg0, "The object whose length is to be determined.");
        }

        static void helpMap () {
            var arg0 = arg ("iterable");
            var arg1 = arg ("callable");
            func ("map", arg0, arg1);
            begindoc ();
            doc (
                "Iterates over the specified iterable, passing the result",
                "of each iteration to the specified callable.",
                "The result of the specified callable is added to a list",
                "that is returned to the caller."
            );
            beginargs ();
            args (arg0, "The iterable to be iterated over.");
            args (arg1, "The callable to be used for mapping.");
        }

        static void helpReduce () {
            var arg0 = arg ("iterable");
            var arg1 = arg ("callable");
            var arg2 = optionaldefault ("default", 0);
            func ("reduce", arg0, arg1, arg2);
            begindoc ();
            doc (
                "Reduces all members of the specified iterable by applying the",
                "specified callable to each item left to right.",
                "The callable passed to reduce receives two arguments,",
                "the first one being the result of the last call to it and the",
                "second one being the current item from the iterable."
            );
            beginargs ();
            arg2 = arg ("default");
            args (arg0, "The iterable to be iterated over.");
            args (arg1, "The callable to be used for reducing.");
            args (arg2, "The default item.");
        }

        static void helpRange () {
            Action arg0, arg1, arg2;
            arg0 = arg ("n");
            func ("range", arg0);
            begindoc ();
            doc (
                "Returns an iterable sequence containing [n] items,",
                "starting with 0 and incrementing by 1, until [n] is reached."
            );
            beginargs ();
            args (arg0, "The number of iterations");
            br ();
            arg0 = arg ("start");
            arg1 = arg ("end");
            arg2 = optionaldefault ("step", 1);
            func ("range", arg0, arg1, arg2);
            begindoc ();
            doc (
                "Returns an iterable sequence containing (([end] - [start]) / [step]) items,",
                "starting with [start] and increasing by [step], until [end] is reached."
            );
            beginargs ();
            arg2 = arg ("step");
            args (arg0, "The first number in the sequence.");
            args (arg1, "The last number in the sequence.");
            args (arg2, "By how much the current number increases every step to reach [end].");
        }

        static void helpRepr () {
            var arg0 = arg ("object");
            func ("repr", arg0);
            begindoc ();
            doc (
                "Returns a string representation of the specified object,",
                "which is obtained by calling its __repr__ function.",
                "If the object does not implement the __repr__ function,",
                "its default string representation is returned."
            );
            beginargs ();
            args (arg0, "The object to be represented.");
        }

        static void helpSum () {
            var arg0 = arg ("iterable");
            var arg1 = optionaldefault ("default", 0);
            func ("sum", arg0, arg1);
            begindoc ();
            doc (
                "Reduces the iterable by adding each item together,",
                "starting with [default]."
            );
            beginargs ();
            arg1 = arg ("default");
            args (arg0, "The iterable to be summed up.");
            args (arg1, "The default item.");
        }

        static void helpType () {
            helpTypeOf ();
        }

        static void helpTypeOf () {
            var arg0 = arg ("object");
            func ("type", arg0);
            begindoc ();
            doc ("Returns the type definition of the specified object.");
            beginargs ();
            args (arg0, "The object whose type is to be determined.");
        }

        static void helpTypeCast () {
            var arg0 = arg ("type");
            var arg1 = arg ("object");
            func ("typecast", arg0, arg1);
            begindoc ();
            doc (
                "Performs a sanity check, verifying that the specified",
                "[object] is an instance of [type].",
                "If the test fails, a TypeCastException is raised."
            );
            beginargs ();
            args (arg0, "The type to be tested against.");
            args (arg1, "The object to be tested.");
        }

        static void helpOpen () {
            var arg0 = arg ("file");
            var arg1 = arg ("mode");
            func ("open", arg0, arg1);
            begindoc ();
            doc (
                "Opens up a file using the specified mode,",
                "returning a new stream object."
            );
            beginargs ();
            args (arg0, "The filename.");
            args (arg1, "The mode.");
            section ("Filemodes");
            args (arg ("'a'"), "append");
            args (arg ("'r'"), "read");
            args (arg ("'w'"), "write");
            args (arg ("'rw'"), "read/write");
        }

        static void helpZip () {
            var arg0 = variadic ("iterables");
            func ("zip", arg0);
            begindoc ();
            doc (
                "Iterates over each iterable in [iterables],",
                "appending every item to a tuple, that is then",
                "appended to a list which is returned to the caller."
            );
            beginargs ();
            arg0 = arg ("iterables");
            args (arg0, "The iterables to be zipped.");
        }

        #endregion

        #region hash

        static void helpMD5 () {
            var arg0 = arg ("value");
            modulefunc ("hash", "md5", arg0);
            begindoc ();
            doc ("Computes the MD5 hash of [value].");
            beginargs ();
            args (arg0, "The value whose hash is to be computed.");
        }

        static void helpSHA1 () {
            var arg0 = arg ("value");
            modulefunc ("hash", "sha1", arg0);
            begindoc ();
            doc ("Computes the SHA1 hash of [value].");
            beginargs ();
            args (arg0, "The value whose hash is to be computed.");
        }

        static void helpSHA256 () {
            var arg0 = arg ("value");
            modulefunc ("hash", "sha256", arg0);
            begindoc ();
            doc ("Computes the SHA256 hash of [value].");
            beginargs ();
            args (arg0, "The value whose hash is to be computed.");
        }

        static void helpSHA512 () {
            var arg0 = arg ("value");
            modulefunc ("hash", "sha512", arg0);
            begindoc ();
            doc ("Computes the SHA512 hash of [value].");
            beginargs ();
            args (arg0, "The value whose hash is to be computed.");
        }

        #endregion

        #region Math

        static void helpPow () {
            var arg0 = arg ("base");
            var arg1 = arg ("power");
            modulefunc ("math", "pow", arg0, arg1);
            begindoc ();
            doc ("Raises [base] to [power].");
            beginargs ();
            args (arg0, "The base.");
            args (arg1, "The power.");
        }

        static void helpSin () {
            var arg0 = arg ("angle");
            modulefunc ("math", "sin", arg0);
            begindoc ();
            doc ("Returns the sine of [angle].");
            beginargs ();
            args (arg0, "The angle whose sine is to be calculated.");
        }

        static void helpCos () {
            var arg0 = arg ("angle");
            modulefunc ("math", "cos", arg0);
            begindoc ();
            doc ("Returns the cosine of [angle].");
            beginargs ();
            args (arg0, "The angle whose cosine is to be calculated.");
        }

        static void helpTan () {
            var arg0 = arg ("angle");
            modulefunc ("math", "tan", arg0);
            begindoc ();
            doc ("Returns the tangent of [angle].");
            beginargs ();
            args (arg0, "The angle whose tangent is to be calculated.");
        }

        static void helpASin () {
            var arg0 = arg ("angle");
            modulefunc ("math", "asin", arg0);
            begindoc ();
            doc ("Returns the inverse sine of [angle].");
            beginargs ();
            args (arg0, "The angle whose inverse sine is to be calculated.");
        }

        static void helpACos () {
            var arg0 = arg ("angle");
            modulefunc ("math", "acos", arg0);
            begindoc ();
            doc ("Returns the inverse cosine of [angle].");
            beginargs ();
            args (arg0, "The angle whose inverse cosine is to be calculated.");
        }

        static void helpATan () {
            var arg0 = arg ("angle");
            modulefunc ("math", "atan", arg0);
            begindoc ();
            doc ("Returns the inverse tangent of [angle].");
            beginargs ();
            args (arg0, "The angle whose inverse tangent is to be calculated.");
        }

        static void helpAbs () {
            var arg0 = arg ("value");
            modulefunc ("math", "abs", arg0);
            begindoc ();
            doc ("Returns the absolute value of [value].");
            beginargs ();
            args (arg0, "The value whose absolute value is to be calculated.");
        }

        static void helpFloor () {
            var arg0 = arg ("value");
            modulefunc ("math", "floor", arg0);
            begindoc ();
            doc ("Rounds [value] down to the closest integer.");
            beginargs ();
            args (arg0, "The value to be rounded down.");
        }

        static void helpCeiling () {
            var arg0 = arg ("value");
            modulefunc ("math", "celiling", arg0);
            begindoc ();
            doc ("Rounds [value] up to the closest integer.");
            beginargs ();
            args (arg0, "The value to be rounded up.");
        }

        static void helpLog () {
            var arg0 = arg ("value");
            var arg1 = optionaldefault ("base", 10);
            modulefunc ("math", "log", arg0, arg1);
            begindoc ();
            doc ("Returns the logarithm of [value] to [base].");
            beginargs ();
            arg1 = arg ("base");
            args (arg0, "The value whose logarithm is to be calculated.");
            args (arg1, "The base.");
        }

        static void helpSqrt () {
            var arg0 = arg ("value");
            modulefunc ("math", "sqrt", arg0);
            begindoc ();
            doc ("Returns the square root of [value].");
            beginargs ();
            args (arg0, "The value whose square root is to be calculated.");
        }

        #endregion

        #region Documentation helpers

        static void br () {
            WriteLine ();
        }

        static void section (string name) {
            WriteLine ("{0}:", name);
        }

        static void modulefunc (string module, string name, params Action [] arguments) {
            Writec (Cyan, "func ");
            Writec (Magenta, module, ".");
            Write ("{0} (", name);
            for (var i = 0; i < arguments.Length; i++) {
                if (i > 0)
                    Write (", ");
                arguments [i] ();
            }
            Write (")");
            WriteLine ();
        }

        static void func (string name, params Action [] arguments) {
            Writec (Cyan, "func ");
            Write ("{0} (", name);
            for (var i = 0; i < arguments.Length; i++) {
                if (i > 0)
                    Write (", ");
                arguments [i] ();
            }
            Write (")");
            WriteLine ();
        }

        static void begindoc () {
            WriteLine ("Documentation:");
        }

        static void beginargs () {
            WriteLine ("Arguments:");
        }

        static void args (Action elem, string description) {
            Write ("   ");
            elem ();
            Write (": ");
            WriteLine (description);
        }

        static Action arg (string name) {
            return new Action (() => Writec (Yellow, name));
        }

        static Action optional (string name) {
            return new Action (() => {
                Writec (Magenta, "[", Yellow, name, Magenta, "]");
            });
        }

        static Action optionaldefault (string name, object _default) {
            return new Action (() => {
                Writec (Magenta, "[", Yellow, name, null, " = ", Yellow, _default, Magenta, "]");
            });
        }

        static Action variadic (string name) {
            return new Action (() => arg (string.Format ("*{0}", name)) ());
        }

        static void doc (params string [] arguments) {
            foreach (var str in arguments)
                WriteLinec ("   ", Green, str);
        }

        #endregion
    }
}

