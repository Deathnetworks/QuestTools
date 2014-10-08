using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuestTools.Helpers
{
    public class ConditionParser
    {
        /// <summary>
        /// Executes the handler method for an expression
        /// </summary>
        internal static Func<Expression, bool> EvaluateExpression = expression =>
        {
            try
            {
                if (string.IsNullOrEmpty(expression.MethodName))
                    return false;

                Type type = typeof(Conditions);
                MethodInfo methodInfo = type.GetMethod(expression.MethodName);
                return (bool)methodInfo.Invoke(null, new object[] { expression });
            }
            catch (Exception ex)
            {
                Logger.Debug("ConditionParser Exception: ", ex);
            }
            return false;
        };

        /// <summary>
        /// Evaluates all expressions 
        /// </summary>
        internal static bool Evaluate (List<Expression> expressions)
        {
            var result = false;
            var first = true;

            foreach (var exp in expressions)
            {
                if (exp.Join == OperatorType.And && (result || first))
                {
                    // prior expressions are true AND this => eval.
                    result = EvaluateExpression(exp);
                }
                else if (exp.Join == OperatorType.Or && (result || first))
                {
                    // prior expressions are true OR this => we're done.
                    return true;
                }
                else if (exp.Join == OperatorType.Or && (!result))
                {
                    // prior expressions are false OR this => eval
                    result = EvaluateExpression(exp);
                }
                else if (exp.Join == OperatorType.Not && (result || first))
                {
                    // prior expressions are true AND NOT this
                    result = !EvaluateExpression(exp);
                }
                else
                {
                    result = false;
                }

                first = false;
            }

            return result;           
        }

        /// <summary>
        /// Evaluate expression of two numbers
        /// </summary>
        public static bool EvalInt(OperatorType operation, int a, int b)
        {
            switch (operation)
            {
                case OperatorType.Equal:
                    return a == b;
                case OperatorType.GreaterThan:
                    return a > b;
                case OperatorType.GreaterThanEqual:
                    return a >= b;
                case OperatorType.LessThan:
                    return a < b;
                case OperatorType.NotEqual:
                    return a != b;
            }
            return false;
        }

        /// <summary>
        /// Evaluate expression of two strings
        /// </summary>
        public static bool EvalString(OperatorType operation, string a, string b)
        {
            switch (operation)
            {
                case OperatorType.Equal:
                    return Equals(a.ToLowerInvariant(), b.ToLowerInvariant());
                case OperatorType.NotEqual:
                    return !Equals(a.ToLowerInvariant(), b.ToLowerInvariant());
            }
            return false;
        }

        /// <summary>
        /// Breaks a string into Expressions
        /// </summary>
        internal static List<Expression> Parse(string input)
        {
            List<string> tokens = Tokenizer.Tokenize(input).ToList();
            var expressions = new List<Expression>();

            Func<string, bool> isValidMethodName = s =>
                Tokenizer.IsEnumValue<Conditions.MethodConditionType>(s) ||
                Tokenizer.IsEnumValue<Conditions.VariableConditionType>(s) ||
                Tokenizer.IsEnumValue<Conditions.BoolMethodConditionType>(s);

            int i = -1;

            tokens.ForEach(token =>
            {
                i++;

                if (!isValidMethodName(token))
                    return;

                var expression = new Expression();
                var behindOne = tokens.ElementAtOrDefault(i - 1);
                var aheadOne = tokens.ElementAtOrDefault(i + 1);
                var aheadTwo = tokens.ElementAtOrDefault(i + 2);

                if (Tokenizer.IsEnumValue<Conditions.VariableConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.Variable;
                }
                else if (Tokenizer.IsEnumValue<Conditions.MethodConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.Method;
                }
                else if (Tokenizer.IsEnumValue<Conditions.BoolMethodConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.BoolMethod;
                }
                else
                {
                    return;
                }

                switch (expression.Type)
                {
                    case Conditions.ConditionType.Variable:

                        // Must Match syntax "[methodname] [operator] [value]"

                        if (aheadOne == null || !Tokenizer.IsOperator(aheadOne))
                            return;

                        if (aheadTwo == null || Tokenizer.IsOperator(aheadTwo) || Tokenizer.IsEnumValue<Conditions.VariableConditionType>(aheadTwo))
                            return;

                        expression.Type = Conditions.ConditionType.Variable;
                        expression.Operator = Tokenizer.GetOperatorType(aheadOne);
                        expression.MethodName = Tokenizer.GetEnumValue<Conditions.VariableConditionType>(token).ToString();
                        expression.Value = aheadTwo;

                        break;
                    case Conditions.ConditionType.Method:
                    case Conditions.ConditionType.BoolMethod:

                        // Must match syntax "[methodname] [(] [param] [)] [operator] [value]"

                        if (aheadOne == null || Tokenizer.GetOperatorType(aheadOne) != OperatorType.OpenParen)
                            return;

                        int openParenIndex = i + 1;
                        int closeParenIndex = 0;
                        var parameters = new List<string>();

                        for (int j = openParenIndex; j < tokens.Count - 1; j++)
                        {
                            string t = tokens.ElementAtOrDefault(j + 1);

                            if (t == null)
                                break;

                            if (Tokenizer.GetOperatorType(t) == OperatorType.CloseParen)
                            {
                                closeParenIndex = j + 1;
                                break;
                            }

                            if (!Tokenizer.IsOperator(t))
                                parameters.Add(t);
                        }

                        if (closeParenIndex == 0)
                            return;

                        string value = tokens.ElementAtOrDefault(closeParenIndex + 2);
                        string op = tokens.ElementAtOrDefault(closeParenIndex + 1);

                        if (expression.Type == Conditions.ConditionType.Method)
                        {
                            if (value == null || op == null || !Tokenizer.IsOperator(op) || Tokenizer.IsOperator(value))
                                return;

                            expression.MethodName = Tokenizer.GetEnumValue<Conditions.MethodConditionType>(token).ToString();
                        }
                        else
                        {
                            expression.MethodName = Tokenizer.GetEnumValue<Conditions.BoolMethodConditionType>(token).ToString();
                        }

                        expression.Type = Conditions.ConditionType.Method;

                        expression.Operator = Tokenizer.GetOperatorType(op);
                        expression.Value = value;
                        expression.Params = parameters;

                        break;
                }

                expression.Join = Tokenizer.GetLogicalOperatorType(behindOne);
                expressions.Add(expression);
            });

            return expressions;
        }
    }

}
