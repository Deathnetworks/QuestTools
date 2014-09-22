using System.Collections.Generic;

namespace QuestTools.Helpers
{
    /// <summary>
    /// A statement to be evaluated, ie "LevelAreaId == 234234"
    /// </summary>
    public class Expression
    {
        public OperatorType Join;
        public string MethodName;
        public OperatorType Operator;
        public List<string> Params;
        public Conditions.ConditionType Type;
        public string Value;
    }
}
