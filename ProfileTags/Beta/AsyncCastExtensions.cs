﻿using System.Runtime.Remoting;
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Complex;
using QuestTools.ProfileTags.Movement;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;

namespace QuestTools.Helpers
{
    public static class AsyncCastExtensions
    {
        public static void CopyTo(this ProfileBehavior a, ProfileBehavior b)
        {
            b.QuestId = a.QuestId;
            b.StepId = a.StepId;
            b.StatusText = a.StatusText;
            b.IgnoreReset = a.IgnoreReset;
            b.QuestName = a.QuestName;
        }

        internal static AsyncLoadProfileTag ToAsync(this LoadProfileTag tag)
        {
            var asyncVersion = new AsyncLoadProfileTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Profile = tag.Profile;
            asyncVersion.LeaveGame = tag.LeaveGame;
            asyncVersion.LoadRandom = tag.LoadRandom;
            asyncVersion.Profiles = tag.Profiles;
            asyncVersion.StayInParty = tag.StayInParty;
            return asyncVersion;
        }

        internal static AsyncLeaveGameTag ToAsync(this LeaveGameTag tag)
        {
            var asyncVersion = new AsyncLeaveGameTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Reason = tag.Reason;
            asyncVersion.StayInParty = tag.StayInParty;
            return asyncVersion;
        }

        internal static AsyncLogMessageTag ToAsync(this LogMessageTag tag)
        {
            var asyncVersion = new AsyncLogMessageTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Output = tag.Output;
            return asyncVersion;
        }

        internal static AsyncWaitTimerTag ToAsync(this WaitTimerTag tag)
        {
            var asyncVersion = new AsyncWaitTimerTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.WaitTime = tag.WaitTime;
            return asyncVersion;
        }

        internal static AsyncUseStopTag ToAsync(this UseStopTag tag)
        {
            var asyncVersion = new AsyncUseStopTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.ID = tag.ID;
            return asyncVersion;
        }

        internal static AsyncSafeMoveTo ToAsync(this SafeMoveToTag tag)
        {
            var asyncVersion = new AsyncSafeMoveTo();
            tag.CopyTo(asyncVersion);
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            asyncVersion.PathPointLimit = tag.PathPointLimit;
            asyncVersion.PathPrecision = tag.PathPrecision;
            return asyncVersion;
        }

        internal static AsyncUseWaypointTag ToAsync(this UseWaypointTag tag)
        {
            var asyncVersion = new AsyncUseWaypointTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            asyncVersion.WaypointNumber = tag.WaypointNumber;
            return asyncVersion;
        }

        internal static AsyncOffsetMoveTag ToAsync(this OffsetMoveTag tag)
        {
            var asyncVersion = new AsyncOffsetMoveTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.OffsetX = tag.OffsetX;
            asyncVersion.OffsetY = tag.OffsetY;
            asyncVersion.PathPrecision = tag.PathPrecision;
            return asyncVersion;
        }

        internal static AsyncMoveToActorTag ToAsync(this MoveToActor tag)
        {
            var asyncVersion = new AsyncMoveToActorTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.ActorId = tag.ActorId;
            asyncVersion.DestinationWorldId = tag.DestinationWorldId;
            asyncVersion.EndAnimation = tag.EndAnimation;
            asyncVersion.ExitWithConversation = tag.ExitWithConversation;
            asyncVersion.ExitWithVendorWindow = tag.ExitWithVendorWindow;
            asyncVersion.InteractAttempts = tag.InteractAttempts;
            asyncVersion.InteractRange = tag.InteractRange;
            asyncVersion.IsPortal = tag.IsPortal;
            asyncVersion.MaxSearchDistance = tag.MaxSearchDistance;
            asyncVersion.Position = tag.Position;
            asyncVersion.PathPointLimit = tag.PathPointLimit;
            asyncVersion.Timeout = tag.Timeout;
            asyncVersion.StraightLinePathing = tag.StraightLinePathing;
            asyncVersion.UseNavigator = tag.UseNavigator;
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            return asyncVersion;
        }

        internal static AsyncMoveToMapMarkerTag ToAsync(this MoveToMapMarker tag)
        {
            var asyncVersion = new AsyncMoveToMapMarkerTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.ActorId = tag.ActorId;
            asyncVersion.DestinationWorldId = tag.DestinationWorldId;
            asyncVersion.InteractAttempts = tag.InteractAttempts;
            asyncVersion.InteractRange = tag.InteractRange;
            asyncVersion.IsPortal = tag.IsPortal;
            asyncVersion.PathPointLimit = tag.PathPointLimit;
            asyncVersion.StraightLinePathing = tag.StraightLinePathing;
            asyncVersion.MapMarkerNameHash = tag.MapMarkerNameHash;
            asyncVersion.MaxSearchDistance = tag.MaxSearchDistance;
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            return asyncVersion;
        }

        internal static AsyncExploreDungeonTag ToAsync(this ExploreDungeonTag tag)
        {
            var asyncVersion = new AsyncExploreDungeonTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.ActorId = tag.ActorId;
            asyncVersion.AlternateActors = tag.AlternateActors;
            asyncVersion.AlternateMarkers = tag.AlternateMarkers;
            asyncVersion.AlternateScenes = tag.AlternateScenes;
            asyncVersion.BoxSize = tag.BoxSize;
            asyncVersion.BoxTolerance = tag.BoxTolerance;
            asyncVersion.Direction = tag.Direction;
            asyncVersion.EndType = tag.EndType;
            asyncVersion.ExitNameHash = tag.ExitNameHash;
            asyncVersion.ExploreTimeoutType = tag.ExploreTimeoutType;
            asyncVersion.FindExits = tag.FindExits;
            asyncVersion.IgnoreGridReset = tag.IgnoreGridReset;
            asyncVersion.IgnoreLastNodes = tag.IgnoreLastNodes;
            asyncVersion.IgnoreMarkers = tag.IgnoreMarkers;
            asyncVersion.IgnoreScenes = tag.IgnoreScenes;
            asyncVersion.InteractWithObject = tag.InteractWithObject;
            asyncVersion.ObjectInteractRange = tag.ObjectInteractRange;
            asyncVersion.Objectives = tag.Objectives;
            asyncVersion.MinOccurances = tag.MinOccurances;
            asyncVersion.RouteMode = asyncVersion.RouteMode;
            asyncVersion.SceneId = tag.SceneId;
            asyncVersion.SceneName = tag.SceneName;
            asyncVersion.SetNodesExploredAutomatically = tag.SetNodesExploredAutomatically;
            asyncVersion.TimeoutValue = tag.TimeoutValue;
            asyncVersion.TownPortalOnTimeout = tag.TownPortalOnTimeout;
            return asyncVersion;
        }

        internal static AsyncReloadProfileTag ToAsync(this ReloadProfileTag tag)
        {
            var asyncVersion = new AsyncReloadProfileTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Force = tag.Force;
            return asyncVersion;
        }

    }
}
