using System;
using Iodine.Compiler;
using Iodine.Runtime;

namespace iosh {
	public class IodineEngine {

		public readonly IodineContext Context;

		public IodineEngine () {
			BuiltInModules.Modules.Add ("__iosh_help__", new HelpModule ());
			Context = new IodineContext ();
		}

		public IodineObject Compile (string source) {
			var unit = SourceUnit.CreateFromSource (source);
			var result = unit.Compile (Context);
			return Context.Invoke (result, new IodineObject[0]);
		}
	}
}

