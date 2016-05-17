using System;
using System.Linq;
using System.Reflection;
using Iodine.Runtime;
using static System.Console;
using static System.ConsoleColor;
using static iosh.ConsoleHelper;

namespace iosh {
    
    public static class Representer {

        public static void WriteDoc (IodineObject obj, int depth = 0, bool suppressindent = false) {
            if (!obj.HasAttribute ("__doc__"))
                return;
            var docstr = (IodineString) obj.GetAttribute ("__doc__");
            if (docstr.Value == null)
                return;
            var doc = DocParser.Parse (docstr.Value);
            if (doc.HasDescription) {
                WriteLinec ("Documentation:");
                Indent ();
                foreach (var line in doc.Description.Split ('\n'))
                    if (!string.IsNullOrWhiteSpace (line))
                        WriteLinec (Green, line);
                Unindent ();
            }
            if (doc.HasAuthors) {
                var authors = doc.Authors;
                WriteLinec ("Authors:");
                Indent ();
                foreach (var author in authors)
                    WriteLinec (author);
                Unindent ();
            }
            if (doc.HasParameters) {
                var parameters = doc.Parameters;
                WriteLinec ("Arguments:");
                Indent ();
                foreach (var p in parameters) {
                    if (p.Name.StartsWith ("*", StringComparison.Ordinal))
                        continue;
                    WriteLinec (
                        Yellow, p.Name, Magenta, " (", Cyan, p.Type, Magenta, ")",
                        null, p.HasDescription ? ": " : string.Empty, p.Description
                    );
                }
                Unindent ();
            }
            if (doc.HasKeywordParameters) {
                var kwargs = doc.KeywordParameters;
                WriteLinec ("Keyword arguments:");
                Indent ();
                foreach (var p in kwargs) {
                    WriteLinec (
                        Yellow, p.Name, Magenta, " (", Cyan, p.Type, Magenta, ")",
                        null, p.HasDescription ? ": " : string.Empty, p.Description
                    );
                }
                Unindent ();
            }
            if (doc.HasVariadicArguments) {
                var varargs = doc.VariadicValue;
                WriteLinec ("Variadic arguments:");
                Indent ();
                WriteLinec (
                    "Zero or more ", Cyan, varargs.Type, "s",
                    null, varargs.HasDescription ? ": " : string.Empty, varargs.Description
                );
                Unindent ();
            }
            if (doc.HasYieldValue) {
                var retval = doc.YieldValue;
                WriteLinec (
                    "Yields ", Cyan, retval.Type,
                    null, retval.HasDescription ? ": " : string.Empty, retval.Description
                );
            }
            if (doc.HasReturnValue) {
                var retval = doc.ReturnValue;
                WriteLinec (
                    "Returns ", Cyan, retval.Type,
                    null, retval.HasDescription ? ": " : string.Empty, retval.Description
                );
            }
        }

        public static bool WriteStringRepresentation (IodineObject obj, int depth = 0, bool suppressindent = false, bool suppressdoc = true) {

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

            if (!suppressdoc)
                WriteDoc (obj, depth, suppressindent);

            PushIndentState ();
            if (suppressindent)
                SuppressIndent ();

            var value = obj.ToString ();
            switch (obj.GetType ().Name) {
            case "IodineNull":
                // Writec (null, "null");
                // return true;
                return false;
            case "IodineBool":
                Writec (DarkYellow, value.ToLowerInvariant ());
                break;
            case "IodineInteger":
            case "IodineFloat":
            case "IodineBigInt":
                Writec (DarkYellow, value);
                break;
            case "IodineGlobals":
                var globs = obj as IodineGlobals;
                Writec (Cyan, "[Globals]");
                break;
            case "IodineTuple":
                var tpl = obj as IodineTuple;
                if (tpl.Objects.Count () == 0) {
                    Writec (Cyan, "[Tuple: ", Magenta, "(empty)", Cyan, "]");
                    break;
                }
                Writec (Cyan, "[Tuple: ");
                WriteLinecn ("(");
                PushFreeze ();
                UnfreezeIndent ();
                Indent ();
                Writec ();
                for (var i = 0; i < tpl.Objects.Count (); i++) {
                    if (i > 0)
                        Writecn (", ");
                    if (i > Shell.MaxListDisplayLength) {
                        Writecn (Magenta, "...");
                        break;
                    }
                    if (i > 0 && CursorLeft > (WindowWidth * 0.2)) {
                        Writecn ("\n");
                        Writec ();
                    }
                    PopFreeze ();
                    FreezeIndent ();
                    WriteStringRepresentation (tpl.Objects [i], depth + 1, false);
                    UnfreezeIndent ();
                    PushFreeze ();
                    UnfreezeIndent ();
                }
                Unindent ();
                Writecn ("\n");
                Writec (")", Cyan, "]");
                PopFreeze ();
                break;
            case "IodineList":
                var lst = obj as IodineList;
                if (lst.Objects.Count == 0) {
                    Writec (Cyan, "[List: ", Magenta, "(empty)", Cyan, "]");
                    break;
                }
                Writec (Cyan, "[List: ");
                WriteLinecn ("[");
                PushFreeze ();
                UnfreezeIndent ();
                Indent ();
                Writec ();
                for (var i = 0; i < lst.Objects.Count; i++) {
                    if (i > 0)
                        Writecn (", ");
                    if (i > Shell.MaxListDisplayLength) {
                        Writecn (Magenta, "...");
                        break;
                    }
                    if (i > 0 && CursorLeft > (WindowWidth * 0.2)) {
                        Writecn ("\n");
                        Writec ();
                    }
                    PopFreeze ();
                    FreezeIndent ();
                    WriteStringRepresentation (lst.Objects [i], depth + 1, false);
                    UnfreezeIndent ();
                    PushFreeze ();
                    UnfreezeIndent ();
                }
                Unindent ();
                Writecn ("\n");
                Writec ("]", Cyan, "]");
                PopFreeze ();
                break;
            case "IodineBytes":
                var bytes = obj as IodineBytes;
                WriteStringRepresentation (new IodineList (bytes.Value.Select (v => new IodineInteger (v)).ToList<IodineObject> ()));
                break;
            case "IodineDictionary":
                var map = obj as IodineDictionary;
                var keys = map.Keys.Reverse ().ToArray ();
                WriteLinec ("{ ");
                Indent ();
                Writec ();
                for (var i = 0; i < keys.Length; i++) {
                    if (i > 0)
                        Writecn (", ");
                    //if (i > Shell.MaxListDisplayLength) {
                    //    Writecn (Magenta, "...");
                    //    break;
                    //}
                    if (CursorLeft > (WindowWidth * 0.2)) {
                        Writecn ("\n");
                        Writec ();
                    }
                    var key = keys [i];
                    var val = map.Get (key);
                    FreezeIndent ();
                    WriteStringRepresentation (key, depth + 1, true);
                    UnfreezeIndent ();
                    Writecn (" = ");
                    FreezeIndent ();
                    WriteStringRepresentation (val, depth + 1);
                    UnfreezeIndent ();
                }
                Unindent ();
                Writecn ();
                Writec ("}");
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
                Writecn (method.Name);
                if (method.ParameterCount > 0) {
                    Writecn (" (");
                    for (var i = 0; i < method.Parameters.Count; i++) {
                        var arg = method.Parameters.ElementAt (i);
                        if (i > 0)
                            Writecn (", ");
                        Writecn (Yellow, arg.Key);
                    }
                    Writecn (")");
                }
                Writecn (Cyan, "]");
                break;
            case "BuiltinMethodCallback":
                var internalfunc = obj as BuiltinMethodCallback;
                var internalfuncname = internalfunc.Callback.Method.Name.ToLowerInvariant ();
                Writec (Cyan, "[Function: ", internalfuncname, Magenta, " (builtin)", Cyan, "]");
                break;
            case "IodineBoundMethod":
                var instancefunc = obj as IodineBoundMethod;
                Writec (Cyan, "[Function: ");
                Writecn (instancefunc.Method.Name, " ");
                if (instancefunc.Method.ParameterCount > 0) {
                    Writecn ("(");
                    for (var i = 0; i < instancefunc.Method.ParameterCount; i++) {
                        var arg = instancefunc.Method.Parameters.ElementAt (i);
                        if (i > 0)
                            Writecn (", ");
                        Writecn (Yellow, arg.Key);
                    }
                    Writecn (") ");
                }
                Writecn (Magenta, "(bound)", Cyan, "]");
                break;
            case "ModuleBuilder":
                var module = obj as IodineModule;
                Writec (Cyan, "[Module: ");
                Writecn (module.Name);
                if (module.ExistsInGlobalNamespace)
                    Writec (Magenta, " (global)");
                Writec (Cyan, "]");
                // Don't recursively list modules
                if (depth > 0) {
                    return false;
                }
                for (var i = 0; i < module.Attributes.Count; i++) {
                    var attr = module.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    WriteLinecn ();
                    Writecn (attr.Key, ": ");
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
                if (iodineTrait.RequiredMethods.Count == 0) {
                    Writec (Cyan, "[Trait: ", Magenta, "(empty)", Cyan, "]");
                    break;
                }
                WriteLinec (Cyan, "[Trait]");
                PushIndentState ();
                AllowIndent ();
                PushFreeze ();
                UnfreezeIndent ();
                Indent ();
                for (var i = 0; i < iodineTrait.RequiredMethods.Count; i++) {
                    if (i > 0) {
                        Writecn (", ");
                        WriteLinecn ();
                    }
                    Writec ();
                    FreezeIndent ();
                    WriteStringRepresentation (iodineTrait.RequiredMethods [i], depth + 1, true);
                    UnfreezeIndent ();
                }
                Unindent ();
                PopIndentState ();
                PopFreeze ();
                break;
                /*
                WriteLinec (Cyan, "[Trait: ", null, iodineTrait.Name);
                Indent ();
                WriteLinec ("Prototypes: ", Cyan, "[");
                Indent ();
                for (var i = 0; i < iodineTrait.RequiredMethods.Count; i++) {
                    var sig = iodineTrait.RequiredMethods [i];
                    WriteStringRepresentation (sig, depth);
                    if (i < iodineTrait.RequiredMethods.Count - 1)
                        WriteLinecn ();
                }
                Unindent ();
                Writec (Cyan, "\n   ]");
                Unindent ();
                Writec(Cyan, "\n]");
                */
            case "IodineContract":
                var iodineContract = (IodineContract)obj;
                if (iodineContract.RequiredMethods.Count == 0) {
                    Writec (Cyan, "[Contract: ", Magenta, "(empty)", Cyan, "]");
                    break;
                }
                WriteLinec (Cyan, "[Contract]");
                PushIndentState ();
                AllowIndent ();
                PushFreeze ();
                UnfreezeIndent ();
                Indent ();
                for (var i = 0; i < iodineContract.RequiredMethods.Count; i++) {
                    if (i > 0) {
                        Writecn (", ");
                        WriteLinecn ();
                    }
                    Writec ();
                    FreezeIndent ();
                    WriteStringRepresentation (iodineContract.RequiredMethods [i], depth + 1, true);
                    UnfreezeIndent ();
                }
                Unindent ();
                PopIndentState ();
                PopFreeze ();
                break;
            case "ClassBuilder":
            case "IodineClass":
                var iodineClass = (IodineClass)obj;
                var iodineClassAttrcount = iodineClass.Attributes.Count;
                var iodineClassHasattrs = !obj.Attributes.All (attr => attr.Key.StartsWith ("__", StringComparison.Ordinal));
                //if (hasattrs)
                //    WriteLinec (Darknull, "# begin class ", iodineClass.Name);
                Writec (Cyan, "[Class: ", null, iodineClass.Name);
                if (iodineClass.Interfaces.Count > 0) {
                    Writecn (" implements ");
                    for (var i = 0; i < iodineClass.Interfaces.Count; i++) {
                        if (i > 0)
                            Writecn (", ");
                        Writecn (Cyan, iodineClass.Interfaces [i].Name);
                    }
                }
                Writecn (Cyan, "]");
                if (!iodineClassHasattrs) {
                    break;
                }
                Indent ();
                for (var i = 0; i < iodineClassAttrcount; i++) {
                    var attr = iodineClass.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    if (attr.Key.StartsWith ("__", StringComparison.Ordinal))
                        continue;
                    WriteLinecn ();
                    Writec ();
                    Writecn (attr.Key, ": ");
                    FreezeIndent ();
                    WriteStringRepresentation (attr.Value, depth + 1, true);
                    UnfreezeIndent ();
                }
                //Writec (Darknull, "# end class ", iodineClass.Name);
                Unindent ();
                break;
            default:
                if (obj is IodineTypeDefinition) {
                    Writec (Cyan, "[Type: ", null, ((IodineTypeDefinition)obj).Name, Cyan, "]");
                } else {
                    Writec (Cyan, "[Typedef: ", obj.TypeDef.Name, Magenta, " (CLR: ", obj.GetType ().Name, ")", Cyan, "]");
                }
                if (obj.Attributes.All (attr => attr.Key.StartsWith ("__", StringComparison.Ordinal)))
                    break;

                var iodineTypeAttrcount = obj.Attributes.Count;
                var iodineTypeHasattrs = !obj.Attributes.All (attr => attr.Key.StartsWith ("__", StringComparison.Ordinal));
                if (!iodineTypeHasattrs) {
                    break;
                }
                PushFreeze ();
                UnfreezeIndent ();
                Indent ();
                Writec ();
                for (var i = 0; i < iodineTypeAttrcount; i++) {
                    var attr = obj.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    if (attr.Key.StartsWith ("__", StringComparison.Ordinal))
                        continue;
                    if (i > 0) {
                        WriteLinecn ();
                        Writec ();
                    }
                    Writecn (attr.Key, ": ");
                    FreezeIndent ();
                    WriteStringRepresentation (attr.Value, depth + 1, true);
                    UnfreezeIndent ();
                }
                Unindent ();
                PopFreeze ();
                break;
                /*
                WriteLinecn ();
                //Writec (Darknull, "# begin type ", obj.TypeDef.Name);
                Indent ();
                Writec ();
                for (var i = 0; i < obj.Attributes.Count; i++) {
                    var attr = obj.Attributes.ElementAt (i);
                    if (attr.Value == null || attr.Value.TypeDef == null)
                        continue;
                    if (attr.Key.StartsWith ("__", StringComparison.Ordinal))
                        continue;
                    WriteLinecn ();
                    Writecn (attr.Key, ": ");
                    FreezeIndent ();
                    WriteStringRepresentation (attr.Value, depth + 1, true);
                    UnfreezeIndent ();
                }
                //Writec (Darknull, "# end type ", obj.TypeDef.Name);
                Unindent ();
                break;
                 */
            }

            PopIndentState ();

            return true;
        }
    }
}

