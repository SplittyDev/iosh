using System;
using System.Collections.Generic;
using System.Text;

namespace iosh {
    public class SemanticMatcher {

        AnalyzerSource source;
        Lexeme [] last;
        
        public SemanticMatcher (AnalyzerSource source) {
            this.source = source;
        }

        /// <summary>
        /// Matches the specified pattern.
        /// </summary>
        /// <remarks>
        /// Usage:
        ///    Identifier :  [id]
        ///    Operator   :  [op]
        ///    String     :  [str]
        ///    Number     :  [num]
        ///    Anything   :  [any]
        ///    Separator  :  space
        ///    Literal    :   ...
        /// </remarks>
        /// <param name="patternString">Pattern.</param>
        public bool IsMatch (string patternString) {
            last = new Lexeme [0];
            var pattern = new LexerSource (patternString);
            var tmp = new List<Lexeme> ();
            var matchees = new Queue<Matchee> ();
            while (pattern.See ()) {
                var c = pattern.Peek ();
                if (c == '[') {
                    pattern.Skip ();
                    var accum = new StringBuilder ();
                    while (pattern.See () && pattern.Peek () != ']')
                        accum.Append (pattern.Read ());
                    if (pattern.See (0) && pattern.Peek () != ']')
                        return false;
                    pattern.Skip ();
                    var str = accum.ToString ();
                    switch (str) {
                    case "id":
                        matchees.Enqueue (new Matchee (TokenClass.Identifier));
                        break;
                    case "op":
                        matchees.Enqueue (new Matchee (TokenClass.Operator));
                        break;
                    case "any":
                        matchees.Enqueue (new Matchee ());
                        break;
                    case "str":
                        matchees.Enqueue (new Matchee (TokenClass.StringLiteral,
                                                       TokenClass.BinaryStringLiteral,
                                                       TokenClass.InterpolatedStringLiteral));
                        break;
                    case "num":
                        matchees.Enqueue (new Matchee (TokenClass.IntLiteral,
                                                       TokenClass.FloatLiteral));
                        break;
                    }
                } else if (pattern.Linepos == 0 || c == ' ') {
                    if (pattern.Linepos > 0 && source.See ())
                        pattern.Skip ();
                    var accum = new StringBuilder ();
                    while (pattern.See () && pattern.Peek () != ' ')
                        accum.Append (pattern.Read ());
                    if (pattern.See (0) && pattern.Peek () != ' ')
                        accum.Append (pattern.Read ());
                    var str = accum.ToString ();
                    matchees.Enqueue (new Matchee (str));
                }
            }
            if (!source.See (matchees.Count))
                return false;
            var i = 0;
            while (matchees.Count > 0) {
                var current = matchees.Dequeue ();
                // Console.WriteLine ($"Matching {current} against {source.Peek (i)}");
                if (current.HasTypes) {
                    var result = true;
                    foreach (var type in current.TokenTypes)
                        result &= source.Peek (i).Is (type);
                    if (!result)
                        return false;
                } else if (current.HasValue) {
                    if (!source.Peek (i).Is (current.Value))
                        return false;
                }
                if (!source.See (i))
                    return false;
                tmp.Add (source.Peek (i));
                i++;
            }
            last = tmp.ToArray ();
            return true;
        }

        public Lexeme [] GetMatch () {
            return last;
        }

        struct Matchee {
            public TokenClass [] TokenTypes;
            public string Value;

            public bool HasTypes => TokenTypes != null && TokenTypes.Length > 0;
            public bool HasValue => !string.IsNullOrEmpty (Value);

            public Matchee (params TokenClass [] tokenClasses) : this () {
                TokenTypes = tokenClasses;
                Value = null;
            }

            public Matchee (string value) : this () {
                TokenTypes = null;
                Value = value;
            }

            public override string ToString () {
                return $"[Matchee: HasTypes={HasTypes}, Value={Value ?? "(null)"}]";
            }
        }
    }
}

