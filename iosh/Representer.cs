using System;
using System.Linq;
using System.Reflection;
using Iodine.Runtime;
using static System.Console;
using static System.ConsoleColor;
using static iosh.ConsoleHelper;

namespace iosh {
    
    public static class Representer {

        public static bool WriteStringRepresentation (IodineObject obj, int depth = 0) {

            if (depth > Shell.MaxRecursionDepth) {
                return false;
            }

            // Test if the typedef is undefined
            if (obj.TypeDef == null && obj != null) {
                var type = obj.ToString ();
                Writec (Cyan, "[Type: ", null, type, Cyan, "]");
                return true;
            }

            // Test if the object is undefined
            if (obj == null && obj.TypeDef == null) {
                Writec (Red, "Error: The object or its typedef are undefined");
                return true;
            }

            var value = obj.ToString ();
            switch (obj.GetType ().Name) {
            case "IodineNull":
                //ConsoleHelper.Write ("{0}", "gray/null");
                return false;
            case "IodineBool":
                Writec (DarkYellow, value.ToLowerInvariant ());
                break;
            case "IodineInteger":
            case "IodineFloat":
                Writec (DarkYellow, value);
                break;
            case "IodineTuple":
                var tpl = obj as IodineTuple;
                Writec (Cyan, "[Tuple: ");
                Write ("( ");
                for (var i = 0; i < tpl.Objects.Count (); i++) {
                    if (i > 0)
                        Write (", ");
                    if (i > Shell.MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
                    WriteStringRepresentation (tpl.Objects [i], depth + 1);
                }
                Write (" )");
                Writec (Cyan, "]");
                break;
            case "IodineList":
                var lst = obj as IodineList;
                if (lst.Objects.Count == 0) {
                    Writec (Cyan, "[List: ", Magenta, "(empty)", Cyan, "]");
                    break;
                }
                Writec (Cyan, "[List: ");
                Write ("[ ");
                for (var i = 0; i < lst.Objects.Count; i++) {
                    if (i > 0)
                        Write (", ");
                    if (i > Shell.MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
                    WriteStringRepresentation (lst.Objects [i], depth + 1);
                }
                Write (" ]");
                Writec (Cyan, "]");
                break;
            case "IodineBytes":
                var bytes = obj as IodineBytes;
                Write ("[ ");
                for (var i = 0; i < bytes.Value.Length; i++) {
                    if (i > 0)
                        Write (", ");
                    if (i > Shell.MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
                    Writec (DarkYellow, bytes.Value [i]);
                }
                Write (" ]");
                break;
            case "IodineDictionary":
                var map = obj as IodineDictionary;
                var keys = map.Keys.Reverse ().ToArray ();
                Write ("{ ");
                for (var i = 0; i < keys.Length; i++) {
                    if (i > 0)
                        Write (", ");
                    if (i > Shell.MaxListDisplayLength) {
                        Writec (Magenta, "...");
                        break;
                    }
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Write ("\n  ");
                    }
                    var key = keys [i];
                    var val = map.Get (key);
                    WriteStringRepresentation (key, depth + 1);
                    Write (" = ");
                    WriteStringRepresentation (val, depth + 1);
                }
                Write (" }");
                break;
            case "IodineString":
                var str = value.Replace ("'", @"\'");
                Writec (Green, "'", str, "'");
                break;
            case "IodineClosure":
                Writec (Cyan, "[Function ", Magenta, "(closure)", Cyan, "]");
                break;
            case "MethodBuilder":
                var method = obj as IodineMethod;
                Writec (Cyan, "[Function: ");
                Write (method.Name);
                if (method.ParameterCount > 0) {
                    Write (" (");
                    for (var i = 0; i < method.Parameters.Count; i++) {
                        var arg = method.Parameters.ElementAt (i);
                        if (i > 0)
                            Write (", ");
                        Writec (Yellow, arg.Key);
                    }
                    Write (")");
                }
                Writec (Cyan, "]");
                break;
            case "BuiltinMethodCallback":
                var internalfunc = obj as BuiltinMethodCallback;
                var internalfuncname = internalfunc.Callback.Method.Name.ToLowerInvariant ();
                Writec (Cyan, "[Function: ", internalfuncname, Magenta, " (builtin)", Cyan, "]");
                break;
            case "IodineBoundMethod":
                var instancefunc = obj as IodineBoundMethod;
                Writec (Cyan, "[Function: ");
                Write ("{0} ", instancefunc.Method.Name);
                if (instancefunc.Method.ParameterCount > 0) {
                    Write ("(");
                    for (var i = 0; i < instancefunc.Method.ParameterCount; i++) {
                        var arg = instancefunc.Method.Parameters.ElementAt (i);
                        if (i > 0)
                            Write (", ");
                        Writec (Yellow, arg.Key);
                    }
                    Write (") ");
                }
                Writec (Magenta, "(bound)", Cyan, "]");
                break;
            case "ModuleBuilder":
                // Don't recursively list modules
                if (depth > 0) {
                    return true;
                }
                var module = obj as IodineModule;
                Writec (Cyan, "[Module: ");
                Write (module.Name);
                if (module.ExistsInGlobalNamespace)
                    Writec (Magenta, " (global)");
                Writec (Cyan, "]");
                for (var i = 0; i < module.Attributes.Count; i++) {
                    var attr = module.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    WriteLine ();
                    Write ("{0}: ", attr.Key);
                    WriteStringRepresentation (attr.Value, depth + 1);
                }
                break;
            case "IodineGenerator":
                var generator = obj as IodineGenerator;
                var generatorfields = generator.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
                var generatorbasemethod = generatorfields.First (field => field.Name == "baseMethod").GetValue (generator);
                var generatormethodname = string.Empty;
                var generatormethod = generatorbasemethod as IodineMethod;
                if (generatormethod != null)
                    generatormethodname = generatormethod.Name;
                var generatorinstancemethod = generatorbasemethod as IodineBoundMethod;
                if (generatorinstancemethod != null)
                    generatormethodname = generatorinstancemethod.Method.Name;
                Writec (Cyan, "[Iterator");
                if (generatormethodname == string.Empty)
                    Writec (Magenta, " (generator, anonymous)");
                else {
                    Writec (Cyan, ": ", null, generatormethodname, Magenta, " (generator)");
                }
                Writec (Cyan, "]");
                break;
            case "IodineRange":
                var range = obj as IodineRange;
                var rangefields = range.GetType ().GetFields (BindingFlags.Instance | BindingFlags.NonPublic);
                var rangemin = rangefields.First (field => field.Name == "min").GetValue (range);
                var rangeend = rangefields.First (field => field.Name == "end").GetValue (range);
                var rangestep = rangefields.First (field => field.Name == "step").GetValue (range);
                Writec (Cyan, "[Iterator: Range (", null, "min: ", Yellow, rangemin, null, " end: ", Yellow, rangeend, null, " step: ", Yellow, rangestep, Cyan, ")]");
                break;
            case "IodineProperty":
                var property = obj as IodineProperty;
                Writec (Cyan, "[Property ");
                if (property.Getter != null && !(property.Getter is IodineNull)
                    && property.Setter != null && !(property.Setter is IodineNull))
                    Writec (Magenta, "(get, set)");
                else if (property.Getter != null && !(property.Getter is IodineNull))
                    Writec (Magenta, "(get)");
                else if (property.Setter != null && !(property.Setter is IodineNull))
                    Writec (Magenta, "(set)");
                else
                    Writec (Magenta, "(null)");
                Writec (Cyan, "]");
                break;
            case "IodineTrait":
                var iodineTrait = (IodineTrait)obj;
                WriteLinec (Cyan, "[Trait: ", null, iodineTrait.Name);
                WriteLinec ("   Prototypes: ", Cyan, "[");
                for (var i = 0; i < iodineTrait.RequiredMethods.Count; i++) {
                    var sig = iodineTrait.RequiredMethods [i];
                    Write ("      ");
                    WriteStringRepresentation (sig, depth);
                    if (i < iodineTrait.RequiredMethods.Count - 1)
                        WriteLine ();
                }
                Writec (Cyan, "\n   ]", Cyan, "\n]");
                break;
            case "IodineContract":
                var iodineContract = (IodineContract)obj;
                WriteLinec (Cyan, "[Contract: ", null, iodineContract.Name);
                WriteLinec ("   Prototypes: ", Cyan, "[");
                for (var i = 0; i < iodineContract.RequiredMethods.Count; i++) {
                    var sig = iodineContract.RequiredMethods [i];
                    Write ("      ");
                    WriteStringRepresentation (sig, depth);
                    if (i < iodineContract.RequiredMethods.Count - 1)
                        WriteLine ();
                }
                Writec (Cyan, "\n   ]", Cyan, "\n]");
                break;
            case "ClassBuilder":
            case "IodineClass":
                var iodineClass = (IodineClass)obj;
                var attrcount = iodineClass.Attributes.Count;
                var hasattrs = !obj.Attributes.All (attr => attr.Key.StartsWith ("__", StringComparison.Ordinal));
                if (hasattrs)
                    WriteLinec (DarkGray, "# begin class ", iodineClass.Name);
                Writec (Cyan, "[Class: ", null, iodineClass.Name);
                if (iodineClass.Interfaces.Count > 0) {
                    Writec (" implements ");
                    for (var i = 0; i < iodineClass.Interfaces.Count; i++) {
                        if (i > 0)
                            Write (", ");
                        Writec (Cyan, iodineClass.Interfaces [i].Name);
                    }
                }
                Writec (Cyan, "]");
                if (!hasattrs)
                    break;
                for (var i = 0; i < attrcount; i++) {
                    var attr = iodineClass.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    if (attr.Key.StartsWith ("__", StringComparison.Ordinal))
                        continue;
                    WriteLine ();
                    Write ("{0}: ", attr.Key);
                    WriteStringRepresentation (attr.Value, depth + 1);
                }
                WriteLine ();
                Writec (DarkGray, "# end class ", iodineClass.Name);
                break;
            default:
                if (obj is IodineTypeDefinition) {
                    Writec (Cyan, "[Type: ", null, ((IodineTypeDefinition)obj).Name, Cyan, "]");
                } else {
                    Writec (Cyan, "[Typedef: ", obj.TypeDef.Name, Magenta, " (CLR: ", obj.GetType ().Name, ")", Cyan, "]");
                }
                if (obj.Attributes.All (attr => attr.Key.StartsWith ("__", StringComparison.Ordinal)))
                    break;
                WriteLine ();
                Writec (DarkGray, "# begin type ", obj.TypeDef.Name);
                for (var i = 0; i < obj.Attributes.Count; i++) {
                    var attr = obj.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    if (attr.Key.StartsWith ("__", StringComparison.Ordinal))
                        continue;
                    WriteLine ();
                    Write ("{0}: ", attr.Key);
                    WriteStringRepresentation (attr.Value, depth + 1);
                }
                WriteLine ();
                Writec (DarkGray, "# end type ", obj.TypeDef.Name);
                break;
            }

            return true;
        }
    }
}

