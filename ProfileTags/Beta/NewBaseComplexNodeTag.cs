using System;
using System.Collections.Generic;
using System.Linq;
using QuestTools.Helpers;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.TreeSharp;

namespace QuestTools.ProfileTags
{
    public abstract class NewBaseComplexNodeTag : ComplexNodeTag
    {
        private bool? _ComplexDoneCheck;
        private bool? _AlreadyCompleted;
        private static Func<ProfileBehavior, bool> _BehaviorProcess;

        protected bool? ComplexDoneCheck
        {
            get
            {
                return _ComplexDoneCheck;
            }
            set
            {
                _ComplexDoneCheck = value;
            }
        }

        public List<ProfileBehavior> ChildProfileBehaviors = new List<ProfileBehavior>();
        public HashSet<Composite> ChildBehaviors = new HashSet<Composite>();
        public HashSet<Guid> ChildBehaviorIds = new HashSet<Guid>();
        
        public override bool IsDone
        {
            get
            {
                if (ProfileManager.CurrentProfileBehavior != null && ProfileManager.CurrentProfileBehavior.Behavior != null)
                {
                    var myprofileBehaviorType = ProfileManager.CurrentProfileBehavior.GetType();

                    // Skip First Tag (which is actually the previous tag)
                    if (ComplexDoneCheck.HasValue && myprofileBehaviorType != typeof(NewBaseComplexNodeTag))
                    {                        
                        // Skip multiple ticks of a tag we've seen before
                        if (!ChildBehaviorIds.Contains(ProfileManager.CurrentProfileBehavior.Behavior.Guid))
                        {                            
                            ChildBehaviors.Add(ProfileManager.CurrentProfileBehavior.Behavior);
                            ChildProfileBehaviors.Add(ProfileManager.CurrentProfileBehavior);
                            ChildBehaviorIds.Add(ProfileManager.CurrentProfileBehavior.Behavior.Guid);
                            OnChildStart();                             
                        }
                    }
                }

                // Make sure we've not already completed this tag
                if (_AlreadyCompleted.GetValueOrDefault(false))
                {
                    return true;
                }
                if (!ComplexDoneCheck.HasValue)
                {
                    ComplexDoneCheck = new bool?(GetConditionExec());
                }
                if (ComplexDoneCheck == false)
                {
                    return true;
                }

                if (_BehaviorProcess == null)
                {
                    _BehaviorProcess = new Func<ProfileBehavior, bool>(p => p.IsDone);
                }
                bool allChildrenDone = Body.All<ProfileBehavior>(_BehaviorProcess);
                if (allChildrenDone)
                {
                    OnChildrenDone();
                    _AlreadyCompleted = true;
                }
                return allChildrenDone;
            }
        }

        public abstract bool GetConditionExec();

        public virtual void OnChildStart() { }

        public virtual void OnChildrenDone() { }

        public override void ResetCachedDone()
        {
            foreach (ProfileBehavior behavior in Body)
            {
                behavior.ResetCachedDone();
            }
            ComplexDoneCheck = null;
        }
    }
}
