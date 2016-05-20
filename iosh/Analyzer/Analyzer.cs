using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Iodine.Compiler;

namespace iosh {
    
    public class Analyzer {

        AnalyzerSource source;
        SemanticMatcher matcher;

        Analyzer (string source) {
            List<Lexeme> lexemes;
            try {
                lexemes = new Lexer (source).Scan ();
            } catch (Exception e) {
                Console.WriteLine ($"Syntax error: {e.Message}");
                return;
            }
            this.source = new AnalyzerSource (lexemes.Where (l => l.Type != TokenClass.Whitespace));
            matcher = new SemanticMatcher (this.source);
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

            if (source == null)
                return;
            
            while (source.See ()) {

                if (source.Peek ().Type == TokenClass.IoshAnalysisHint)
                    AnalyzerHint.Parse (source.Read ());

                if (matcher.IsMatch ("[any] . __type__")) {
                    var identifier = matcher.GetMatch ().First ();
                    Recommend (matcher.GetMatch ().Last (), $"use type({identifier.Value}) instead of {identifier.Value}.__type__");
                } else if (matcher.IsMatch ("[any] . __len__")) {
                    var identifier = matcher.GetMatch ().First ();
                    Recommend (matcher.GetMatch ().Last (), $"use len({identifier.Value}) instead of {identifier.Value}.__len__");
                } else if (matcher.IsMatch ("[any] . __name__")) {
                    var identifier = matcher.GetMatch ().First ();
                    Recommend (matcher.GetMatch ().Last (), $"use Str(type({identifier.Value})) instead of {identifier.Value}.__name__");
                } else if (matcher.IsMatch ("[str] . format (")) {
                    Recommend (matcher.GetMatch ().Last (), "use string interpolation syntax instead of format");
                } else if (matcher.IsMatch ("foreach (")) {
                    Warn (matcher.GetMatch ().First (), "foreach is deprecated, use for instead");
                } else if (matcher.IsMatch ("given (") || matcher.IsMatch ("when [any]")) {
                    Warn (matcher.GetMatch ().First (), "given/when is deprecated, use match/case instead");
                } else if (matcher.IsMatch ("= { }")) {
                    Recommend (matcher.GetMatch ().First (), "Use Dict() instead of {} to create an empty dictionary");
                }

                if (source.See ())
                    source.Skip ();
            }
        }

        void Recommend (Lexeme lex, string what) {
            foreach (var hint in AnalyzerHint.Flags) {
                if (hint.HasFlag (AnalyzerFlags.Disable) && hint.HasFlag (AnalyzerFlags.Recommendations))
                    return;
            }
            Console.WriteLine ($"[Recommended at {lex.Location}]: {what}");
        }

        void Warn (Lexeme lex, string what) {
            foreach (var hint in AnalyzerHint.Flags) {
                if (hint.HasFlag (AnalyzerFlags.Disable) && hint.HasFlag (AnalyzerFlags.Warnings))
                    return;
            }
            Console.WriteLine ($"[Warning at {lex.Location}]: {what}");
        }
    }
}

