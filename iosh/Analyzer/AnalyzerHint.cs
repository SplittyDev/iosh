using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace iosh {
    
    public class AnalyzerHint {

        public static List<AnalyzerFlags> Flags;

        static AnalyzerHint () {
            Flags = new List<AnalyzerFlags> ();
        }

        public static void Parse (Lexeme lex) {
            var hint = new AnalyzerHint (lex);
            Flags.Add (hint.ParseHintData ());
        }

        readonly string action;
        readonly string name;
        readonly Lexeme lexeme;

        public string Action => Action;
        public string Name => name;

        AnalyzerHint (Lexeme lex) {
            Contract.Assert (lex.Type == TokenClass.IoshAnalysisHint);
            var parts = lex.Value.Split (':');
            Contract.Assert (parts.Length == 2);
            action = parts [0].Trim ();
            name = parts [1].Trim ().ToLowerInvariant ();
            lexeme = lex;
        }

        AnalyzerFlags ParseHintData () {
            var flags = AnalyzerFlags.None;
            try {
                var faction = (AnalyzerFlags) Enum.Parse (typeof (AnalyzerFlags), action, true);
                var fname = (AnalyzerFlags) Enum.Parse (typeof (AnalyzerFlags), name, true);
                foreach (var current in Flags) {
                    if (current.HasFlag (fname))
                        Flags.Remove (current);
                }
                flags |= faction;
                flags |= fname;
            } catch {
                Console.WriteLine ($"Invalid analyzer hint at {lexeme.Location}");
            }
            return flags;
        }
    }
}