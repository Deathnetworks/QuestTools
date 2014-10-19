using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
                Logger.Error("ConditionParser Exception: {0}, {1}", ex.Message, ex.InnerException);
            }
            return false;
        };


        /// <summary>
        /// Evaluates all expressions 
        /// </summary>
        internal static bool Evaluate(List<Expression> expressions)
        {
            return RecursiveEvaluate(expressions);
        }

        private static bool RecursiveEvaluate(List<Expression> expressions, int depth = 0)
        {
            var runningResult = false;
            var first = true;
            var indent = ">".PadLeft(5 * depth);

            Logger.Verbose(indent + " Starting Group of {0} Expressions", expressions.Count);

            foreach (var exp in expressions)
            {
                bool thisResult;
                string thisResultLog;
                var notLog = exp.Negated ? "Not" : string.Empty;

                // abort if there's no point evaluating the rest of the expressions
                if (exp.Join == OperatorType.Or && (runningResult || first))
                {
                    Logger.Verbose("true OR this => we're done.");
                    runningResult = true;
                    break;
                }

                if (exp.Children.Any())
                {
                    thisResult = RecursiveEvaluate(exp.Children, depth + 1);
                    thisResultLog = string.Format("{0} Group: Result={1}", exp.Join + " " + notLog, thisResult);
                }
                else
                {
                    thisResult = EvaluateExpression(exp);
                    thisResultLog = string.Format("{0} Result={1}", exp.Join + " " + notLog + " " + exp.Original, thisResult);
                }

                if (exp.Negated)
                {
                    thisResult = !thisResult;
                }

                if (exp.Join == OperatorType.And && (runningResult || first))
                {
                    // prior expressions are true AND this => eval.
                    runningResult = thisResult;
                }
                else if (exp.Join == OperatorType.Or && (!runningResult))
                {
                    // prior expressions are false OR this => eval
                    runningResult = thisResult;
                }
                else
                {
                    runningResult = false;
                }

                first = false;

                Logger.Verbose(indent + " -- {0} RunningResult={1}", thisResultLog, runningResult);
            }

            Logger.Verbose(indent + " Evaluated Group as {0}", runningResult);

            return runningResult;
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

            int i = -1;

            var skippableTokenIndexes = new List<int>();
            var parents = new Dictionary<int, Expression>();
            var depth = -1;

            tokens.ForEach(token =>
            {
                i++;

                // If the current token exists in any of the enums of keywords we support,
                // create an expression by grabbing data from tokens ahead and behind.
                // mark indexes of tokens we use as skippable so they dont get processed twice.

                //if (!isValidMethodName(token))
                //    return;

                if (skippableTokenIndexes.Contains(i))
                {
                    Logger.Verbose("Skipping token {0}: {1}", i, token);
                    return;
                }

                var expression = new Expression();
                var behindOne = tokens.ElementAtOrDefault(i - 1);
                var behindTwo = tokens.ElementAtOrDefault(i - 2);
                var aheadOne = tokens.ElementAtOrDefault(i + 1);
                var aheadTwo = tokens.ElementAtOrDefault(i + 2);

                if (Tokenizer.IsEnumValue<Conditions.VariableConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.Variable;
                }
                else if (token.ToLower() == "true" || token.ToLower() == "false")
                {
                    expression.Type = Conditions.ConditionType.Boolean;
                }
                else if (Tokenizer.IsEnumValueFromPartial<Conditions.BoolVariableConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.BoolVariable;
                }
                else if (Tokenizer.IsEnumValue<Conditions.MethodConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.Method;
                }
                else if (Tokenizer.IsEnumValue<Conditions.BoolMethodConditionType>(token))
                {
                    expression.Type = Conditions.ConditionType.BoolMethod;
                }
                else if (Tokenizer.IsOperator(token, OperatorType.OpenParen))
                {
                    expression.Type = Conditions.ConditionType.Group;
                }
                else if (Tokenizer.IsOperator(token, OperatorType.CloseParen))
                {
                    expression.Type = Conditions.ConditionType.Group;
                }
                else
                {
                    if (!Tokenizer.IsOperator(token))
                        Logger.Verbose("Unrecognized token {0}", token);

                    return;
                }

                // Account for negation with NOT "Me.IsInTown or not (something)" versus "Me.IsInTown and not (something)"
                var behindOneAsOperator = Tokenizer.GetLogicalOperatorType(behindOne);
                var behindTwoAsOperator = Tokenizer.GetLogicalOperatorType(behindTwo);

                if (behindOneAsOperator == OperatorType.Not)
                {
                    expression.Join = behindTwoAsOperator;
                    expression.Negated = true;
                }
                else
                {
                    expression.Join = behindOneAsOperator;
                }

                // Order by Depth
                Func<KeyValuePair<int, Expression>> getDirectParent = () => parents.OrderBy(k => k.Key).FirstOrDefault();

                Logger.Verbose("Found {2} Token {0}: {1}", i, token, expression.Type);

                switch (expression.Type)
                {
                    case Conditions.ConditionType.Group:

                        if (Tokenizer.IsOperator(token, OperatorType.OpenParen))
                        {
                            depth++;
                            parents.Add(depth, expression);

                            Logger.Verbose("Created Group parentsCount={0} depth={1}", parents.Count, depth);
                        }

                        if (Tokenizer.IsOperator(token, OperatorType.CloseParen))
                        {
                            if (parents.Any())
                            {

                                var parent = getDirectParent();

                                Logger.Verbose("Ending Group parentsCount={0} parentChildCount={1}", parents.Count, parent.Value.Children.Count);

                                expressions.Add(parent.Value);

                                parents.Remove(parent.Key);
                            }
                            else
                            {
                                Logger.Debug("Orphan close paren found :(");
                            }


                        }

                        return;

                    case Conditions.ConditionType.Variable:

                        // Must Match syntax "[methodname] [operator] [value]"

                        // Expecting to find a valid operator as the next token
                        if (aheadOne == null || !Tokenizer.IsOperator(aheadOne))
                        {
                            return;
                        }

                        // Expecting to find a valid to compare against two tokens ahead
                        if (aheadTwo == null || Tokenizer.IsOperator(aheadTwo) || Tokenizer.IsEnumValue<Conditions.VariableConditionType>(aheadTwo))
                        {
                            return;
                        }

                        skippableTokenIndexes.AddRange(new[] { i + 1, i + 2 });

                        expression.Operator = Tokenizer.GetOperatorType(aheadOne);
                        expression.MethodName = Tokenizer.GetEnumValue<Conditions.VariableConditionType>(token).ToString();
                        expression.Value = aheadTwo;

                        break;

                    case Conditions.ConditionType.Boolean:
                        expression.MethodName = "GetBoolean";
                        expression.Value = token;
                        break;
                    case Conditions.ConditionType.BoolVariable:

                        expression.MethodName = Tokenizer.GetEnumValueFromPartial<Conditions.BoolVariableConditionType>(token).ToString();
                        break;

                    case Conditions.ConditionType.Method:
                    case Conditions.ConditionType.BoolMethod:

                        // Must match syntax "[methodname] [(] [param] [)] [operator] [value]"

                        // Expecting to find a valid operator as the next token
                        if (aheadOne == null || Tokenizer.GetOperatorType(aheadOne) != OperatorType.OpenParen)
                        {
                            return;
                        }

                        int openParenIndex = i + 1;
                        int closeParenIndex = 0;
                        var parameters = new List<string>();
                        var tokenIndexesInsideParens = new List<int>();

                        // Map out how many comma seperated values there are before finding a close paren
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

                            tokenIndexesInsideParens.Add(j + 1);

                            if (!Tokenizer.IsOperator(t))
                                parameters.Add(t);
                        }

                        // Expecting to find a close paren somewhere after these comma seperated variables
                        if (closeParenIndex == 0)
                        {
                            return;
                        }

                        // Find the tokens where a value and an operator should be
                        var operatorIndex = closeParenIndex + 1;
                        var valueIndex = closeParenIndex + 2;
                        var op = tokens.ElementAtOrDefault(operatorIndex);
                        var value = tokens.ElementAtOrDefault(valueIndex);

                        if (expression.Type == Conditions.ConditionType.Method)
                        {
                            // Expecting to have an operator and value to compare against
                            if (value == null || op == null || !Tokenizer.IsOperator(op) || Tokenizer.IsOperator(value))
                            {
                                return;
                            }

                            expression.MethodName = Tokenizer.GetEnumValue<Conditions.MethodConditionType>(token).ToString();

                            skippableTokenIndexes.AddRange(new[] { i, openParenIndex, closeParenIndex, operatorIndex, valueIndex });
                            skippableTokenIndexes.AddRange(tokenIndexesInsideParens);
                        }
                        else
                        {
                            // BoolMethod doesnt require a comparison to anything, so we dont care if 'op' or 'value' exist
                            expression.MethodName = Tokenizer.GetEnumValue<Conditions.BoolMethodConditionType>(token).ToString();

                            skippableTokenIndexes.AddRange(new[] { i, openParenIndex, closeParenIndex });
                            skippableTokenIndexes.AddRange(tokenIndexesInsideParens);
                        }

                        expression.Operator = Tokenizer.GetOperatorType(op);
                        expression.Value = value;
                        expression.Params = parameters;
                        break;
                }



                if (parents.Any())
                {
                    Logger.Verbose("Adding expression to Group");
                    getDirectParent().Value.Children.Add(expression);
                }
                else
                {
                    Logger.Verbose("Adding expression to root");
                    expressions.Add(expression);
                }

            });

            Logger.Verbose("Original = {0}", input);

            Logger.Verbose("Tokenized = {0}", string.Join(" • ", tokens));

            //LogExpressionTree(expressions, "Expression Tree:");

            return expressions;
        }

        public static void LogExpressionTree(List<Expression> expressions, string message = "")
        {
            Func<List<Expression>, int, string> recurseExpressions = null;

            recurseExpressions = (e, d) =>
            {
                var output = string.Empty;
                var indent = "".PadLeft(d * 5);
                e.ForEach(exp =>
                {
                    var not = exp.Negated ? "NOT" : string.Empty;

                    output += "\n" + indent + exp.Join.ToString().ToUpperInvariant() + " " + not + " " + exp.Original;

                    if (exp.Children.Any())
                    {
                        if (recurseExpressions != null)
                            output += "(" + indent + recurseExpressions(exp.Children, d + 1) + "\n" + indent + ")";
                    }

                });
                return output;
            };

            if (expressions.Any())
            {
                var output = (string.IsNullOrEmpty(message) ? ">" : message) + recurseExpressions(expressions, 1);

                //output += string.Format("\n> Result = {0}", Evaluate(expressions));

                Logger.Log(output.Trim());
            }
            else
            {
                Logger.Log("No Expressions Found");
            }
        }

    }

}
