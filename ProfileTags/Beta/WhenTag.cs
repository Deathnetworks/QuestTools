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
    public class WhenTag : NewBaseComplexNodeTag
    {
        [XmlAttribute("condition")]
        public string Condition { get; set; }

        [XmlAttribute("immediate")]
        public bool Immediate { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        private readonly List<int> _questIds = new List<int>();
        private List<Expression> _parsedConditions = new List<Expression>();

        /// <summary>
        /// This is run once, when a 'When' tag is hit in the profile
        /// </summary>
        public override bool GetConditionExec()
        {
            // Store Expressions so condition is only tokenized/parsed once.
            _parsedConditions = ConditionParser.Parse(Condition);

            // in immediate mode just act like an empty container and leave children alone.
            if (Immediate)
                return ConditionParser.Evaluate(_parsedConditions);
                
            if (QuestTools.EnableDebugLogging)
                Logger.Log("Async Initializing '{1}' with condition={0}", Condition, Name);

            ProfileUtils.AsyncReplaceTags(Body);

            // Prevent tags from using logger when initialized
            if (!QuestTools.EnableDebugLogging)
                LoggingController.Disable();

            return true;
        }

        public override void OnChildStart()
        {
            if (QuestTools.EnableDebugLogging)
                Logger.Log("Child Behavior Initialized {0}", ProfileManager.CurrentProfileBehavior.GetType());
        }

        /// <summary>
        /// When all children have been executed.       
        /// </summary>
        public override void OnChildrenDone()
        {
            if (!QuestTools.EnableDebugLogging)
                LoggingController.Enable();

            if (QuestTools.EnableDebugLogging)
                Logger.Log("Successfully Initialized '{0}' with {1} Tags", Name, Body.Count);

            if (Immediate)
                return;

            // Make sure all the behaviors are set back to a fresh state            
            Body.ForEach(b =>
            {
                b.IgnoreReset = false;
                b.ResetCachedDone();
                b.ResetCachedDone(true);
            });

            // Queue all the children along with a condition callback that determines when they should be run
            BotBehaviorQueue.Queue(Body, ret => ConditionParser.Evaluate(_parsedConditions), Name);
        }
    }
}

