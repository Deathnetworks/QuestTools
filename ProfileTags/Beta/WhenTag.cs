using System.ComponentModel;
using System.Windows.Annotations;
using QuestTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using QuestTools.ProfileTags.Beta;
using QuestTools.ProfileTags.Movement;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.XmlEngine;
using ConditionParser = QuestTools.Helpers.ConditionParser;

namespace QuestTools.ProfileTags.Complex
{
    [XmlElement("When")]
    public class WhenTag : BaseComplexNodeTag, IAsyncProfileBehavior
    {
        [XmlAttribute("condition")]
        public string Condition { get; set; }

        [XmlAttribute("immediate")]
        public bool Immediate { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        private readonly List<int> _questIds = new List<int>();
        private List<Expression> _parsedConditions = new List<Expression>();

        public override bool GetConditionExec()
        {
            _parsedConditions = ConditionParser.Parse(Condition);

            if (Immediate)
                return ConditionParser.Evaluate(_parsedConditions);
                
            if (QuestTools.EnableDebugLogging)
                Logger.Log("Async Initializing '{0}' with condition={1}", Name, Condition);

            ProfileUtils.AsyncReplaceTags(Body);

            BotBehaviorQueue.Queue(Body, ret => ConditionParser.Evaluate(_parsedConditions), Name);

            return false;
        }

        #region IAsyncProfileBehavior : BaseComplexNodeTag

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public void Done()
        {
            ComplexDoneCheck = false;
            this.SetChildrenDone();
        }

        #endregion
    }
}

