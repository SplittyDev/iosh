using System;
using System.Text;

namespace iosh {
    
    public class Lexeme {

        public TokenClass Type;
        public string Value;
        public object LiteralValue;
        public string Location;

        public Lexeme (TokenClass type, LexerSource src, string value, object literal = null) {
            Type = type;
            Value = value;
            LiteralValue = literal ?? value;
            Location = src.Location;
        }

        public Lexeme (TokenClass type, LexerSource src, StringBuilder value, object literal = null)
            : this (type, src, value.ToString (), literal) { }

        public Lexeme (TokenClass type, LexerSource src, char value, object literal = null)
            : this (type, src, value.ToString (), literal) { }

        public bool Is (TokenClass type) {
            return Type == type;
        }

        public bool Is (string str) {
            return Value == str;
        }

        public override string ToString () {
            return $"[Lexeme: Type={Type.ToString ()}, Value={Value}]";
        }
    }
}

