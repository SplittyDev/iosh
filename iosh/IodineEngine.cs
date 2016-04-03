using System;
using Iodine.Compiler;
using Iodine.Runtime;

namespace iosh {
	public class IodineEngine {

		IodineContext context;
		public IodineContext Context {
			get { return context; }
		}

		public IodineEngine () {
			BuiltInModules.Modules.Add ("__iosh_help__", new HelpModule ());
			Init ();
		}

		public void Reload () {
			Init ();
		}

		public void IncludeFolder (string path) {
			context.SearchPath.Add (path);
		}

		public IodineObject Compile (string source) {
			var unit = SourceUnit.CreateFromSource (source);
			var result = unit.Compile (Context);
			return context.Invoke (result, new IodineObject[0]);
		}

		void Init () {
			context = new IodineContext ();
		}
	}
}

