using System;
using System.IO;
using System.Text;

namespace ConsoleApp1
{
    public enum TokenKind
    {
        Unknown=0, Name=1, Punctuation, SingleQuotLiteral, DoubleQuotLiteral, NumericLiteral, Comment, Whitespace
    }

    public abstract class SyntaxToken
    {
        public abstract TokenKind Kind {get;}
        public abstract bool HasStart(TokenReader reader);
        public abstract bool HasContinuation(string prevPart, TokenReader reader);        
    }

    public class NameToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Name;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsLetter(c);
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsLetterOrDigit(c);
        }
    }

    public class PunctuationToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Punctuation;

        static bool IsPunctuation(char c)
        {
            return (char.IsPunctuation(c) || char.IsSymbol(c)) && c!='\'' && c!='"';
        }

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            if (c == '/')
            {
                char[] chars = reader.PeekChars(2);
                if (chars.Length >= 2 && (chars[1] == '*' || chars[1] == '/')) return false;
                else return true;
            }
            else
            {
                return IsPunctuation(c);
            }
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length >= 1) return false;
            else return true;
        }
    }

    public class WhitespaceToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Whitespace;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsWhiteSpace(c);
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsWhiteSpace(c);
        }
    }

    public class NumericLiteralToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.NumericLiteral;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();

            return char.IsDigit(c);
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            char c = reader.PeekChar();
            return char.IsDigit(c) || c=='.';
        }
    }

    public class DoubleQuotLiteralToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.DoubleQuotLiteral;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c=='"';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 1) return true;

            char c = prevPart[prevPart.Length - 1];

            if (c == '"')
            {
                if (prevPart[prevPart.Length - 2] == '\\') return true; //escaped
                else return false;
            }
            else return true;
        }
    }

    public class SingleQuotLiteralToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.SingleQuotLiteral;

        public override bool HasStart(TokenReader reader)
        {
            char c = reader.PeekChar();
            return c == '\'';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 1) return true;

            char c = prevPart[prevPart.Length - 1];

            if (c == '\'')
            {
                if (prevPart[prevPart.Length - 2] == '\\') return true; //escaped
                else return false;
            }
            else return true;
        }
    }

    public class CommentToken : SyntaxToken
    {
        public override TokenKind Kind => TokenKind.Comment;

        public override bool HasStart(TokenReader reader)
        {
            /**/
            char[] chars = reader.PeekChars(2);
            if (chars.Length < 2) return false;

            return chars[0] == '/' && chars[1] == '*';
        }

        public override bool HasContinuation(string prevPart, TokenReader reader)
        {
            if (prevPart.Length <= 2) return true;

            char c1 = prevPart[prevPart.Length - 2];
            char c2 = prevPart[prevPart.Length - 1];
            return !(c1=='*' && c2== '/');
        }
    }

    public class TokenReader
    {
        char[] _source;
        int _pos = 0;

        public TokenReader(string src)
        {
            this._source = src.ToCharArray();
        }

        public string ReadToken()
        {
            StringBuilder sb = new StringBuilder();
            SyntaxToken currentToken=null;
            SyntaxToken[] tokens = new SyntaxToken[] {
                new NameToken(), new PunctuationToken(), new WhitespaceToken(), new NumericLiteralToken(),
                new DoubleQuotLiteralToken(), new SingleQuotLiteralToken(), new CommentToken()
            };

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].HasStart(this))
                {
                    currentToken = tokens[i];
                    break;
                }
            }

            if (currentToken == null) return string.Empty;

            while (true)
            {
                char c = this.ReadChar();

                if(c==(char)0) break;

                sb.Append(c);

                if (!currentToken.HasContinuation(sb.ToString(), this))
                {
                    break;
                }
            }

            return sb.ToString();
        }

        public char ReadChar()
        {
            if (_pos >= _source.Length) return (char)0;

            char ret = _source[_pos];
            _pos++;
            return ret;
        }

        public char PeekChar()
        {
            if (_pos >= _source.Length) return (char)0;
            else return _source[_pos];
        }

        public char GetPreviousChar(int offset)
        {
            if (_pos - offset < 0) return (char)0;
            else return _source[_pos - offset];
        }

        public char[] PeekChars(int n)
        {
            if (_pos + n >= _source.Length) return new char[0];

            char[] ret = new char[n];

            for (int i = 0; i < n; i++)
            {
                ret[i] = _source[_pos + i];
            }

            return ret;
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            TokenReader reader = new TokenReader("int i=1*2/0.5; /*число*/ string s1 = \"Hello, world\";/*string2*/ char c='\\'';");

            int i = 1;
            while (true)
            {
                string s = reader.ReadToken();
                if (string.IsNullOrEmpty(s)) break;
                Console.WriteLine(i.ToString().PadLeft(2)+": "+s);
                i++;
            }

            Console.WriteLine("End");
            Console.ReadKey();
        }
    }
}
