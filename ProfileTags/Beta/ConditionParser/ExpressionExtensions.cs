using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestTools.Helpers;
using Zeta.Common;

namespace QuestTools.ProfileTags.Beta.ConditionParser
{
    public static class ExpressionExtensions
    {
        public static OperatorType ParseJoin(this Expression exp)
        {
            // Look for AND/OR at the start of expression.
            var behindOneAsOperator = ParserUtils.GetLogicalOperatorType(exp.BehindOne);
            var behindTwoAsOperator = ParserUtils.GetLogicalOperatorType(exp.BehindTwo);
            return ParserUtils.GetLogicalOperatorType(exp.GetToken(-1)) == OperatorType.Not ? behindTwoAsOperator : behindOneAsOperator;
        }

        public static bool ParseNegated(this Expression exp)
        {
            // Look for 'NOT' at the start of expression.
            return ParserUtils.GetLogicalOperatorType(exp.GetToken(-1)) == OperatorType.Not;
        }

        private static bool IsParams(this Expression exp)
        {
            if (exp.AheadOne == null || exp.AheadTwo == null)
                return false;

            // Expecting to find an open paren followed by at least one param
            if (!ParserUtils.IsOperator(exp.AheadOne, OperatorType.OpenParen) || ParserUtils.IsOperator(exp.AheadTwo))
                return false;

            return true;
        }

        public static void ParseParams(this Expression exp)
        {
            if (!exp.IsParams() || exp.Params.Any())
                return;

            var openParenIndex = exp.Index + 1;
            var closeParenIndex = 0;
            var parameters = new List<string>();
            var tokenIndexesInsideParens = new List<int>();
            var usedIndexes = new List<int>();

            // Find comma seperated values before finding a close paren
            for (var j = openParenIndex; j < exp.Tokens.Count - 1; j++)
            {
                string t = exp.Tokens.ElementAtOrDefault(j + 1);

                if (t == null)
                    break;

                if (ParserUtils.IsOperator(t, OperatorType.CloseParen))
                {
                    closeParenIndex = j + 1;
                    break;
                }

                tokenIndexesInsideParens.Add(j + 1);

                if (!ParserUtils.IsOperator(t))
                    parameters.Add(t);
            }

            // Expecting to find a close paren somewhere after these comma seperated variables
            if (closeParenIndex == 0)
                return;

            exp.UsedIndexes.Add(openParenIndex);
            exp.UsedIndexes.AddRange(tokenIndexesInsideParens);
            exp.UsedIndexes.Add(closeParenIndex);
            exp.Params = parameters;
        }

        public static void ParseComparison(this Expression exp)
        {
            int operatorPosition;

            if (!exp.Params.Any())
                exp.ParseParams();

            if (exp.Params.Any())
            {
                // Keyword ( [ param1 , param2 ] ) O V
                operatorPosition = exp.UsedIndexes.Max() + 1;
            }
            else
            {
                // Keyword O V
                operatorPosition = exp.Index + 1;
            }            

            var valuePosition = operatorPosition + 1;
            var op = exp.Tokens.ElementAtOrDefault(operatorPosition);
            var value = exp.Tokens.ElementAtOrDefault(valuePosition);

            if (value == null || op == null || !ParserUtils.IsOperator(op) || ParserUtils.IsOperator(value) )
                return;

            if (ParserUtils.IsDictionaryKeyPartial(Helpers.ConditionParser.NamespaceConditionMapping, value) || 
                Helpers.ConditionParser.MethodNameAndConditionType.ContainsKey(value))
                return;

            exp.UsedIndexes.Add(valuePosition);
            exp.UsedIndexes.Add(operatorPosition);
            exp.Operator = ParserUtils.GetOperatorType(op);
            exp.Value = value;
        }

        public static string ToString(this List<Expression> expressions, string message = "")
        {
            Func<List<Expression>, int, string> recurseExpressions = null;

            recurseExpressions = (e, d) =>
            {
                var output = String.Empty;
                var indent = "".PadLeft(d * 5);

                e.ForEach(exp =>
                {
                    var not = exp.Negated ? "NOT" : String.Empty;

                    var expression = !ParserUtils.IsOperator(exp.Keyword) ? exp.ToString() : string.Empty;

                    output += "\n" + indent + exp.Join.ToString().ToUpperInvariant() + " " + not + " " + expression;

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
                var output = (String.IsNullOrEmpty(message) ? ">" : message) + recurseExpressions(expressions, 1);
                return output.Trim();
            }

            return string.Empty;
        }
    }
}
