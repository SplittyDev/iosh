using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace iosh {
    
    public class AnalyzerSource {
        
        readonly Lexeme [] source;
        int pos;

        public AnalyzerSource (IEnumerable<Lexeme> lexemes) {
            source = lexemes.ToArray ();
        }

        public void Skip (int n = 1) {
            Contract.Assert (See (n));
            pos += n;
        }

        public bool See (int lookahead = 1) => pos + lookahead < source.Length;

        public Lexeme Peek (int lookahead = 0) {
            Contract.Ensures (Contract.Result<Lexeme> () != null);
            Contract.Assert (See (lookahead));
            return source [pos + lookahead];
        }

        public Lexeme Read (int lookahead = 0) {
            Contract.Ensures (Contract.Result<Lexeme> () != null);
            var lex = Peek (lookahead);
            Skip (lookahead);
            return lex;
        }
    }
}

