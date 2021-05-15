using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace PascalLexer
{
    class Lexer
    {
        internal enum TokenType
        {
            Identifier,
            Number,
            Comment,
            String
        }

        internal class Token
        {
            public TokenType Type;
            public string Value;

            public Token(string value, TokenType type)
            {
                Value = value;
                Type = type;
            }

            public override string ToString()
            {
                return (Type.ToString(), '"' + Value + '"').ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() != this.GetType()) return false;

                var other = (Token) obj;
                return Value == other.Value && Type == other.Type;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int) Type, Value);
            }
        }

        class TokenDefinition
        {
            public Regex Regex;
            public TokenType Type;

            public TokenDefinition(Regex regex, TokenType type)
            {
                Regex = regex;
                Type = type;
            }
        }

        private readonly Regex _delimiter = new Regex(@"^[\n\r\t\s]+");
        
        private readonly TokenDefinition _identifier =
            new TokenDefinition(new Regex(@"^[a-zA-Z][a-zA-Z\d]+"), TokenType.Identifier);
        private readonly TokenDefinition _number =
            new TokenDefinition(new Regex(@"^[+-]?(([0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?)|([0-9]+|\$[0-9a-fA-F]+|\&[0-7]+|\%[01]+))"), TokenType.Number);
        private readonly TokenDefinition _comment =
            new TokenDefinition(new Regex(@"^\{[^\}]*\}|\(\*((.|\n)*?)\*\)|//[^\n]*"), TokenType.Comment);
        private readonly TokenDefinition _string =
            new TokenDefinition(new Regex(@"('([^'\r])*'|#[0-9]+)+"), TokenType.String);

        private List<TokenDefinition> _defList = new List<TokenDefinition>();

        public Lexer()
        {
            _defList.Add(_identifier);
            _defList.Add(_number);
            _defList.Add(_comment);
            _defList.Add(_string);
        }
        
        private (string, string) LexString(string s, Regex r)
        {
            var match = r.Match(s);
            if (!match.Success) return (null, s);

            var token = s[match.Index..(match.Index + match.Length)];
            var rest = s[(match.Index + match.Length)..];
            return (token, rest);
        }

        private (Token, string) LexToken(string s)
        {
            foreach (var tokenDef in _defList)
            {
                var (tokenVal, rest) = LexString(s, tokenDef.Regex);
                if (tokenVal == null) continue;
                
                var token = new Token(tokenVal, tokenDef.Type);
                return (token, rest);
            }

            return (null, s);
        }

        public (List<Token>, string) lex(string s)
        {
            s = s.Trim(new char[] {'\n', '\r', '\t', ' '});
            var tokens = new List<Token>();

            var (token, rest) = LexToken(s);
            if (token == null) return (tokens, s);
            tokens.Add(token);
            
            while (rest.Length != 0)
            {
                var (token1, rest1) = LexString(rest, _delimiter);
                if (token1 == null) return (tokens, rest1);

                var (token2, rest2) = LexToken(rest1);
                if (token2 == null) return (tokens, rest2);
                
                tokens.Add(token2);
                rest = rest2;
            }
            return (tokens, rest);
        }
    }

    public class Tests
    {
        private Lexer lexer = new Lexer();

        [Test]
        public void Test1()
        {
            var expected = new List<Lexer.Token>
            {
                new Lexer.Token("+10.10E10", Lexer.TokenType.Number),
                new Lexer.Token("identifier1", Lexer.TokenType.Identifier),
                new Lexer.Token("-$a10fAFDE17", Lexer.TokenType.Number),
                new Lexer.Token("%1010", Lexer.TokenType.Number),
                new Lexer.Token("&01234567", Lexer.TokenType.Number),
                new Lexer.Token("identifier2", Lexer.TokenType.Identifier)
            };
            
            var (tokens, rest) = lexer.lex("+10.10E10 identifier1 -$a10fAFDE17 %1010 &01234567 identifier2");
            Assert.AreEqual(expected, tokens);
            Assert.IsEmpty(rest);
        }

        [Test]
        public void Test2()
        {
            var (tokens, rest) = lexer.lex("  (* comment 1, any words *)   (* comment 2 *)  { comment3 } ");
            var expected = new List<Lexer.Token>
            {
                new Lexer.Token("(* comment 1, any words *)", Lexer.TokenType.Comment),
                new Lexer.Token("(* comment 2 *)", Lexer.TokenType.Comment),
                new Lexer.Token("{ comment3 }", Lexer.TokenType.Comment)
            };
            Assert.AreEqual(expected, tokens);
            Assert.IsEmpty(rest);
        }
        
        [Test]
        public void TestMultilineComments()
        {
            var s = @"
// This is a Delphi comment. All is ignored till the end of the line.
// Another one comment

{ Comment 1 (* comment 2 *) }  
(* Comment 1 { comment 2 } *)  
{ comment 1 // Comment 2 }  
(* comment 1 // Comment 2 *)  
// comment 1 (* comment 2 *)  
// comment 1 { comment 2 }

{  
   My beautiful function returns an interesting result,  
   but only if the argument A is less than B.  
} 

(*  
My beautiful function returns an interesting result, 
but only if the argument A is less than B. 
*)
";
            var (tokens, rest) = lexer.lex(s);
            Console.Out.WriteLine(rest);
            var expected = new List<Lexer.Token>
            {
                new Lexer.Token("// This is a Delphi comment. All is ignored till the end of the line.", Lexer.TokenType.Comment),
                new Lexer.Token("// Another one comment", Lexer.TokenType.Comment),
                new Lexer.Token("{ Comment 1 (* comment 2 *) }", Lexer.TokenType.Comment),
                new Lexer.Token("(* Comment 1 { comment 2 } *)", Lexer.TokenType.Comment),
                new Lexer.Token("{ comment 1 // Comment 2 }", Lexer.TokenType.Comment),
                new Lexer.Token("(* comment 1 // Comment 2 *)", Lexer.TokenType.Comment),
                new Lexer.Token("// comment 1 (* comment 2 *)  ", Lexer.TokenType.Comment),
                new Lexer.Token("// comment 1 { comment 2 }", Lexer.TokenType.Comment),
                new Lexer.Token("{  \n" +
                    "   My beautiful function returns an interesting result,  \n" +
                    "   but only if the argument A is less than B.  \n" +
                    "}", Lexer.TokenType.Comment),
                new Lexer.Token("(*  \n" +
                    "My beautiful function returns an interesting result, \n" +
                    "but only if the argument A is less than B. \n" +
                    "*)", Lexer.TokenType.Comment)
            };
            
            Assert.AreEqual(expected, tokens);
        }
        
        [Test]
        public void Test3()
        {
            var (tokens, rest) = lexer.lex(" 'This is a pascal string' 'a' 'some'#13#10'string'");

            var expected = new List<Lexer.Token>
            {
                new Lexer.Token("'This is a pascal string'", Lexer.TokenType.String),
                new Lexer.Token("'a'", Lexer.TokenType.String),
                new Lexer.Token("'some'#13#10'string'", Lexer.TokenType.String)
            };
            Assert.AreEqual(expected, tokens);
            Assert.IsEmpty(rest);
        }

    }
}
