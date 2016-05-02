using System;
using System.Diagnostics.Contracts;

namespace iosh {
    
    public class LexerSource {

        int pos;
        int line;
        int linepos;
        string source;

        public int Line => line;
        public int Linepos => linepos;

        public LexerSource (string source) {
            line = 1;
            this.source = source;
        }

        public string Location => $"{line}:{linepos}";

        public void Skip (int n = 1) {
            Contract.Assert (See (n));
            for (var i = 0; i < n; i++) {
                var c = Peek (i);
                if (c == '\n') {
                    line++;
                    linepos = 0;
                }
                else
                    linepos++;
            }
            pos += n;
        }

        public void SkipWhitespace () {
            while (See (1) && char.IsWhiteSpace (Peek ()))
                Skip ();
        }

        public void SkipLine () {
            while (Peek () != '\n')
                Skip ();
        }

        public bool See (int lookahead = 0) => pos + lookahead < source.Length;

        public char Peek (int lookahead = 0) {
            Contract.Assert (See (lookahead));
            return source [pos + lookahead];
        }

        public string Peeks (int count, int lookahead = 0) {
            Contract.Assert (See (count + lookahead));
            return source.Substring (pos + lookahead, count);
        }

        public char Read (int lookahead = 0) {
            var c = Peek (lookahead);
            Skip (1 + lookahead);
            return c;
        }

        public string Reads (int count, int lookahead = 0) {
            var str = Peeks (count, lookahead);
            Skip (lookahead + count);
            return str;
        }

        void assert (bool condition) {
            if (!condition)
                throw new Exception ();
        }
    }
}

