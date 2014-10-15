using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using QuestTools.ProfileTags.Beta.ConditionParser;

namespace QuestTools.Helpers
{
    public enum ExpressionType
    {
        Unknown = 0,
        Variable,
        Namespace,
        Boolean,
        BoolVariable,
        Method,
        BoolMethod,
        Group,
    }

    /// <summary>
    /// A statement to be evaluated, ie "LevelAreaId == 234234"
    /// </summary>
    public class Expression
    {
        public override int GetHashCode()
        {
            //int mc = //magic constant, usually some prime
            //return mc * _id.GetHashCode() * prop2.GetHashCode ;
            return Id.GetHashCode();
        }

        private readonly Guid _id = new Guid();

        public Guid Id
        {
            get { return _id; }
        }

        public OperatorType Join;
        public bool Negated;
        public string MethodName;
        public OperatorType Operator;
        public List<string> Params = new List<string>();
        public ExpressionType Type;
        public string Value;
        public string Keyword;
        public List<string> Tokens = new List<string>();
        public List<Expression> Children = new List<Expression>();
        public List<int> UsedIndexes = new List<int>();
        public int Index;
        public string Original;
        public Expression Parent;

        /// <summary>
        /// For logging purposes
        /// </summary>
        public string ParserId = "QuestTools";

        public string GetToken(int offset, int index = -9999)
        {
            return index == -9999 ? Tokens.ElementAtOrDefault(Index + offset) : Tokens.ElementAtOrDefault(index + offset);
        }

        public string AheadOne { get { return GetToken(1); } }
        public string AheadTwo { get { return GetToken(2); } }
        public string AheadThree { get { return GetToken(3); } }
        public string AheadFour { get { return GetToken(4); } }
        public string AheadFive { get { return GetToken(5); } }
        public string BehindOne { get { return GetToken(-1); } }
        public string BehindTwo { get { return GetToken(-2); } }
        public string BehindThree { get { return GetToken(-3); } }
        public string BehindFour { get { return GetToken(-4); } }
        public string BehindLive { get { return GetToken(-5); } }

        /// <summary>
        /// Reconstructed expression as a string without join (AND/OR)
        /// </summary>
        public override string ToString()
        {       
            var s = new StringBuilder();

            s.Append(Keyword);

            if (Params != null && Params.Any())
            {
                s.Append("(");

                foreach (var param in Params)
                {
                    s.Append(param);

                    if (param != Params.Last())
                        s.Append(",");
                }

                s.Append(")");
            }

            if (!string.IsNullOrEmpty(Value) && Operator != OperatorType.Unknown)
            {
                s.Append(" " + ParserUtils.GetOperatorSymbol(Operator) + " " + Value);
            }       
            return Tokenizer.RemoveLineEndings(s.ToString());
        }
        
    }

}
