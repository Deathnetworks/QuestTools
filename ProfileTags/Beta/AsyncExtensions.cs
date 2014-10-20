using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Contexts;
using System.Windows.Documents;
using System.Windows.Navigation;
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Beta;
using QuestTools.ProfileTags.Complex;
using QuestTools.ProfileTags.Movement;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.TreeSharp;

namespace QuestTools.Helpers
{
    public static class AsyncExtensions
    {

        public static List<ProfileBehavior> GetChildren(this ProfileBehavior behavior)
        {
            var result = new List<ProfileBehavior>();

            if (behavior is INodeContainer)
                result = (behavior as INodeContainer).GetNodes().ToList();

            return result;
        }

        public static void SetChildrenDone(this ProfileBehavior behavior)
        {
            behavior.GetChildren().ForEach(b =>
            {
                if (b is IAsyncProfileBehavior)
                    (b as IAsyncProfileBehavior).Done();                
            });
        }

        /// <summary>
        /// Prepare IAsyncProfileBehavior to be executed as TreeSharp Composite
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        private static Composite RunAsync(this IAsyncProfileBehavior behavior)
        {
            if (!(behavior is ProfileBehavior)) 
                return new Action(ret => RunStatus.Failure);

            behavior.AsyncUpdateBehavior();

            if ((behavior as ProfileBehavior).QuestId == 0)
                (behavior as ProfileBehavior).QuestId = 1;

            if ((behavior as ProfileBehavior).StepId == 0)
                (behavior as ProfileBehavior).StepId = 1;
            
            (behavior as ProfileBehavior).ResetCachedDone();

            behavior.AsyncOnStart();

            return (behavior as ProfileBehavior).Behavior;
        }

        /// <summary>
        /// Prepare ProfileBehavior to be executed as TreeSharp Composite
        /// </summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public static Composite Run(this ProfileBehavior behavior)
        {
            var type = behavior.GetType();            

            if (behavior is IAsyncProfileBehavior)
                return (behavior as IAsyncProfileBehavior).RunAsync();

            if (type == typeof(LoadProfileTag))
                return (behavior as LoadProfileTag).ToAsync().RunAsync();

            if (type == typeof(LeaveGameTag))
                return (behavior as LeaveGameTag).ToAsync().RunAsync();

            if (type == typeof(LogMessageTag))
                return (behavior as LogMessageTag).ToAsync().RunAsync();

            if (type == typeof(WaitTimerTag))
                return (behavior as WaitTimerTag).ToAsync().RunAsync();

            if (type == typeof(UseWaypointTag))
                return (behavior as UseWaypointTag).ToAsync().RunAsync();

            if (type == typeof(ToggleTargetingTag))
                return (behavior as ToggleTargetingTag).ToAsync().RunAsync();

            if (type == typeof(IfTag))
                return (behavior as IfTag).ToAsync().RunAsync();

            if (type == typeof(WhileTag))
                return (behavior as WhileTag).ToAsync().RunAsync();

            if (type == typeof(UseObjectTag))
                return (behavior as UseObjectTag).ToAsync().RunAsync();

            if (type == typeof(UsePowerTag))
                return (behavior as UsePowerTag).ToAsync().RunAsync();
            
            Logger.Warn("You attempted to run a tag ({0}) that can't be converted to IAsyncProfileBehavior ", behavior.GetType());

            return new Action(ret => RunStatus.Failure);           
        }

        public static void CopyTo(this ProfileBehavior a, ProfileBehavior b)
        {
            b.QuestId = a.QuestId;
            b.StepId = a.StepId;
            b.StatusText = a.StatusText;
            b.IgnoreReset = a.IgnoreReset;
            b.QuestName = a.QuestName;
        }

        public static EnhancedLoadProfileTag ToEnhanced(this LoadProfileTag tag)
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

        public static EnhancedLeaveGameTag ToEnhanced(this LeaveGameTag tag)
        {
            var asyncVersion = new AsyncLeaveGameTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Reason = tag.Reason;
            asyncVersion.StayInParty = tag.StayInParty;
            return asyncVersion;
        }

        public static EnhancedLogMessageTag ToEnhanced(this LogMessageTag tag)
        {
            var asyncVersion = new AsyncLogMessageTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Output = tag.Output;
            return asyncVersion;
        }

        public static EnhancedWaitTimerTag ToEnhanced(this WaitTimerTag tag)
        {
            var asyncVersion = new AsyncWaitTimerTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.WaitTime = tag.WaitTime;
            return asyncVersion;
        }

        public static EnhancedUseWaypointTag ToEnhanced(this UseWaypointTag tag)
        {
            var asyncVersion = new AsyncUseWaypointTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            asyncVersion.WaypointNumber = tag.WaypointNumber;
            return asyncVersion;
        }

        public static EnhancedToggleTargetingTag ToEnhanced(this ToggleTargetingTag tag)
        {
            var asyncVersion = new AsyncToggleTargetingTag();
            tag.CopyTo(asyncVersion);
            asyncVersion.Combat = tag.Combat;
            asyncVersion.KillRadius = tag.KillRadius;
            asyncVersion.Looting = tag.Looting;
            asyncVersion.LootRadius = tag.LootRadius;
            return asyncVersion;
        }

        public static EnhancedIfTag ToEnhanced(this IfTag tag)
        {
            var asyncVersion = new AsyncIfTag();
            asyncVersion.Body = tag.Body;
            asyncVersion.Condition = tag.Condition;
            asyncVersion.Conditional = tag.Conditional;
            tag.CopyTo(asyncVersion);
            return asyncVersion;
        }

        internal static AsyncWhileTag ToAsync(this WhileTag tag)
        {
            var asyncVersion = new AsyncWhileTag();
            asyncVersion.Body = tag.Body;
            asyncVersion.Condition = tag.Condition;
            asyncVersion.Conditional = tag.Conditional;
            tag.CopyTo(asyncVersion);
            return asyncVersion;
        }

        public static EnhancedUseObjectTag ToEnhanced(this UseObjectTag tag)
        {
            var asyncVersion = new AsyncUseObjectTag();
            asyncVersion.ActorId = tag.ActorId;
            asyncVersion.Hotspots = tag.Hotspots;
            asyncVersion.IsPortal = tag.IsPortal;
            asyncVersion.InteractRange = tag.InteractRange;
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            tag.CopyTo(asyncVersion);
            return asyncVersion;
        }

        public static EnhancedUsePowerTag ToEnhanced(this UsePowerTag tag)
        {
            var asyncVersion = new AsyncUsePowerTag();
            asyncVersion.SNOPower = tag.SNOPower;
            asyncVersion.X = tag.X;
            asyncVersion.Y = tag.Y;
            asyncVersion.Z = tag.Z;
            tag.CopyTo(asyncVersion);
            return asyncVersion;
        }

    }
}

