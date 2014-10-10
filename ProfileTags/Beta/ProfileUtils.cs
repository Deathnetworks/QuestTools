﻿using QuestTools.Helpers;
using QuestTools.ProfileTags.Complex;
using QuestTools.ProfileTags.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;

namespace QuestTools.ProfileTags.Beta
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
                    // TODO - figure out why this ToAsync() approach is throwing its toys (exception)
                    //var newnode = (node as IfTag).ToAsync();
                    //newnode.ReadyToRun = true;
                    //return newnode;

                    return new AsyncIfTag
                    {
                        Body = (node as IfTag).Body,
                        Condition = (node as IfTag).Condition,
                        Conditional = (node as IfTag).Conditional,
                        ReadyToRun = true
                    };
                }

                return node;
            });
        }

        public static void AsyncReplaceTags(IList<ProfileBehavior> tags)
        {
            RecurseBehaviors(tags, (behavior, i, type) =>
            {
                if (type == typeof(LoadProfileTag))
                    return (behavior as LoadProfileTag).ToAsync();

                if (type == typeof(LeaveGameTag))
                    return (behavior as LeaveGameTag).ToAsync();

                if (type == typeof(LogMessageTag))
                    return (behavior as LogMessageTag).ToAsync();

                if (type == typeof(WaitTimerTag))
                    return (behavior as WaitTimerTag).ToAsync();

                if (type == typeof(UseStopTag))
                    return (behavior as UseStopTag).ToAsync();

                if (type == typeof(SafeMoveToTag))
                    return (behavior as SafeMoveToTag).ToAsync();

                if (type == typeof(MoveToActor))
                    return (behavior as MoveToActor).ToAsync();

                if (type == typeof(MoveToMapMarker))
                    return (behavior as MoveToMapMarker).ToAsync();

                if (type == typeof(OffsetMoveTag))
                    return (behavior as OffsetMoveTag).ToAsync();

                if (type == typeof(UseWaypointTag))
                    return (behavior as UseWaypointTag).ToAsync();

                if (type == typeof(ExploreDungeonTag))
                    return (behavior as ExploreDungeonTag).ToAsync();

                if (type == typeof(ReloadProfileTag))
                    return (behavior as ReloadProfileTag).ToAsync();

                if (type == typeof(ToggleTargetingTag))
                    return (behavior as ToggleTargetingTag).ToAsync();

                if (type == typeof(TownPortalTag))
                    return (behavior as TownPortalTag).ToAsync();

                if (type == typeof(ProfileSettingTag))
                    return (behavior as ProfileSettingTag).ToAsync();

                if (type == typeof(StartTimerTag))
                    return (behavior as StartTimerTag).ToAsync();

                if (type == typeof(StopTimerTag))
                    return (behavior as StopTimerTag).ToAsync();

                if (type == typeof(StopAllTimersTag))
                    return (behavior as StopAllTimersTag).ToAsync();

                if (type == typeof(LoadLastProfileTag))
                    return (behavior as LoadLastProfileTag).ToAsync();

                if (behavior is IfTag && type == typeof(IfTag))
                    return new AsyncIfTag
                    {
                        Body = (behavior as IfTag).Body,
                        Condition = (behavior as IfTag).Condition,
                        Conditional = (behavior as IfTag).Conditional,
                        ReadyToRun = true
                    };

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
                    Logger.Debug("".PadLeft(depth * 5) + "{0}> {1}", depth, newType != type ?
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