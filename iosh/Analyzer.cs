using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Iodine.Compiler;

namespace iosh {
    
    public class Analyzer {

        AnalyzerSource source;

        Analyzer (string source) {
            List<Lexeme> lexemes;
            try {
                lexemes = new Lexer (source).Scan ();
            } catch (Exception e) {
                Console.WriteLine ($"Syntax error: {e.Message}");
                return;
            }
            this.source = new AnalyzerSource (lexemes);
        }

        public static Analyzer Create (string source) {
            Contract.Ensures (Contract.Result<Analyzer> () != null);
            return new Analyzer (source);
        }

        public static Analyzer Create (SourceUnit source) {
            Contract.Ensures (Contract.Result<Analyzer> () != null);
            return Create (source.Text);
        }

        public void Run () {

            while (source.See (1)) {
                if (source.See (3)
                    && (source.Peek (0).Is (TokenClass.StringLiteral)
                        || source.Peek (0).Is (TokenClass.InterpolatedStringLiteral))
                    && source.Peek (1).Is (".")
                    && source.Peek (2).Is ("format")) {
                    Recommend (source.Peek (1), $"Use string interpolation syntax instead of format");
                }
                if (source.See (2)
                    && source.Peek (0).Is (TokenClass.Identifier)
                    && source.Peek (1).Is (".")
                    && source.Peek (2).Is (TokenClass.Identifier)) {
                    var obj = source.Peek ();
                    var identifier = source.Peek (2);
                    if (identifier.Is ("__type__")) {
                        Recommend (identifier, $"Use type({obj.Value}) instead of {obj.Value}.__type__");
                    } else if (identifier.Is ("__len__")) {
                        Recommend (identifier, $"Use len({obj.Value}) instead of {obj.Value}.__len__");
                    }
                    source.Skip (3);
                }
                if (source.See ())
                    source.Skip ();
            }
        }

        void Recommend (Lexeme lex, string what) {
            Console.WriteLine ($"{lex.Location}: {what}");
        }
    }
}

