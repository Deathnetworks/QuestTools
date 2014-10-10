using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestTools.Helpers
{
    internal class Tokenizer
    {
        public static readonly string[] DefaultOperators = { "&&", "||", "==", ">=", ">", "<", "<=", "!=", "(", ")" };

        public static readonly string[] IgnoreChars = { "'", "," };

        private static string[] _operators;

        private static readonly Func<string[], char[]> LookupChars = operators => operators.SelectMany(x => x.ToCharArray()).Distinct().ToArray();

        public static IEnumerable<string> Tokenize(string input, string[] ops = null)
        {
            var buffer = new StringBuilder();

            _operators = ops ?? DefaultOperators;

            foreach (char c in input)
            {
                if (Char.IsWhiteSpace(c) || IsIgnoreChar(c))
                {
                    if (buffer.Length > 0)
                    {
                        yield return Flush(buffer);
                    }
                    continue; // just skip whitespace
                }

                if (IsOperatorChar(c))
                {
                    if (buffer.Length > 0)
                    {
                        // we have back-buffer; could be a>b, but could be >=
                        // need to check if there is a combined operator candidate
                        if (!CanCombine(buffer, c))
                        {
                            yield return Flush(buffer);
                        }
                    }
                    buffer.Append(c);
                    continue;
                }

                // so here, the new character is *not* an operator; if we have
                // a back-buffer that *is* operators, yield that
                if (buffer.Length > 0 && IsOperatorChar(buffer[0]))
                {
                    yield return Flush(buffer);
                }

                // append
                buffer.Append(c);
            }

            // out of chars... anything left?
            if (buffer.Length != 0)
            {
                yield return Flush(buffer);
            }
        }

        private static string Flush(StringBuilder buffer)
        {
            string s = buffer.ToString();
            buffer.Clear();
            return s;
        }

        private static bool IsOperatorChar(char newChar)
        {
            return Array.IndexOf(LookupChars(_operators), newChar) >= 0;
        }

        private static bool IsIgnoreChar(char newChar)
        {
            return Array.IndexOf(LookupChars(IgnoreChars), newChar) >= 0;
        }

        public static bool IsOperator(string value, string[] operators = null)
        {
            if (operators == null)
                operators = DefaultOperators;

            return operators.Contains(value);
        }

        public static bool IsOperator(string value, OperatorType type)
        {
            return GetOperatorType(value) == type;
        }

        public static OperatorType GetOperatorType(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                switch (value.ToLowerInvariant())
                {
                    case "<":
                        return OperatorType.LessThan;
                    case "<=":
                        return OperatorType.LessThanEqual;
                    case ">":
                        return OperatorType.GreaterThan;
                    case ">=":
                        return OperatorType.GreaterThanEqual;
                    case "!=":
                        return OperatorType.NotEqual;
                    case "==":
                        return OperatorType.Equal;
                    case "&&":
                        return OperatorType.And;
                    case "||":
                        return OperatorType.Or;
                    case "and":
                        return OperatorType.And;
                    case "or":
                        return OperatorType.Or;
                    case "not":
                        return OperatorType.Not;
                    case "is":
                        return OperatorType.Equal;
                    case "(":
                        return OperatorType.OpenParen;
                    case ")":
                        return OperatorType.CloseParen;
                }
            }
            return default(OperatorType);
        }

        public static string GetOperatorSymbol(OperatorType value)
        {
            switch (value)
            {
                case OperatorType.LessThan:
                    return "<";
                case OperatorType.LessThanEqual:
                    return "<=";
                case OperatorType.GreaterThan:
                    return ">";
                case OperatorType.GreaterThanEqual:
                    return ">=";
                case OperatorType.NotEqual:
                    return "!=";
                case OperatorType.Equal:
                    return "==";
                case OperatorType.And:
                    return "and";
                case OperatorType.Or:
                    return "or";
                case OperatorType.Not:
                    return "not";
                case OperatorType.OpenParen:
                    return "(";
                case OperatorType.CloseParen:
                    return ")";
            }        
            return string.Empty;
        }

        public static OperatorType GetLogicalOperatorType(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                switch (value.ToLowerInvariant())
                {
                    case "&&":
                        return OperatorType.And;
                    case "||":
                        return OperatorType.Or;
                    case "and":
                        return OperatorType.And;
                    case "or":
                        return OperatorType.Or;
                    case "not":
                        return OperatorType.Not;
                }
            }
            return OperatorType.And;
        }

        public static bool IsEnumValue<T>(string value)
        {
            return Enum.GetNames(typeof(T)).Any(token => token == value);
        }

        public static bool IsEnumValueFromPartial<T>(string value)
        {
            foreach (var name in Enum.GetNames(typeof(T)))
            {
                if (value.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    return true;
            }
            return false;
        }

        public static T GetEnumValueFromPartial<T>(string value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");
            
            var i = 0;
            foreach (var name in Enum.GetNames(typeof(T)))
            {
                if (value.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                    return (T)(object)i;

                i++;
            }
            return default(T);
        }

        public static T GetEnumValue<T>(string value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            if (String.IsNullOrEmpty(value))
                return default(T);

            T lResult;

            return Enum.TryParse(value, true, out lResult) ? lResult : default(T);
        }

        private static bool CanCombine(StringBuilder buffer, char c)
        {
            foreach (string op in _operators)
            {
                if (op.Length <= buffer.Length) continue;

                // check starts with same plus this one
                bool startsWith = true;

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (op[i] != buffer[i])
                    {
                        startsWith = false;
                        break;
                    }
                }
                if (startsWith && op[buffer.Length] == c) return true;
            }
            return false;
        }
    }

    public enum OperatorType
    {
        Unknown = 0,
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanEqual,
        LessThan,
        LessThanEqual,
        And,
        Or,
        Not,
        OpenParen,
        CloseParen,
    }
}
