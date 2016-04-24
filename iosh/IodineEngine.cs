using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public VirtualMachine VirtualMachine {
            get { return context.VirtualMachine; }
        }

		public IodineEngine () {
            if (!BuiltInModules.Modules.ContainsKey ("__iosh_help__"))
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

        public bool TryCompile (string source, out IodineModule module, out IodineObject result) {
            result = IodineNull.Instance;
            if (TryCompileModule (source, out module)) {
                if (TryInvokeModule (module, out result))
                    return true;
            }
            return false;
        }

        public bool TryInvokeModule (IodineModule module, out IodineObject result) {
            return TryIodineOperation (() => context.Invoke (module, new IodineObject [0]), out result);
        }

        public bool TryInvokeModule (IodineModule module) {
            IodineObject _ = null;
            return TryInvokeModule (module, out _);
        }

        public bool TryInvokeModuleAttribute (IodineModule module, string attr, IEnumerable<IodineObject> args, out IodineObject result) {
            result = null;
            if (!module.HasAttribute ("main"))
                return false;
            return TryIodineOperation (() => context.Invoke (module.GetAttribute (attr), args.ToArray ()), out result);
        }

        public bool TryInvokeModuleAttribute (IodineModule module, string attr, IEnumerable<IodineObject> args) {
            IodineObject _;
            return TryInvokeModuleAttribute (module, attr, args, out _);
        }

        public bool TryIodineOperation<T> (Func<T> operation, out T result) {
            result = default (T);
            try {
                result = operation ();
                return true;
            } catch (UnhandledIodineExceptionException e) {
                var msg = ((IodineString)e.OriginalException.GetAttribute ("message")).Value;
                Console.WriteLine (msg);
                e.PrintStack ();
            } catch (ModuleNotFoundException e) {
                ConsoleHelper.WriteLine ("{0}", string.Format ("red/Module not found: {0}", e.Name));
                Console.WriteLine ("Searched in");
                foreach (var path in e.SearchPath) {
                    var workingpath = new Uri (Environment.CurrentDirectory);
                    var currentpath = new Uri (path);
                    var relativepath = workingpath.MakeRelativeUri (currentpath).ToString ();
                    Console.WriteLine ("- ./{0}", relativepath);
                }
            } catch (SyntaxException e) {
                foreach (var error in e.ErrorLog) {
                    var location = error.Location;
                    Console.WriteLine ("[{0}: {1}] Error: {2}", location.Line, location.Column, error.Text);
                }
                e.ErrorLog.Clear ();
            } catch (Exception e) {
                Console.WriteLine ("{0}", e.Message);
            }
            return false;
        }


        public bool TryCompileModule (string source, out IodineModule module) {
            return TryCompileModule (SourceUnit.CreateFromSource (source), out module);
        }

        public bool TryCompileModule (SourceUnit unit, out IodineModule module) {
            module = null;
            var result = TryIodineOperation (() => unit.Compile (Context), out module);
            if (result)
                module = unit.Compile (Context);
            return result;
        }

        void Init () {
            context = new IodineContext ();
            context.SearchPath.Add (AssemblyDirectory);
        }
	}
}

