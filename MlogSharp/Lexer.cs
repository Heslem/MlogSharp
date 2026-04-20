using System;
using System.Collections.Generic;

namespace MlogSharp
{
    public class Lexer
    {
        private readonly string _source;
        private int _position, _line;

        public Lexer(string source) => (_source, _position, _line) = (source, 0, 1);

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (_position < _source.Length)
            {
                char c = _source[_position];
                if (char.IsWhiteSpace(c))
                {
                    if (c == '\n') _line++;
                    _position++;
                    continue;
                }
                if (char.IsDigit(c) || (c == '.' && _position + 1 < _source.Length && char.IsDigit(_source[_position + 1])))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }
                if (c == '"') { tokens.Add(ReadString()); continue; }
                if (char.IsLetter(c) || c == '_')
                {
                    if (StartsWithKeyword("asm"))
                    {
                        int tempPos = _position + 3;
                        while (tempPos < _source.Length && char.IsWhiteSpace(_source[tempPos]))
                        {
                            if (_source[tempPos] == '\n') _line++;
                            tempPos++;
                        }
                        if (tempPos < _source.Length && _source[tempPos] == '{')
                        {
                            tokens.Add(ReadAsmBlock());
                            continue;
                        }
                    }
                    tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }
                switch (c)
                {
                    case '+': tokens.Add(new Token(TokenType.Plus, "+", _line)); break;
                    case '-': tokens.Add(new Token(TokenType.Minus, "-", _line)); break;
                    case '*': tokens.Add(new Token(TokenType.Star, "*", _line)); break;
                    case '/':
                        if (PeekChar() == '/') { SkipLineComment(); continue; }
                        tokens.Add(new Token(TokenType.Slash, "/", _line));
                        break;
                    case '=':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.EqualEqual, "==", _line)); }
                        else tokens.Add(new Token(TokenType.Equals, "=", _line));
                        break;
                    case '!':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.NotEqual, "!=", _line)); }
                        else throw new Exception($"Unexpected character: ! at line {_line}");
                        break;
                    case '<':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.LessThanEq, "<=", _line)); }
                        else tokens.Add(new Token(TokenType.LessThan, "<", _line));
                        break;
                    case '>':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.GreaterThanEq, ">=", _line)); }
                        else tokens.Add(new Token(TokenType.GreaterThan, ">", _line));
                        break;
                    case '(': tokens.Add(new Token(TokenType.OpenParen, "(", _line)); break;
                    case ')': tokens.Add(new Token(TokenType.CloseParen, ")", _line)); break;
                    case '[': tokens.Add(new Token(TokenType.OpenBracket, "[", _line)); break;
                    case ']': tokens.Add(new Token(TokenType.CloseBracket, "]", _line)); break;
                    case '{': tokens.Add(new Token(TokenType.OpenBrace, "{", _line)); break;
                    case '}': tokens.Add(new Token(TokenType.CloseBrace, "}", _line)); break;
                    case ',': tokens.Add(new Token(TokenType.Comma, ",", _line)); break;
                    case ';': tokens.Add(new Token(TokenType.Semicolon, ";", _line)); break;
                    default: throw new Exception($"Unknown character: {c} at line {_line}");
                }
                _position++;
            }
            tokens.Add(new Token(TokenType.EndOfFile, "", _line));
            return tokens;
        }

        private bool StartsWithKeyword(string keyword)
        {
            if (_position + keyword.Length > _source.Length) return false;
            for (int i = 0; i < keyword.Length; i++)
                if (_source[_position + i] != keyword[i]) return false;
            char nextChar = _source[_position + keyword.Length];
            return !(char.IsLetterOrDigit(nextChar) || nextChar == '_');
        }

        private Token ReadAsmBlock()
        {
            int startLine = _line;
            _position += 3;
            while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
            {
                if (_source[_position] == '\n') _line++;
                _position++;
            }
            if (_position >= _source.Length || _source[_position] != '{')
                throw new Exception("Expected '{' after asm at line " + _line);
            _position++;
            int braceCount = 1, startIndex = _position;
            while (_position < _source.Length && braceCount > 0)
            {
                char c = _source[_position];
                if (c == '\n') _line++;
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                _position++;
            }
            if (braceCount > 0) throw new Exception("Unclosed asm block starting at line " + startLine);
            return new Token(TokenType.AsmBlock, _source.Substring(startIndex, (_position - 1) - startIndex), startLine);
        }

        private void SkipLineComment()
        {
            _position += 2;
            while (_position < _source.Length && _source[_position] != '\n') _position++;
        }

        private char PeekChar() => _position + 1 < _source.Length ? _source[_position + 1] : '\0';

        private Token ReadNumber()
        {
            int start = _position;
            while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.')) _position++;
            return new Token(TokenType.Number, _source.Substring(start, _position - start), _line);
        }

        private Token ReadIdentifierOrKeyword()
        {
            int start = _position;
            while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_')) _position++;
            string text = _source.Substring(start, _position - start);
            TokenType type = text switch
            {
                "function" => TokenType.Function,
                "return" => TokenType.Return,
                "print" => TokenType.Print,
                "if" => TokenType.If,
                "else" => TokenType.Else,
                "while" => TokenType.While,
                "for" => TokenType.For,
                "array" => TokenType.Array,
                _ => TokenType.Identifier
            };
            return new Token(type, text, _line);
        }

        private Token ReadString()
        {
            _position++;
            int start = _position;
            while (_position < _source.Length && _source[_position] != '"')
            {
                if (_source[_position] == '\n') _line++;
                _position++;
            }
            if (_position >= _source.Length) throw new Exception("Unclosed string");
            string text = _source.Substring(start, _position - start);
            _position++;
            return new Token(TokenType.StringLiteral, text, _line);
        }
    }
}