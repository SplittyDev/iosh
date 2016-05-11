using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace iosh {

    public class Lexer {

        readonly LexerSource source;
        readonly List<Lexeme> lexemes;

        readonly char [] OperatorChars = "+-*/=<>~!&^|%@?.".ToCharArray ();
        readonly string [] OperatorStrings = "is,isnot,as".Split (',');
        readonly string [] Keywords = {
                "if",
                "else",
                "do",
                "while",
                "for",
                "in",
                "func",
                "lambda",
                "class",
                "super",
                "self",
                "true",
                "false",
                "break",
                "continue",
                "with",
                "null",
                "enum",
                "contract",
                "trait",
                "raise",
                "try",
                "except",
                "match",
                "case",
                "default",
                "yield",
                "return",
                "use",
                "from",
                "with",
                "var"
        };

        public Lexer (string source) {
            lexemes = new List<Lexeme> ();
            this.source = new LexerSource (source);
        }

        public List<Lexeme> Scan () {
            while (source.See (1))
                ScanToken ();
            return lexemes;
        }

        void ScanToken () {
            var c = source.Peek ();
            if (char.IsWhiteSpace (source.Peek ())) {
                source.SkipWhitespace ();
                lexemes.Add (new Lexeme (TokenClass.Whitespace, source, string.Empty));
                return;
            }
            if (source.See (2) && source.Peeks (2) == "0x") {
                ReadHexNumber ();
                return;
            }
            if (char.IsDigit (c)) {
                ReadNumber ();
                return;
            }
            if (OperatorChars.Contains (c)) {
                ReadOperator ();
                return;
            }
            var dict = new Dictionary<char, TokenClass> {
                    { '{', TokenClass.OpenBrace },
                    { '}', TokenClass.CloseBrace },
                    { '(', TokenClass.OpenParen },
                    { ')', TokenClass.CloseParen },
                    { '[', TokenClass.OpenBracket },
                    { ']', TokenClass.CloseBracket },
                    { ';', TokenClass.Semicolon },
                    { ':', TokenClass.Colon },
                    { ',', TokenClass.Comma }
                };
            if (dict.ContainsKey (c)) {
                if (source.See (1))
                    source.Skip ();
                if (c == '{')
                    source.OpenBrace ();
                else if (c == '}')
                    source.CloseBrace ();
                lexemes.Add (new Lexeme (dict [c], source, c));
                return;
            }
            switch (c) {
            case '#':
                source.Skip ();
                source.SkipLine ();
                break;
            case '\'':
            case '"':
                ReadString ();
                break;
            default:
                if (source.See (1) && c == 'b' && (source.Peek (1) == '\"' || source.Peek (1) == '\'')) {
                    ReadBinaryString ();
                    break;
                }
                if (char.IsLetter (c) || c == '_') {
                    ReadIdentifier ();
                    break;
                }
                throw new Exception ($"Unexpected token: '{c}' at {source.Location}");
            }
        }

        string JustReadString (out char delimiter) {
            var accum = new StringBuilder ();
            delimiter = source.Read ();
            var c = source.Peek ();
            while (source.See () && c != delimiter) {
                c = source.Read ();
                if (c == '\\') {
                    var next = source.Peek ();
                    switch (next) {
                    case '\"':
                    case '\'':
                        c = next;
                        break;
                    case 'n':
                        c = '\n';
                        break;
                    case 'r':
                        c = '\r';
                        break;
                    case 'b':
                        c = '\b';
                        break;
                    case 't':
                        c = '\t';
                        break;
                    case 'f':
                        c = '\f';
                        break;
                    default:
                        throw new Exception ($"Unrecognized escape sequence: '\\{next}'");
                    }
                    source.Skip ();
                }
                /*
                else if (delimiter == '"' && c == '#' && source.Peek () == '{') {
                    source.Skip ();
                    var balance = source.BraceBalance;
                    while (lexemes.Last ().Value != "}" && source.BraceBalance - balance == 0)
                        ScanToken ();
                    source.Skip ();
                    source.CloseBrace ();
                    continue;
                }
                */
                accum.Append (c);
                c = source.Peek ();
            }
            if (c != delimiter)
                throw new Exception ($"Unterminated string literal at {source.Location}");
            if (source.See ())
                source.Skip ();
            return accum.ToString ();
        }

        void ReadString () {
#if DEBUG
            Console.WriteLine (new StackFrame ().GetMethod ().Name);
#endif
            char delimiter;
            var str = JustReadString (out delimiter);
            var tokenclass = delimiter == '"' ? TokenClass.InterpolatedStringLiteral : TokenClass.StringLiteral;
            lexemes.Add (new Lexeme (tokenclass, source, str));
        }

        void ReadBinaryString () {
#if DEBUG
            Console.WriteLine (new StackFrame ().GetMethod ().Name);
#endif
            char _;
            var str = JustReadString (out _);
            lexemes.Add (new Lexeme (TokenClass.BinaryStringLiteral, source, str));
        }

        void ReadNumber () {
#if DEBUG
            Console.WriteLine (new StackFrame ().GetMethod ().Name);
#endif
            var accum = new StringBuilder ();
            var c = source.Peek ();
            var isfloat = false;
            while (source.See () && (char.IsDigit (c) || c == '.')) {
                if (c == '.' && !isfloat)
                    isfloat = true;
                else if (c == '.')
                    throw new Exception ("Floating point number can only have one decimal point");
                accum.Append (c);
                source.Skip ();
                c = source.Peek ();
            }
            if (isfloat) {
                lexemes.Add (new Lexeme (TokenClass.FloatLiteral, source, accum));
                return;
            }
            lexemes.Add (new Lexeme (TokenClass.IntLiteral, source, accum));
        }

        void ReadHexNumber () {
#if DEBUG
            Console.WriteLine (new StackFrame ().GetMethod ().Name);
#endif
            var accum = new StringBuilder ();
            source.Skip (2);
            var c = source.Peek ();
            while (source.See () && (char.IsDigit (c) || "abcdefABCDEF".Contains (c.ToString ()))) {
                accum.Append (c);
                source.Skip ();
                c = source.Peek ();
            }
            lexemes.Add (new Lexeme (TokenClass.IntLiteral, source, long.Parse (accum.ToString (), NumberStyles.HexNumber).ToString ()));
        }

        void ReadOperator () {
#if DEBUG
            Console.WriteLine (new StackFrame ().GetMethod ().Name);
#endif
            var op = source.Peek ();
            string op2 = source.Peeks (2);
            string op3 = source.Peeks (3);
            switch (op3) {
            case "<<=":
            case ">>=":
            case "...":
                source.Skip (3);
                lexemes.Add (new Lexeme (TokenClass.Operator, source, op3));
                break;
            }
            switch (op2) {
            case ">>":
            case "<<":
            case "&&":
            case "||":
            case "==":
            case "!=":
            case "=>":
            case "<=":
            case ">=":
            case "+=":
            case "-=":
            case "*=":
            case "/=":
            case "%=":
            case "&=":
            case "^=":
            case "|=":
            case "??":
            case "..":
                source.Skip (2);
                lexemes.Add (new Lexeme (TokenClass.Operator, source, op2));
                break;
            case ".?":
                source.Skip (2);
                lexemes.Add (new Lexeme (TokenClass.MemberDefaultAccess, source, op2));
                break;
            case "/*":
                source.Skip (2);
                var nn = source.Peeks (2);
                while (nn != "*/") {
                    source.Skip ();
                    nn = source.Peeks (2);
                }
                break;
            }
            source.Skip ();
            if (op == '.') {
                lexemes.Add (new Lexeme (TokenClass.MemberAccess, source, op));
                return;
            }
            lexemes.Add (new Lexeme (TokenClass.Operator, source, op.ToString ()));
        }

        void ReadIdentifier () {
#if DEBUG
            Console.WriteLine (new StackFrame ().GetMethod ().Name);
#endif
            var accum = new StringBuilder ();
            var c = source.Peek ();
            while (char.IsLetterOrDigit (c) || c == '_') {
                accum.Append (c);
                source.Skip ();
                c = source.Peek ();
            }
            var str = accum.ToString ();
            if (Keywords.Contains (str)) {
                lexemes.Add (new Lexeme (TokenClass.Keyword, source, str));
                return;
            }
            if (OperatorStrings.Contains (str)) {
                lexemes.Add (new Lexeme (TokenClass.Operator, source, str));
                return;
            }
            lexemes.Add (new Lexeme (TokenClass.Identifier, source, str));
        }
    }
}

