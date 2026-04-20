using System;
using System.Collections.Generic;
using System.Text;

namespace MlogSharp
{
    public class Lexer
    {
        private readonly string _source;
        private int _position;
        private int _line;

        public Lexer(string source)
        {
            _source = source;
            _position = 0;
            _line = 1;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (_position < _source.Length)
            {
                char current = _source[_position];

                // Пропускаем пробелы и переносы
                if (char.IsWhiteSpace(current))
                {
                    if (current == '\n') _line++;
                    _position++;
                    continue;
                }

                // Числа
                if (char.IsDigit(current) || (current == '.' && _position + 1 < _source.Length && char.IsDigit(_source[_position + 1])))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                // Строки
                if (current == '"')
                {
                    tokens.Add(ReadString());
                    continue;
                }

                // Идентификаторы и Ключевые слова (включая asm)
                if (char.IsLetter(current) || current == '_')
                {
                    // Специальная проверка на asm block
                    if (StartsWithKeyword("asm"))
                    {
                        // Проверяем, идет ли после 'asm' открывающая скобка (возможно, через пробелы)
                        int tempPos = _position + 3; // длина "asm"

                        // Пропускаем пробелы после asm
                        while (tempPos < _source.Length && char.IsWhiteSpace(_source[tempPos]))
                        {
                            if (_source[tempPos] == '\n') _line++; // коррекция строки не нужна, так как мы не двигаем основной _position, но если бы двигали...
                            // Тут важно: мы пока не двигаем _position, просто смотрим.
                            // Но если мы решим, что это asm блок, нам нужно будет сдвинуть _position.
                            tempPos++;
                        }

                        if (tempPos < _source.Length && _source[tempPos] == '{')
                        {
                            // Это точно asm блок!
                            tokens.Add(ReadAsmBlock());
                            continue;
                        }
                    }

                    // Иначе читаем как обычный идентификатор/ключевое слово
                    tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }

                // Операторы и символы
                switch (current)
                {
                    case '+': tokens.Add(new Token(TokenType.Plus, "+", _line)); break;
                    case '-': tokens.Add(new Token(TokenType.Minus, "-", _line)); break;
                    case '*': tokens.Add(new Token(TokenType.Star, "*", _line)); break;
                    case '/':
                        // Проверка на комментарии //
                        if (PeekChar() == '/')
                        {
                            SkipLineComment();
                            continue;
                        }
                        tokens.Add(new Token(TokenType.Slash, "/", _line));
                        break;

                    case '=':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.EqualEqual, "==", _line)); }
                        else { tokens.Add(new Token(TokenType.Equals, "=", _line)); }
                        break;

                    case '!':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.NotEqual, "!=", _line)); }
                        else { throw new Exception($"Unexpected character: ! at line {_line}"); }
                        break;

                    case '<':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.LessThanEq, "<=", _line)); }
                        else { tokens.Add(new Token(TokenType.LessThan, "<", _line)); }
                        break;

                    case '>':
                        if (PeekChar() == '=') { _position++; tokens.Add(new Token(TokenType.GreaterThanEq, ">=", _line)); }
                        else { tokens.Add(new Token(TokenType.GreaterThan, ">", _line)); }
                        break;

                    case '(': tokens.Add(new Token(TokenType.OpenParen, "(", _line)); break;
                    case ')': tokens.Add(new Token(TokenType.CloseParen, ")", _line)); break;
                    case '{': tokens.Add(new Token(TokenType.OpenBrace, "{", _line)); break;
                    case '}': tokens.Add(new Token(TokenType.CloseBrace, "}", _line)); break;
                    case ',': tokens.Add(new Token(TokenType.Comma, ",", _line)); break;
                    case ';': tokens.Add(new Token(TokenType.Semicolon, ";", _line)); break;

                    default:
                        throw new Exception($"Unknown character: {current} at line {_line}");
                }
                _position++;
            }

            tokens.Add(new Token(TokenType.EndOfFile, "", _line));
            return tokens;
        }

        // --- Helpers ---

        private bool StartsWithKeyword(string keyword)
        {
            if (_position + keyword.Length > _source.Length) return false;

            // Проверяем совпадение символов
            for (int i = 0; i < keyword.Length; i++)
            {
                if (_source[_position + i] != keyword[i]) return false;
            }

            // Проверяем, что дальше идет не буква/цифра (чтобы 'asmX' не считался 'asm')
            char nextChar = _source[_position + keyword.Length];
            if (char.IsLetterOrDigit(nextChar) || nextChar == '_') return false;

            return true;
        }

        private Token ReadAsmBlock()
        {
            int startLine = _line;
            // Пропускаем само слово "asm"
            _position += 3;

            // Пропускаем пробелы до {
            while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
            {
                if (_source[_position] == '\n') _line++;
                _position++;
            }

            // Теперь мы должны быть на {
            if (_position >= _source.Length || _source[_position] != '{')
            {
                throw new Exception("Expected '{' after asm at line " + _line);
            }

            _position++; // пропускаем {

            int braceCount = 1;
            int startIndex = _position;

            while (_position < _source.Length && braceCount > 0)
            {
                char c = _source[_position];
                if (c == '\n') _line++;

                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;

                _position++;
            }

            if (braceCount > 0)
            {
                throw new Exception("Unclosed asm block starting at line " + startLine);
            }

            // Вырезаем содержимое между { и }
            // _position сейчас указывает на символ ПОСЛЕ закрывающей }
            // Длина контента = (_position - 1) - startIndex
            string content = _source.Substring(startIndex, (_position - 1) - startIndex);

            return new Token(TokenType.AsmBlock, content, startLine);
        }

        private void SkipLineComment()
        {
            // Пропускаем // и всё до конца строки
            _position += 2;
            while (_position < _source.Length && _source[_position] != '\n')
            {
                _position++;
            }
        }

        private char PeekChar()
        {
            if (_position + 1 < _source.Length)
                return _source[_position + 1];
            return '\0';
        }

        private Token ReadNumber()
        {
            int start = _position;
            while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.'))
            {
                _position++;
            }
            string text = _source.Substring(start, _position - start);
            return new Token(TokenType.Number, text, _line);
        }

        private Token ReadIdentifierOrKeyword()
        {
            int start = _position;
            while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
            {
                _position++;
            }
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
                // "asm" сюда не попадет, так как мы обработали его выше
                _ => TokenType.Identifier
            };

            return new Token(type, text, _line);
        }

        private Token ReadString()
        {
            _position++; // skip opening quote
            int start = _position;
            while (_position < _source.Length && _source[_position] != '"')
            {
                if (_source[_position] == '\n') _line++;
                _position++;
            }
            if (_position >= _source.Length) throw new Exception("Unclosed string");

            string text = _source.Substring(start, _position - start);
            _position++; // skip closing quote
            return new Token(TokenType.StringLiteral, text, _line);
        }
    }
}