using System;
using System.Collections.Generic;
using System.Text;

namespace MlogSharp
{
    public enum TokenType
    {
        Number,
        Identifier,
        StringLiteral, // "hello"

        // Operators
        Plus, Minus, Star, Slash, Equals,
        OpenParen, CloseParen,
        OpenBrace, CloseBrace,
        Comma, Semicolon,

        EqualEqual,      // ==
        NotEqual,        // !=
        LessThan,        // <
        GreaterThan,     // >
        LessThanEq,      // <=
        GreaterThanEq,   // >=

        // Keywords
        Function, Return, Print, If, Else, While, For, Asm, AsmBlock, // на будущее

        EndOfFile
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Text { get; }
        public int Line { get; }

        public Token(TokenType type, string text, int line)
        {
            Type = type;
            Text = text;
            Line = line;
        }

        public override string ToString() => $"{Type}: '{Text}'";
    }
}
