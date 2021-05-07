using System.Collections.Generic;
using System.Text;
using NUnit.Framework;


namespace SimpleExpressionParser
{
    public interface IExpressionVisitor
    {
        void Visit(Literal expression);
        void Visit(Variable expression);
        void Visit(BinaryExpression expression);
        void Visit(ParenExpression expression);
    }

    public interface IExpression
    {
        void Accept(IExpressionVisitor visitor);
    }

    public class Literal : IExpression
    {
        public Literal(string value)
        {
            Value = value;
        }

        public readonly string Value;
        
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class Variable : IExpression
    {
        public Variable(string name)
        {
            Name = name;
        }

        public readonly string Name;
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class BinaryExpression : IExpression
    {
        public readonly IExpression FirstOperand;
        public readonly IExpression SecondOperand;
        public readonly string Operator;

        public BinaryExpression(IExpression firstOperand, IExpression secondOperand, string @operator)
        {
            FirstOperand = firstOperand;
            SecondOperand = secondOperand;
            Operator = @operator;
        }

        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class ParenExpression : IExpression
    {
        public ParenExpression(IExpression operand)
        {
            Operand = operand;
        }

        public readonly IExpression Operand;
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class DumpVisitor : IExpressionVisitor
    {
        private readonly StringBuilder myBuilder;

        public DumpVisitor()
        {
            myBuilder = new StringBuilder();
        }

        public void Visit(Literal expression)
        {
            myBuilder.Append("Literal(" + expression.Value + ")");
        }

        public void Visit(Variable expression)
        {
            myBuilder.Append("Variable(" + expression.Name + ")");
        }

        public void Visit(BinaryExpression expression)
        {
            myBuilder.Append("Binary(");
            expression.FirstOperand.Accept(this);
            myBuilder.Append(expression.Operator);
            expression.SecondOperand.Accept(this);
            myBuilder.Append(")");
        }

        public void Visit(ParenExpression expression)
        {
            myBuilder.Append("Paren(");
            expression.Operand.Accept(this);
            myBuilder.Append(")");
        }

        public override string ToString()
        {
            return myBuilder.ToString();
        }
    }

    public class Parser
    {
        private Dictionary<char, int> _operators = new Dictionary<char, int>
        {
            {'+', 0},
            {'-', 0},
            {'*', 1}
        };

        public IExpression Parse(string s)
        {
            string notation = ToReversePolishNotation(s);
            if (notation == null)
                return null;

            var stack = new Stack<IExpression>();
            foreach (var token in notation)
            {
                if (char.IsDigit(token))
                {
                    stack.Push(new Literal(token.ToString()));
                }
                if (char.IsLetter(token))
                {
                    stack.Push(new Variable(token.ToString()));
                }
                else if (_operators.ContainsKey(token))
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    stack.Push(new BinaryExpression(left, right, token.ToString()));
                }
            }

            return stack.Pop();
        }
        
        public string ToReversePolishNotation(string s)
        {
            var result = "";
            var opStack = new Stack<char>();
            
            foreach (var token in s)
            {
                if (char.IsDigit(token) || char.IsLetter(token))
                {
                    result += token;
                }
                else if (_operators.ContainsKey(token))
                {
                    while (opStack.Count != 0 &&
                           _operators.ContainsKey(opStack.Peek()) &&
                           _operators[opStack.Peek()] >= _operators[token])
                    {
                        result += opStack.Pop();
                    }
                    opStack.Push(token);
                }
                else if (token == '(')
                {
                    opStack.Push('(');
                }
                else if (token == ')')
                {
                    while (opStack.Count != 0 &&
                           opStack.Peek() != '(')
                    {
                        result += opStack.Pop();
                    }
                    if (opStack.Count == 0)
                    {
                        return null;
                    }
                    opStack.Pop();
                }
                else
                {
                    return null;
                }
            }
            while (opStack.Count != 0)
            {
                var op = opStack.Pop();
                if (_operators.ContainsKey(op))
                {
                    result += op;
                }
                else
                {
                    return null;
                }
            }
            return result;
        }
    }

    public class Tests
    {
        private Parser parser = new Parser();
        
        private static string ExprToString(IExpression expr)
        {
            var dumpVisitor = new DumpVisitor();
            expr.Accept(dumpVisitor);
            return dumpVisitor.ToString();
        }
        
        [SetUp]
        public void Setup() {}

        [Test]
        public void Test1()
        {
            var expr = parser.Parse("1*(2+3*4+1)*6");
            Assert.AreEqual(
                "Binary(Binary(Literal(1)*Binary(Binary(Literal(2)+Binary(Literal(3)*Literal(4)))+Literal(1)))*Literal(6))",
                ExprToString(expr));
        }

        [Test]
        public void Test2()
        {
            var expr = parser.Parse("1+2");
            Assert.AreEqual("Binary(Literal(1)+Literal(2))", ExprToString(expr));
        }
        
        [Test]
        public void Test3()
        {
            var expr = parser.Parse("(1+2)*(3+4)+5");
            Assert.AreEqual(
                "Binary(Binary(Binary(Literal(1)+Literal(2))*Binary(Literal(3)+Literal(4)))+Literal(5))",
                ExprToString(expr));
        }
        
        [Test]
        public void Test4()
        {
            var expr = parser.Parse("(1+2");
            Assert.IsNull(expr);
            
            expr = parser.Parse("1+2)");
            Assert.IsNull(expr);
        }
    }
}