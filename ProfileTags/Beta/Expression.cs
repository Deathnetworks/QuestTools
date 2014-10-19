using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestTools.Helpers
{
    /// <summary>
    /// A statement to be evaluated, ie "LevelAreaId == 234234"
    /// </summary>
    public class Expression
    {
        public OperatorType Join;
        public bool Negated;
        public string MethodName;
        public OperatorType Operator;
        public List<string> Params;
        public Conditions.ConditionType Type;
        public string Value;

        public List<Expression> Children = new List<Expression>();

        /// <summary>
        /// Reconstructed expression as a string without join (AND/OR)
        /// </summary>
        public string Original
        {
            get 
            {
                if (!string.IsNullOrEmpty(_original))
                    return _original;

                var s = new StringBuilder();

                s.Append(MethodName);

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

                if (!string.IsNullOrEmpty(Value) && Operator != OperatorType.Unknown && 
                    (Type == Conditions.ConditionType.Method || Type == Conditions.ConditionType.Variable))
                {
                    s.Append(" " + Tokenizer.GetOperatorSymbol(Operator) + " " + Value);
                }                    

                _original = s.ToString();

                return _original;        
            }

        }
        private string _original;

    }
}
