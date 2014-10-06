using System.ComponentModel;
using QuestTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using QuestTools.ProfileTags.Movement;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
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
        private List<Expression> parsedConditions = new List<Expression>();

        /// <summary>
        /// This is run once, when a 'When' tag is hit in the profile
        /// </summary>
        public override bool GetConditionExec()
        {
            // Store expressions so condition is only processed once.
            parsedConditions = ConditionParser.Parse(Condition);

            // in immediate mode just act like an empty container and leave children alone.
            if (Immediate)            
                return parsedConditions.All(expression => ConditionParser.Evaluate(expression));

            if (QuestTools.EnableDebugLogging)
                Logger.Log("Async Initializing '{1}' with condition={0}", Condition, Name);

            // Prevent tags from using logger when initialized
            if(!QuestTools.EnableDebugLogging)
                LoggingController.Disable();

            ReplaceBehaviors();

            return true;
        }

        /// <summary>
        /// Go through each child of When tag and process them.
        /// </summary>
        private void ReplaceBehaviors()
        {
            var i = 0;
            foreach (var node in GetNodes().ToList())
            {
                ReplaceBehavior(i, node);
                i++;
            }            
        }        

        /// <summary>
        /// Replace tags we recognize with special 'Async' wrapped versions.
        /// Async versions implement the IAsyncProfileBehavior interface.
        /// They will not run until '.ReadyToRun' is set to True.
        /// They have additional utility methods and expose CreateBehavior()
        /// </summary>
        private void ReplaceBehavior(int index, ProfileBehavior behavior)
        {
            var type = behavior.GetType();

            if (type == typeof(LoadProfileTag))
                Body[index] = (behavior as LoadProfileTag).ToAsync();

            else if (type == typeof(LeaveGameTag))
                Body[index] = (behavior as LeaveGameTag).ToAsync();

            else if (type == typeof(LogMessageTag))
                Body[index] = (behavior as LogMessageTag).ToAsync();

            else if (type == typeof(WaitTimerTag))
                Body[index] = (behavior as WaitTimerTag).ToAsync();

            else if (type == typeof(UseStopTag))
                Body[index] = (behavior as UseStopTag).ToAsync();

            else if (type == typeof(SafeMoveToTag))
                Body[index] = (behavior as SafeMoveToTag).ToAsync();

            else if (type == typeof(MoveToActor))
                Body[index] = (behavior as MoveToActor).ToAsync();
            
            else if (type == typeof(MoveToMapMarker))
                Body[index] = (behavior as MoveToMapMarker).ToAsync();

            else if (type == typeof(OffsetMoveTag))
                Body[index] = (behavior as OffsetMoveTag).ToAsync();

            else if (type == typeof(UseWaypointTag))
                Body[index] = (behavior as UseWaypointTag).ToAsync();
                
            else if (type == typeof(ExploreDungeonTag))
                Body[index] = (behavior as ExploreDungeonTag).ToAsync();

            else if (type == typeof(ReloadProfileTag))
                Body[index] = (behavior as ReloadProfileTag).ToAsync();

            else if (type == typeof(ToggleTargetingTag))
                Body[index] = (behavior as ToggleTargetingTag).ToAsync();

            else if (type == typeof(TownPortalTag))
                Body[index] = (behavior as TownPortalTag).ToAsync();
            
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
                Logger.Log("Successfully Initialized '{0}' with {1} Tags", Name, ChildBehaviorIds.Count);

            if (Immediate)
                return;

            // Make sure all the behaviors are set back to a fresh state
            ChildProfileBehaviors.ForEach(b =>
            {
                b.IgnoreReset = false;
                b.ResetCachedDone();
                b.ResetCachedDone(true);
            });

            // Queue all the children along with a condition callback that determines when they should be run
            BotBehaviorQueue.Queue(ChildProfileBehaviors, ret =>
                parsedConditions.All(expression => ConditionParser.Evaluate(expression)), Name);
        }
    }
}

