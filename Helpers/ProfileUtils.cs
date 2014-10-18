﻿using QuestTools.ProfileTags.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;

namespace QuestTools.Helpers
{
    internal class ProfileUtils
    {
        /// <summary>
        /// Replace some default DemonBuddy tags with enhanced Questtools versions
        /// </summary>
        public static void ReplaceDefaultTags()
        {
            RecurseBehaviors(Zeta.Bot.ProfileManager.CurrentProfile.Order, (node, i, type) =>
            {
                if (node is IfTag && type == typeof(IfTag))
                {
                    return new EnhancedIfTag
                    {
                        Body = (node as IfTag).Body,
                        Condition = (node as IfTag).Condition,
                        Conditional = (node as IfTag).Conditional,
                    };
                }

                if (node is WhileTag && type == typeof(WhileTag))
                {
                    return new EnhancedWhileTag
                    {
                        Body = (node as IfTag).Body,
                        Condition = (node as IfTag).Condition,
                        Conditional = (node as IfTag).Conditional,
                    };
                }

                //if (node is LogMessageTag && type == typeof(LogMessageTag))
                //{
                //    return (node as LogMessageTag).ToEnhanced();
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
            if (depth == maxDepth)
            {
                Logger.Debug("MaxDepth ({0}) reached on ProfileUtils.ReplaceBehaviors()", maxDepth);
                return;
            }

            if (nodes == null || !nodes.Any())
                return;

            for (var i = 0; i < nodes.Count(); i++)
            {
                var node = nodes[i];
                var type = node.GetType();

                if (replacementDelegate != null)
                    nodes[i] = replacementDelegate.Invoke(node, i, type);

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