using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestTools.Helpers
{
    /// <summary>
    /// Handles ONLY breaking a string into multiple pieces
    /// </summary>
    internal class Tokenizer
    {
        public static readonly string[] DefaultOperators = { "&&", "||", "==", ">=", ">", "<", "<=", "!=", "(", ")" };

        public static readonly string[] IgnoreChars = { "'", "," };

        private static string[] _operators;

        private static readonly Func<string[], char[]> LookupChars = operators => operators.SelectMany(x => x.ToCharArray()).Distinct().ToArray();

        public static IEnumerable<string> Tokenize(string input, string[] ops = null)
        {
            input = RemoveLineEndings(input);

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
            return RemoveLineEndings(s);
        }

        private static bool IsOperatorChar(char newChar)
        {
            return Array.IndexOf(LookupChars(_operators), newChar) >= 0;
        }

        private static bool IsIgnoreChar(char newChar)
        {
            return Array.IndexOf(LookupChars(IgnoreChars), newChar) >= 0;
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

        public static string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            var lineSeparator = ((char)0x2028).ToString(CultureInfo.InvariantCulture);
            var paragraphSeparator = ((char)0x2029).ToString(CultureInfo.InvariantCulture);

            return value.Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(lineSeparator, string.Empty)
                .Replace(paragraphSeparator, string.Empty);
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
