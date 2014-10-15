using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestTools.Helpers;

namespace QuestTools.ProfileTags.Beta.ConditionParser
{
    public class ParserUtils
    {
        public static bool IsValidParams(List<string> strings, int count)
        {
            if (strings.Count != count)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (strings[i] == null)
                    return false;
            }
            return true;
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

        public static OperatorType GetLogicalOperatorType(string value)
        {
            if (!String.IsNullOrEmpty(value))
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

        public static bool IsOperator(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                return GetOperatorType(value) != OperatorType.Unknown;
            }
            return false;
        }

        public static bool IsOperator(string value, OperatorType type)
        {
            return GetOperatorType(value) == type;
        }

        public static OperatorType GetOperatorType(string value)
        {
            if (!String.IsNullOrEmpty(value))
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
            return String.Empty;
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

        public static bool IsDictionaryKeyPartial<T1, T2>(Dictionary<T1, T2> dictionary, string value)
        {
            if (dictionary.Any(pair => value.ToLowerInvariant().Contains(pair.Key.ToString().ToLowerInvariant())))
                return true;

            return false;
        }

        public static T2 GetDictionaryValueFromPartialKey<T1, T2>(Dictionary<T1, T2> dictionary, string value)
        {
            foreach (var pair in dictionary)
            {
                if (value.ToLowerInvariant().Contains(pair.Key.ToString().ToLowerInvariant()))
                {
                    return pair.Value;
                }
            }
            return default(T2);
        }

        public static KeyValuePair<T1,T2> GetDictionaryPairFromPartialKey<T1, T2>(Dictionary<T1, T2> dictionary, string value)
        {
            foreach (var pair in dictionary)
            {
                if (value.ToLowerInvariant().Contains(pair.Key.ToString().ToLowerInvariant()))
                {
                    return pair;
                }
            }
            return default(KeyValuePair<T1, T2>);
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


    }
}
