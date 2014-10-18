﻿using QuestTools.Helpers;
using Zeta.Common;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Complex
{
    [XmlElement("When")]
    public class WhenTag : BaseComplexNodeTag
    {
        [XmlAttribute("condition")]
        public string Condition { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        public override bool GetConditionExec()
        {
            if (QuestTools.EnableDebugLogging)
                Logger.Log("Async Initializing '{0}' with condition={1}", Name, Condition);

            ProfileUtils.AsyncReplaceTags(Body);

            BotBehaviorQueue.Queue(Body, ret => ScriptManager.GetCondition(Condition).Invoke(), Name);

            return false;
        }
    }
}
