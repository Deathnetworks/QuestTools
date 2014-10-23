using System.Collections;
using System.Xml.Linq;
using System.Xml.XPath;
using log4net.Core;
using QuestTools.ProfileTags.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.Bot.Settings;
using Zeta.Game;

namespace QuestTools.Helpers
{
    internal class ProfileUtils
    {
        public static void LoadAdditionalGameParams()
        {           
            // Only worry about GameParams if we're about to start a new game
            if (ZetaDia.IsInGame || ProfileManager.CurrentProfile == null)
                return;
           
            var document = ProfileManager.CurrentProfile.Element;

            // Set Difficulty
            var difficultyAttribute = ((IEnumerable)document.XPathEvaluate("/GameParams[1]/@difficulty")).Cast<XAttribute>().ToList().FirstOrDefault();
            if (difficultyAttribute != null)
            {
                var difficulty = difficultyAttribute.Value.ChangeType<GameDifficulty>();
                if (difficulty != CharacterSettings.Instance.GameDifficulty)
                {
                    Logger.Log("Difficulty changed to " + difficulty + " by profile: " + ProfileManager.CurrentProfile.Name);                      
                    CharacterSettings.Instance.GameDifficulty = difficulty;
                }              
            }

        }

        /// <summary>
        /// Replace some default DemonBuddy tags with enhanced Questtools versions
        /// </summary>
        public static void ReplaceDefaultTags()
        {
            RecurseBehaviors(Zeta.Bot.ProfileManager.CurrentProfile.Order, (node, i, type) =>
            {
                //if (node is IfTag && type == typeof(IfTag))
                //{
                //    return new EnhancedIfTag
                //    {
                //        Body = (node as IfTag).Body,
                //        Condition = (node as IfTag).Condition,
                //        Conditional = (node as IfTag).Conditional,
                //    };
                //}

                //if (node is WhileTag && type == typeof(WhileTag))
                //{
                //    return new EnhancedWhileTag
                //    {
                //        Body = (node as IfTag).Body,
                //        Condition = (node as IfTag).Condition,
                //        Conditional = (node as IfTag).Conditional,
                //    };
                //}

                return node;
            });
        }

        public static void AsyncReplaceTags(IList<ProfileBehavior> tags)
        {
            RecurseBehaviors(tags, (behavior, i, type) =>
            {
                if (behavior is IEnhancedProfileBehavior)
                    return behavior;

                if (type == typeof(LoadProfileTag))
                    return (behavior as LoadProfileTag).ToEnhanced();

                if (type == typeof(LeaveGameTag))
                    return (behavior as LeaveGameTag).ToEnhanced();

                if (type == typeof(LogMessageTag))
                    return (behavior as LogMessageTag).ToEnhanced();

                if (type == typeof(WaitTimerTag))
                    return (behavior as WaitTimerTag).ToEnhanced();

                if (type == typeof(UseWaypointTag))
                    return (behavior as UseWaypointTag).ToEnhanced();

                if (type == typeof(ToggleTargetingTag))
                    return (behavior as ToggleTargetingTag).ToEnhanced();

                if (type == typeof(IfTag))
                    return (behavior as IfTag).ToEnhanced();

                if (type == typeof(WhileTag))
                    return (behavior as WhileTag).ToEnhanced();

                if (type == typeof(UseObjectTag))
                    return (behavior as UseObjectTag).ToEnhanced();

                if (type == typeof(UsePowerTag))
                    return (behavior as UsePowerTag).ToEnhanced();

                if (type == typeof(WaitWhileTag))
                    return (behavior as WaitWhileTag).ToEnhanced();

                return behavior;
            });
        }

        public delegate ProfileBehavior TagProcessingDelegate(ProfileBehavior node, int index, Type type);

        /// <summary>
        /// Walks through profile nodes recursively, 
        /// TagProcessingDelegate is called for every Tag.
        /// The original tag is replaced by tag returned by TagProcessingDelegate
        /// </summary>
        public static void RecurseBehaviors(IList<ProfileBehavior> nodes, TagProcessingDelegate replacementDelegate, int depth = 0, int maxDepth = 20)
        {
            if (nodes == null || !nodes.Any())
                return;

            if (replacementDelegate == null)
                return;

            if (depth == maxDepth)
            {
                Logger.Debug("MaxDepth ({0}) reached on ProfileUtils.ReplaceBehaviors()", maxDepth);
                return;
            }

            for (var i = 0; i < nodes.Count(); i++)
            {
                if (nodes[i] == null)
                    continue;

                var node = nodes[i];
                var type = node.GetType();

                
                nodes[i] = replacementDelegate.Invoke(node, i, type);

                if(nodes[i] == null)
                    continue;

                var newType = nodes[i].GetType();

                if (QuestTools.EnableDebugLogging)
                    Logger.Verbose("".PadLeft(depth * 5) + "{0}> {1}", depth, newType != type ?
                    string.Format("replaced {0} with {1}", type, newType) :
                    string.Format("ignored {0}", newType)
                    );

                if (node is INodeContainer)
                {
                    RecurseBehaviors((node as INodeContainer).GetNodes() as List<ProfileBehavior>, replacementDelegate, depth + 1, maxDepth);
                }

            }
        }
    }
}
