using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using QuestTools.ProfileTags.Beta.ConditionParser;
using Zeta.Common;
using Extensions = Zeta.Common.Extensions;

namespace QuestTools.Helpers
{

    [AttributeUsage(AttributeTargets.Method) ]
    public class Condition : Attribute
    {
        public ExpressionType Type;

        public override string ToString()
        {
            return Type.ToString();
        }
    }

    public class ConditionParser
    {
        private static bool _initialized;
        public static void Initialize()
        {            
            NamespaceConditionMapping.AddRange(GetMethodFixedMethodMapping(typeof(Zeta.Bot.ConditionParser)));
            MethodNameAndConditionType.AddRange(GetMethodMapping(typeof(Conditions)));

            NamespaceConditionMapping.ForEach(pair => Logger.Debug(pair.Key + " " + pair.Value));
            MethodNameAndConditionType.ForEach(pair => Logger.Debug(pair.Key + " " + pair.Value));

            _initialized = true;
        }

        private static Dictionary<string, ExpressionType> GetMethodFixedMethodMapping(IReflect t)
        {
            return t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.GetParameters().Any())
                .Select(m => m.Name).Distinct().ToDictionary(k => k, v => ExpressionType.Namespace);
        }

        // The KEYS will be searched for within tokens, if found, DB Parser will be used on the expression
        public static Dictionary<string, ExpressionType> NamespaceConditionMapping = new Dictionary<string, ExpressionType>
        {
            {"ZetaDia.", ExpressionType.Namespace},
            {"Me.", ExpressionType.Namespace},
            {"Zeta.Bot", ExpressionType.Namespace},
            {"CurrentWorldId", ExpressionType.Namespace},
            {"CurrentLevelAreaId", ExpressionType.Namespace},
        };

        private static Dictionary<string, Condition> GetMethodMapping(Type t)
        {
            var dict = new Dictionary<string, Condition>();

            t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.GetParameters().Any()).ForEach(m =>
            {
                var condition = m.GetCustomAttributes(true).FirstOrDefault(a => a is Condition) as Condition;                
                dict.Add(m.Name,condition);
            });

            return dict;
        }

        public static Dictionary<string, Condition> MethodNameAndConditionType = new Dictionary<string, Condition>();

        /// <summary>
        /// Executes the handler method for an expression
        /// </summary>
        internal static Func<Expression, bool> EvaluateExpression = expression =>
        {                            
            try
            {   
                if (String.IsNullOrEmpty(expression.MethodName))
                    return false;

                var type = typeof(Conditions);
                var methodInfo = type.GetMethod(expression.MethodName);
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

        private static bool RecursiveEvaluate(List<Expression> expressions, int depth = 0, Expression parent = null)
        {
            var runningResult = false;
            var first = true;
            var indent = ">".PadLeft(5 * depth);

            if (depth != 0)
                Logger.Verbose(indent + " \\ {0}Group of {1} Expression{2}", parent != null ? 
                    parent.Join + " " : String.Empty, 
                    expressions.Count, 
                    expressions.Count > 1 ? "s" : String.Empty,
                    parent != null && parent.Negated ? "Not" : String.Empty
                    );

            foreach (var exp in expressions)
            {
                bool thisResult;
                string thisResultLog;
                var notLog = exp.Negated ? "Not" : String.Empty;

                // abort if there's no point evaluating the rest of the expressions
                if (exp.Join == OperatorType.Or && (runningResult || first))
                {
                    Logger.Verbose(indent + " -- True OR this => We're done.");
                    runningResult = true;
                    break;
                }

                if (exp.Children.Any())
                {
                    thisResult = RecursiveEvaluate(exp.Children, depth + 1, exp);
                    thisResultLog = String.Format("{0} Group: Result={1}", exp.Join + " " + notLog, thisResult);
                }
                else
                {
                    thisResult = EvaluateExpression(exp);
                    thisResultLog = String.Format("{0} Result={1}", exp.Join + " " + notLog + " " + exp, thisResult);
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

                if (exp.Type != ExpressionType.Group)
                    Logger.Verbose(indent + " -- {0} RunningResult={1} ({2})", thisResultLog, runningResult, exp.ParserId);
            }

            if (depth == 0)
                Logger.Verbose(indent + " Final Result = {0}", runningResult);
            else
                Logger.Verbose(indent + " / = {0}", runningResult);

            return runningResult;
        }

        /// <summary>
        /// Breaks a string into Expressions
        /// </summary>
        internal static List<Expression> Parse(string input)
        {
            if (!_initialized)
                Initialize();

            var i = -1;
            var depth = -1;
            var skippableTokenIndexes = new List<int>();
            var groups = new List<KeyValuePair<int, Expression>>();
            var tokens = Tokenizer.Tokenize(input).ToList();
            var expressions = new List<Expression>();

            tokens.ForEach(token =>
            {
                i++;

                // If the current token exists in any of the enums of keywords we support,
                // create an expression by grabbing data from tokens ahead and behind.
                // mark indexes of tokens we use as skippable so they dont get processed twice.

                if (skippableTokenIndexes.Contains(i))
                {
                    Logger.Verbose("  `Skipping token {0}: {1}", i, token);
                    return;
                }

                var expression = new Expression
                {
                    Index = i,
                    Keyword = token,
                    Tokens = tokens,
                    Type = GetExpressionType(token),
                };

                if (expression.Type == ExpressionType.Unknown)
                    return;

                Logger.Verbose("Found {2} Token {0}: {1}", i, token, expression.Type);

                expression.Join = expression.ParseJoin();
                expression.Negated = expression.ParseNegated();

                switch (expression.Type)
                {
                    case ExpressionType.Group:

                        // Save OpenParen to storage facility, while we process its children
                        if (ParserUtils.IsOperator(token, OperatorType.OpenParen))
                        {
                            // Entering parenthesis
                            depth++;

                            groups.Add(new KeyValuePair<int, Expression>(depth, expression));

                            Logger.Verbose("Created Group parents={0} depth={1}", groups.Count, depth);
                        }

                        // Group has all its child nodes, so we need to take from storage and insert to root or parent group:
                        if (ParserUtils.IsOperator(token, OperatorType.CloseParen))
                        {
                            if (groups.Any())
                            {
                                var parent = groups.LastOrDefault(p => p.Key == depth);
                                var parentsParent = groups.LastOrDefault(p => p.Key == depth - 1);

                                Logger.Verbose("Ending Group parents={0}, direct parent has {1} children", groups.Count, parent.Value.Children.Count);

                                if (depth == 0)
                                {
                                    // Add to the root
                                    expressions.Add(groups.First(p => p.Key == 0).Value);
                                    groups.Clear();
                                }
                                else
                                {
                                    // Add to parent;
                                    parentsParent.Value.Children.Add(parent.Value);
                                }

                                // Leaving a parenthesis
                                depth--;
                            }
                            else
                            {
                                Logger.Debug("Orphan close paren found :(");
                            }

                        }

                        return;

                    case ExpressionType.Boolean:
                        expression.MethodName = "GetBoolean";
                        expression.Value = token;
                        break;

                    case ExpressionType.Variable:
                    case ExpressionType.BoolVariable:
                    case ExpressionType.BoolMethod:
                    case ExpressionType.Method:
                        expression.ParseParams();
                        expression.ParseComparison();
                        expression.MethodName = ParserUtils.GetDictionaryPairFromPartialKey(MethodNameAndConditionType, token).Key;
                        skippableTokenIndexes.AddRange(expression.UsedIndexes);
                        break;

                    case ExpressionType.Namespace:
                        expression.ParseParams();
                        expression.ParseComparison();
                        var pair = ParserUtils.GetDictionaryPairFromPartialKey(NamespaceConditionMapping, token);
                        expression.Type = pair.Value;
                        expression.MethodName = "ZetaNamespace";
                        skippableTokenIndexes.AddRange(expression.UsedIndexes);
                        break;

                    default:
                        Logger.Verbose("Unrecognized token {0}", token);
                        break;

                }

                if (groups.Any())
                {
                    Logger.Verbose("Adding expression to Group");
                    var parent = groups.LastOrDefault(p => p.Key == depth).Value;
                    expression.Parent = parent;
                    parent.Children.Add(expression);
                }
                else
                {
                    expressions.Add(expression);
                }

            });

            Logger.Verbose("Original = {0}", input);
            Logger.Verbose("Tokenized = {0}", String.Join(" • ", tokens));

            Logger.Verbose(expressions.ToString("Expressions"));

            return expressions;
        }

        public static ExpressionType GetExpressionType(string token)
        {
            if (token.ToLower() == "true" || token.ToLower() == "false")
            {
                return ExpressionType.Boolean;
            }
            var matchinCondition = ParserUtils.GetDictionaryValueFromPartialKey(MethodNameAndConditionType, token);
            if (matchinCondition != null)
            {
                return matchinCondition.Type;
            }
            if (ParserUtils.IsDictionaryKeyPartial(NamespaceConditionMapping, token))
            {
                return ExpressionType.Namespace;
            }
            if (ParserUtils.IsOperator(token, OperatorType.OpenParen))
            {
                return ExpressionType.Group;
            }
            if (ParserUtils.IsOperator(token, OperatorType.CloseParen))
            {
                return ExpressionType.Group;
            }
            if (!ParserUtils.IsOperator(token))
                Logger.Verbose("Unrecognized token {0}", token);

            return ExpressionType.Unknown;
        }
    }



}
