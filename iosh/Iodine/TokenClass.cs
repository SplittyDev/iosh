using System;

namespace iosh {
    
    public enum TokenClass {
        Whitespace,
        StringLiteral,
        BinaryStringLiteral,
        InterpolatedStringLiteral,
        FloatLiteral,
        IntLiteral,
        Keyword,
        Operator,
        Identifier,
        MemberAccess,
        MemberDefaultAccess,
        OpenBrace,
        CloseBrace,
        OpenParen,
        CloseParen,
        OpenBracket,
        CloseBracket,
        Semicolon,
        Colon,
        Comma,
        IoshAnalysisHint,
    }
}

