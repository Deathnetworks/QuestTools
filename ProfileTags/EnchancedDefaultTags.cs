using QuestTools.Helpers;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.Common;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;


namespace QuestTools.ProfileTags.Complex
{
    public class CompositeTag : ProfileBehavior, IEnhancedProfileBehavior
    {
        public CompositeTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;            
        }

        private bool _isDone;
        public override bool IsDone
        {
            get
            {
                var delegateIsDone = IsDoneDelegate != null && IsDoneDelegate.Invoke(null);
                return !IsActiveQuestStep || _isDone || delegateIsDone;
            }
        }

        public Behaviors.IsDoneCondition IsDoneDelegate;
        public Composite BehaviorDelegate { get; set; }
        protected override Composite CreateBehavior()
        {
            return BehaviorDelegate;
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedLeaveGameTag : LeaveGameTag, IEnhancedProfileBehavior
    {
        public EnhancedLeaveGameTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion     
    }

    public class EnhancedLoadProfileTag : LoadProfileTag, IEnhancedProfileBehavior
    {
        public EnhancedLoadProfileTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedLogMessageTag : LogMessageTag, IEnhancedProfileBehavior
    {
        public EnhancedLogMessageTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedUseWaypointTag : UseWaypointTag, IEnhancedProfileBehavior
    {
        public EnhancedUseWaypointTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedWaitTimerTag : WaitTimerTag, IEnhancedProfileBehavior
    {
        public EnhancedWaitTimerTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedUseObjectTag : UseObjectTag, IEnhancedProfileBehavior
    {
        public EnhancedUseObjectTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedUsePowerTag : UsePowerTag, IEnhancedProfileBehavior
    {
        public EnhancedUsePowerTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedToggleTargetingTag : ToggleTargetingTag, IEnhancedProfileBehavior
    {
        public EnhancedToggleTargetingTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
        }

        public override void OnStart() { }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return Behaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone,
                ret => new Action(r => base.OnStart())
            );
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class EnhancedIfTag : IfTag, IEnhancedProfileBehavior
    {
        private bool _isDone;
        private bool _firstRun = true;
        public bool ContinuouslyRecheck;

        public override bool IsDone
        {
            get
            {
                if (_isDone)
                    return true;

                // End if children are finished
                if (Body.All(p => p.IsDone))
                {
                    _isDone = true;
                    return true;
                }

                // Check Condition
                if (_firstRun || ContinuouslyRecheck)
                {
                    // End if condition is false
                    if (!GetConditionExec())
                    {
                        _isDone = true;
                        Body.ForEach(b => b.SetChildrenDone());
                        return true;
                    }
                    _firstRun = false;
                }

                return false;
            }
        }

        public new bool GetConditionExec()
        {
            return ScriptManager.GetCondition(Condition).Invoke();
        }

        public override void ResetCachedDone()
        {
            _firstRun = true;
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IEnhancedProfileBehavior : INodeContainer

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
            this.SetChildrenDone();
        }

        #endregion

        public List<ProfileBehavior> Children
        {
            get { return Body; }
            set { Body = value; }
        }

    }

    public class EnhancedWhileTag : WhileTag, IEnhancedProfileBehavior
    {
        private bool _isDone;
        private bool _firstRun = true;

        public bool ContinuouslyRecheck;

        public override bool IsDone
        {
            get
            {               
                if (_isDone)
                    return true;

                // End if children are finished && condition is false, otherwise re-run them all
                if (Body.All(p => p.IsDone))
                {
                    if (GetConditionExec())
                    {
                        _isDone = false;                        
                        Body.ForEach(b => b.ResetCachedDone());
                        return false;
                    }
                  
                    _isDone = true;
                    return true;                  
                }

 
                // Check Condition
                if (_firstRun || ContinuouslyRecheck)
                {
                    // End if condition is false
                    if (!GetConditionExec())
                    {
                        _isDone = true;
                        Body.ForEach(b => b.SetChildrenDone());
                        return true;
                    }
                    _firstRun = false;
                }

                return false;
            }
        }

        public new bool GetConditionExec()
        {
            return ScriptManager.GetCondition(Condition).Invoke();
        }

        public override void ResetCachedDone()
        {
            _firstRun = true;
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IEnhancedProfileBehavior : INodeContainer

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
            this.SetChildrenDone();
        }

        #endregion

        public List<ProfileBehavior> Children
        {
            get { return Body; }
            set { Body = value; }
        }

    }

}

