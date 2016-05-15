using System;
using Iodine.Runtime;

namespace iosh {

    [IodineBuiltinModule ("__iosh_doc__")]
    public class DocModule : IodineModule {

        public DocModule () : base ("__iosh_doc__") {
            ExistsInGlobalNamespace = true;
            SetAttribute ("doc", new BuiltinMethodCallback (doc, this));
        }

        [BuiltinDocString (
            "Prints the Iododoc for any object.",
            "@param obj The object",
            "@variadic Bool Add a 'true' argument to get the doc as Str"
        )]
        static IodineObject doc (VirtualMachine vm, IodineObject self, IodineObject [] args) {
            if (args.Length == 0) {
                vm.RaiseException (new IodineArgumentException (1));
                return IodineNull.Instance;
            }
            var obj = args [0];
            var returnlog = args.Length == 2 && args [1].Equals (IodineBool.True);
            if (returnlog)
                ConsoleHelper.Silence ();
            Representer.WriteStringRepresentation (obj);
            ConsoleHelper.WriteLinecn ();
            ConsoleHelper.StartLog ();
            Representer.WriteDoc (obj);
            ConsoleHelper.StopLog ();
            if (returnlog)
                ConsoleHelper.Unsilence ();
            var log = ConsoleHelper.GetLog ().Trim (' ', '\t', '\r', '\n');
            return returnlog ? new IodineString (log) : (IodineObject) IodineNull.Instance;
        }
    }
}

