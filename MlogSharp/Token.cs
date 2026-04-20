using System;
using System.Collections.Generic;

namespace MlogSharp
{
    public enum TokenType
    {
        Number, Identifier, StringLiteral,
        Plus, Minus, Star, Slash, Equals,
        OpenParen, CloseParen, OpenBrace, CloseBrace,
<<<<<<< проект-обзор-и-анализ-4601b
        OpenBracket, CloseBracket,
        Comma, Semicolon,
        EqualEqual, NotEqual, LessThan, GreaterThan, LessThanEq, GreaterThanEq,
        Function, Return, Print, If, Else, While, For, AsmBlock, Array,
=======
        Comma, Semicolon,
        EqualEqual, NotEqual, LessThan, GreaterThan, LessThanEq, GreaterThanEq,
        Function, Return, Print, If, Else, While, For, AsmBlock,
>>>>>>> master
        EndOfFile
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Text { get; }
        public int Line { get; }

        public Token(TokenType type, string text, int line) =>
            (Type, Text, Line) = (type, text, line);

        public override string ToString() => $"{Type}: '{Text}'";
    }
}
