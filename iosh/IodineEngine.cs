using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Iodine.Compiler;
using Iodine.Runtime;

namespace iosh {
	public class IodineEngine {

        public static string AssemblyDirectory {
            get {
                var codeBase = Assembly.GetExecutingAssembly ().CodeBase;
                var uri = new UriBuilder (codeBase);
                var path = Uri.UnescapeDataString (uri.Path);
                return Path.GetDirectoryName (path);
            }
        }

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

		public void IncludeFolders(IEnumerable<string> paths) {
			foreach (var path in paths)
				IncludeFolder(path);
		}

		public IodineObject Compile (string source) {
			var unit = SourceUnit.CreateFromSource (source);
			var result = unit.Compile (Context);
			return context.Invoke (result, new IodineObject[0]);
		}

        void Init () {
            context = new IodineContext ();
            context.SearchPath.Add (AssemblyDirectory);
        }
	}
}

