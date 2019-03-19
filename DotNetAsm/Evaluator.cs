﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OperationDef = System.Tuple<System.Func<System.Collections.Generic.List<double>, double>, int>;

namespace DotNetAsm
{
    /// <summary>
    /// Exception class for evaluation expressions.
    /// </summary>
    public class ExpressionException : Exception
    {
        /// <summary>
        /// Gets or sets the expression string that raises the exception.
        /// </summary>
        public string ExpressionString { get; set; }

        /// <summary>
        /// Constructs an instance of the ExpressionException class.
        /// </summary>
        /// <param name="expression">The expression that raises the exception.</param>
        public ExpressionException(string expression) : base()
        {
            ExpressionString = expression;
        }

        /// <summary>
        /// Overrides the Exception message.
        /// </summary>
        public override string Message
            => "Unknown or invalid expression: " + ExpressionString;
    }

    /// <summary>
    /// Math expression evaluator class. Takes string input and parses and evaluates 
    /// to a <see cref="System.Int64"/> value.
    /// </summary>
    public sealed class Evaluator : IEvaluator
    {
        #region Members

        Func<string, IEnumerable<ExpressionElement>> _parsingFunc;

        #region Static Members
        
        static Random _rng = new Random();

        static Dictionary<string, OperationDef> _functions;

        static readonly HashSet<string> _compounds = new HashSet<string>
        {
            "||", "&&", "<<", ">>", "<=", "==", ">=", "!=", "**"
        };

        static readonly Dictionary<ExpressionElement, OperationDef> _operators = new Dictionary<ExpressionElement, OperationDef>
        {
            {
                new ExpressionElement{ integral = false, word = ",",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(null,                                              int.MinValue)
            },
            { 
                new ExpressionElement{ integral = true,  word = "||", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => ((int)parms[1]!=0?1:0) | ((int)parms[0]!=0? 1 : 0),  0) 
            },
            {
                new ExpressionElement{ integral = true,  word = "&&", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => ((int)parms[1]!=0?1:0) & ((int)parms[0]!=0? 1 : 0),  1)
            },
            {
                new ExpressionElement{ integral = true,  word = "|",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => (long)parms[1]                | (long)parms[0],      2)
            },
            {
                new ExpressionElement{ integral = true,  word = "^",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => (long)parms[1]                ^ (long)parms[0],      3)
            },
            {
                new ExpressionElement{ integral = true,  word = "&",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => (long)parms[1]                & (long)parms[0],      4)
            },
            {
                new ExpressionElement{ integral = false, word = "!=", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => !parms[1].AlmostEquals(parms[0])          ? 1 : 0,   5)
            }, 
            {
                new ExpressionElement{ integral = false, word = "==", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1].AlmostEquals(parms[0])           ? 1 : 0,   5)
            },
            {
                new ExpressionElement{ integral = false, word = "<",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      <  parms[0] ? 1 : 0,   6)
            },
            {
                new ExpressionElement{ integral = false, word = "<=", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      <= parms[0] ? 1 : 0,   6)
            },
            {
                new ExpressionElement{ integral = false, word = ">=", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      >= parms[0] ? 1 : 0,   6)
            },
            {
                new ExpressionElement{ integral = false, word = ">",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      >  parms[0] ? 1 : 0,   6)
            },
            {
                new ExpressionElement{ integral = true,  word = "<<", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => (int)parms[1]                 << (int)parms[0],      7)
            },
            {
                new ExpressionElement{ integral = true,  word = ">>", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => (int)parms[1]                 >> (int)parms[0],      7)
            },
            {
                new ExpressionElement{ integral = false, word = "-",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      -  parms[0],           8)
            },
            {
                new ExpressionElement{ integral = false, word = "+",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      +  parms[0],           8)
            },
            {
                new ExpressionElement{ integral = false, word = "/",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      /  parms[0],           9)
            },
            {
                new ExpressionElement{ integral = false, word = "*",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => parms[1]                      *  parms[0],           9)
            },
            {
                new ExpressionElement{ integral = true,  word = "%",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => (long)parms[1]                %  (long)parms[0],     9)
            },
            {
                new ExpressionElement{ integral = false, word = "-",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => -parms[0],                                          10)
            },
            {
                new ExpressionElement{ integral = false, word = "+",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => +parms[0],                                          10)
            },
            {
                new ExpressionElement{ integral = true,  word = "~",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => ~((long)parms[0]),                                  11)
            },
            {
                new ExpressionElement{ integral = true,  word = "!",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => (long)parms[0] == 0 ? 1 : 0,                        11)
            },
            {
                new ExpressionElement{ integral = false, word = "**", type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Binary },
                new OperationDef(parms => Math.Pow(parms[1], parms[0]),                       12)
            },
            {
                new ExpressionElement{ integral = true,  word = ">",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => (long)(parms[0] / 0x100) % 256,                     13)
            },
            {
                new ExpressionElement{ integral = true,  word = "<",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => (long)parms[0]  % 256,                              13)
            },
            {
                new ExpressionElement{ integral = true,  word = "&",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => (long)parms[0]  % 65536,                            13)
            },
            {
                new ExpressionElement{ integral = true,  word = "^",  type = ExpressionElement.Type.Operator, subtype = ExpressionElement.Subtype.Unary },
                new OperationDef(parms => (long)(parms[0] / 0x10000) % 256,                   13)
            }
        };

        #endregion

        #endregion

        #region Constructors

        public Evaluator() 
            : this(false)
        {
            
        }

        /// <summary>
        /// Constructs an instance of the <see cref="T:DotNetAsm.Evaluator"/> class, used to evaluate 
        /// strings as mathematical expressions.
        /// </summary>
        /// <param name="functionsCaseSensitive">Determines whether to treat case 
        /// sensitivity to function names in expressions.</param>
        public Evaluator(bool functionsCaseSensitive)
        {
            StringComparer comparer = functionsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

            _functions = new Dictionary<string, OperationDef>(comparer)
            {
                { "abs",    new OperationDef(parms => Math.Abs(parms[0]),             1) },
                { "acos",   new OperationDef(parms => Math.Acos(parms[0]),            1) },
                { "atan",   new OperationDef(parms => Math.Atan(parms[0]),            1) },
                { "cbrt",   new OperationDef(parms => Math.Pow(parms[0], 1.0 / 3.0),  1) },
                { "ceil",   new OperationDef(parms => Math.Ceiling(parms[0]),         1) },
                { "cos",    new OperationDef(parms => Math.Cos(parms[0]),             1) },
                { "cosh",   new OperationDef(parms => Math.Cosh(parms[0]),            1) },
                { "deg",    new OperationDef(parms => (parms[0] * 180 / Math.PI),     1) },
                { "exp",    new OperationDef(parms => Math.Exp(parms[0]),             1) },
                { "floor",  new OperationDef(parms => Math.Floor(parms[0]),           1) },
                { "frac",   new OperationDef(parms => Math.Abs(parms[0] - Math.Abs(Math.Round(parms[0], 0))), 1) },
                { "hypot",  new OperationDef(parms => Math.Sqrt(Math.Pow(parms[1], 2) + Math.Pow(parms[0], 2)), 2) },
                { "ln",     new OperationDef(parms => Math.Log(parms[0]),             1) },
                { "log10",  new OperationDef(parms => Math.Log10(parms[0]),           1) },
                { "pow",    new OperationDef(parms => Math.Pow(parms[1], parms[0]),   2) },
                { "rad",    new OperationDef(parms => (parms[0] * Math.PI / 180),     1) },
                { "random", new OperationDef(parms => _rng.Next((int)parms[1], (int)parms[0]), 2) },
                { "round",  new OperationDef(parms => Math.Round(parms[0]),           1) },
                { "sgn",    new OperationDef(parms => Math.Sign(parms[0]),            1) },
                { "sin",    new OperationDef(parms => Math.Sin(parms[0]),             1) },
                { "sinh",   new OperationDef(parms => Math.Sinh(parms[0]),            1) },
                { "sqrt",   new OperationDef(parms => Math.Sqrt(parms[0]),            1) },
                { "tan",    new OperationDef(parms => Math.Tan(parms[0]),             1) },
                { "tanh",   new OperationDef(parms => Math.Tanh(parms[0]),            1) }
            };

            _parsingFunc = ParseElements;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Extracts the individual expression elements, or tokens, from a string
        /// representation of a mathematical expression before they are sent to
        /// the calculation unit for final processing.
        /// </summary>
        /// <returns> A <see cref="System.Collections.Generic.IEnumerable{DotNetAsm.ExpressionElement}"/>
        /// </returns>
        /// <param name="expression">The mathematical expression.</param>
        public IEnumerable<ExpressionElement> ParseElements(string expression)
        {
            var elements = new List<ExpressionElement>();
            StringBuilder elementBuilder = new StringBuilder();
            ExpressionElement currentElement = new ExpressionElement();

            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];
                if (char.IsWhiteSpace(c))
                {
                    AddElement();
                }
                else if (c.IsOperator() || c == ',')
                {
                    char next = i < expression.Length - 1 ? expression[i + 1] : char.MinValue;
                    bool nextIsOperand = char.IsLetterOrDigit(next) || next == '_' || next == '.' || next == '#';
                    if (currentElement.type != ExpressionElement.Type.Operator)
                    {
                        AddElement();
                        currentElement.type = ExpressionElement.Type.Operator;
                        if (c == ',')
                        {
                            currentElement.subtype = ExpressionElement.Subtype.Binary;
                        }
                        else
                        {
                            if (currentElement.subtype == ExpressionElement.Subtype.Open)
                            {
                                if (c.IsRadixOperator() && nextIsOperand)
                                {
                                    currentElement.type = ExpressionElement.Type.Operand;
                                    currentElement.subtype = ExpressionElement.Subtype.None;
                                }
                                else
                                {
                                    currentElement.subtype = ExpressionElement.Subtype.Unary;
                                }
                            }
                            else
                            {
                                currentElement.subtype = ExpressionElement.Subtype.Binary;
                            }
                        }
                    }
                    else if (!_compounds.Contains(elementBuilder.ToString() + c))
                    {
                        currentElement.subtype = ExpressionElement.Subtype.Binary;
                        AddElement();
                        if (c.IsRadixOperator())
                        {
                            currentElement.type = ExpressionElement.Type.Operand;
                            currentElement.subtype = ExpressionElement.Subtype.None;
                        }
                        else
                        {
                            currentElement.subtype = ExpressionElement.Subtype.Unary;
                        }
                    }
                    elementBuilder.Append(c);
                }
                else if (c == '(' || c == ')')
                {
                    AddElement();
                    if (c == '(' && elements.Count > 0 && (currentElement.type == ExpressionElement.Type.Operand && char.IsLetter(currentElement.word[0])))
                    {
                        // Convert operand expressions to functions where appropriate
                        elementBuilder.Append(elements.Last().word);
                        currentElement.type = ExpressionElement.Type.Function;
                        elements.RemoveAt(elements.Count - 1);
                        AddElement();
                    }
                    currentElement.type = ExpressionElement.Type.Group;
                    if (c == '(')
                        currentElement.subtype = ExpressionElement.Subtype.Open;
                    else
                        currentElement.subtype = ExpressionElement.Subtype.Close;
                    elementBuilder.Append(c);
                }
                else
                {
                    if (currentElement.type != ExpressionElement.Type.Operand)
                    {
                        AddElement();
                        currentElement.type = ExpressionElement.Type.Operand;
                        currentElement.subtype = ExpressionElement.Subtype.None;
                    }
                    elementBuilder.Append(c);
                }
                if (i == expression.Length - 1)
                    AddElement();
            }
            return elements;

            void AddElement()
            {
                if (elementBuilder.Length > 0)
                {
                    currentElement.word = elementBuilder.ToString();
                    elementBuilder.Clear();
                    elements.Add(currentElement);
                }
            }
        }

        double Calculate(IEnumerable<ExpressionElement> parsedElements)
        {
            var output = new Stack<double>();
            var operators = new Stack<ExpressionElement>();
            try
            {
                foreach (var element in parsedElements)
                {
                    if (element.type == ExpressionElement.Type.Operand)
                    {
                        if (element.word[0].IsRadixOperator())
                        {
                            var hexbin = element.word.Substring(1);
                            int radix;
                            if (element.word[0] == '%')
                            {
                                radix = 2;
                                hexbin = Regex.Replace(hexbin, @"^([#.]+)$", m => m.Groups[1].Value.Replace("#", "1").Replace(".", "0"));
                            }
                            else
                            {
                                radix = 16;
                            }
                            output.Push(Convert.ToInt64(hexbin, radix));
                        }
                        else
                        {
                            output.Push(double.Parse(element.word));
                        }
                    }
                    else if (element.type == ExpressionElement.Type.Function || element.subtype == ExpressionElement.Subtype.Open)
                    {
                        operators.Push(element);
                    }
                    else if (element.type == ExpressionElement.Type.Operator)
                    {
                        if (operators.Count > 0)
                        {
                            ExpressionElement topElement = new ExpressionElement();
                            var elemOrder = _operators[element].Item2;
                            topElement = operators.Peek();
                            while (topElement.type == ExpressionElement.Type.Function || topElement.type == ExpressionElement.Type.Operator || topElement.subtype == ExpressionElement.Subtype.Open)
                            {
                                var topOrder = topElement.type == ExpressionElement.Type.Operator ? _operators[topElement].Item2 : int.MaxValue;
                                if (topElement.subtype != ExpressionElement.Subtype.Open && topOrder >= elemOrder)
                                {
                                    operators.Pop();
                                    DoOperation(topElement);
                                    if (operators.Count > 0)
                                        topElement = operators.Peek();
                                    else
                                        break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        operators.Push(element);
                    }
                    else if (element.subtype == ExpressionElement.Subtype.Close)
                    {
                        if (operators.Count > 0)
                        {
                            var topElement = operators.Peek();
                            while (topElement.subtype != ExpressionElement.Subtype.Open)
                            {
                                operators.Pop();
                                if (!topElement.word.Equals(","))
                                    DoOperation(topElement);
                                if (operators.Count == 0)
                                    throw new Exception();
                                topElement = operators.Peek();
                            }
                            if (topElement.subtype == ExpressionElement.Subtype.Open)
                                operators.Pop();
                        }
                    }
                }
                while (operators.Count > 0)
                    DoOperation(operators.Pop());

                void DoOperation(ExpressionElement op)
                {
                    OperationDef operation = null;
                    List<double> parms = new List<double> { output.Pop() };
                    if (op.type == ExpressionElement.Type.Function)
                    {
                        operation = _functions[op.word];
                        var parmcount = operation.Item2 - 1;
                        while (parmcount-- > 0)
                            parms.Add(output.Pop());
                    }
                    else
                    {
                        operation = _operators[op];
                        if (op.subtype == ExpressionElement.Subtype.Binary)
                            parms.Add(output.Pop());
                        if (op.integral && parms.Any(p => !p.AlmostEquals(Math.Round(p))))
                            throw new Exception();
                    }
                    output.Push(operation.Item1(parms));
                }
                if (output.Count > 1)
                    throw new Exception();
                return output.Pop();
            }
            catch
            {
                throw new Exception();
            }
        }

        // Evaluate internally the expression to a double.
        double EvalInternal(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ExpressionException(expression);

            IEnumerable<ExpressionElement> elements = _parsingFunc(expression);

            try
            {
                var result = Calculate(elements);

                if (double.IsInfinity(result))
                    throw new DivideByZeroException(expression);

                if (double.IsNaN(result))
                    throw new ExpressionException(expression);
                
                return result;
            }
            catch (Exception)
            {
                throw new ExpressionException(expression);
            }
        }

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <returns>The result of the expression evaluation as a <see cref="T:System.Int64"/> value.</returns>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        /// <exception cref="T:System.DivideByZeroException">System.DivideByZeroException</exception>
        public long Eval(string expression) => Eval(expression, Int32.MinValue, UInt32.MaxValue);

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <param name="minval">The minimum value of expression. If the evaluated value is
        /// lower, a System.OverflowException will occur.</param>
        /// <param name="maxval">The maximum value of the expression. If the evaluated value 
        /// is higher, a System.OverflowException will occur.</param>
        /// <returns>The result of the expression evaluation as a <see cref="T:System.Int64"/> value.</returns>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        /// <exception cref="T:System.DivideByZeroException">System.DivideByZeroException</exception>
        /// <exception cref="T:System.OverflowException">System.OverflowException</exception>
        public long Eval(string expression, long minval, long maxval)
        {
            var result = EvalInternal(expression);
            if (result < minval || result > maxval)
                throw new OverflowException(expression);
            return (long)result;
        }

        /// <summary>
        /// Evaluates a text string as a conditional (boolean) evaluation.
        /// </summary>
        /// <param name="condition">The string representation of the conditional expression.</param>
        /// <returns><c>True</c>, if the expression is true, otherwise <c>false</c>.</returns>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        public bool EvalCondition(string condition) => Eval(condition) == 1;

        /// <summary>
        /// Defines a parser for the evaluator. Typically used to translate symbols (such as 
        /// variables) in expressions.
        /// </summary>
        /// <param name="parsingFunc">The parsing function to return the expression elements..</param>
        public void DefineParser(Func<string, IEnumerable<ExpressionElement>> parsingFunc)
        {
            if (parsingFunc == null)
                throw new ArgumentNullException();
            _parsingFunc = parsingFunc;
        }

        #endregion
    }
}
