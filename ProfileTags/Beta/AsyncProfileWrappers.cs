using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using QuestTools.Helpers;
using QuestTools.ProfileTags.Beta;
using QuestTools.ProfileTags.Movement;
using System.Diagnostics;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.Common;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;
using ConditionParser = QuestTools.Helpers.ConditionParser;


namespace QuestTools.ProfileTags.Complex
{

    public class AsyncCompositeTag : ProfileBehavior, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get
            {
                var delegateIsDone = IsDoneDelegate != null && IsDoneDelegate.Invoke(null);
                return _isDone || ForceDone || delegateIsDone;
            }
        }

        public AsyncCommonBehaviors.IsDoneCondition IsDoneDelegate;

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }

        public Composite BehaviorDelegate { get; set; }

        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            return BehaviorDelegate;
        }
    }

    public class AsyncLeaveGameTag : LeaveGameTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite AsyncGetBehavior()
        {
            return CreateBehavior();
        }

        public Composite AsyncGetBaseBehavior()
        {
            return base.CreateBehavior();
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }        
        public void Tick() {}

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new DecoratorContinue(ret => !_isDone && ReadyToRun, base.CreateBehavior()),
                new Action(ret => _isDone = true)
            );
        }               
    }

    public class AsyncLoadProfileTag : LoadProfileTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        public override void ResetCachedDone(bool force = false)
        {
            _isDone = false;
            base.ResetCachedDone(force);
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncLogMessageTag : LogMessageTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnFailureOrBehaviorResult(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncUseWaypointTag : UseWaypointTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncOffsetMoveTag : OffsetMoveTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior() 
            );
        }
    }

    public class AsyncMoveToActorTag : MoveToActor, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncMoveToMapMarkerTag : MoveToMapMarker, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnFailureOrBehaviorResult(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncUseStopTag : UseStopTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnSuccessOrBehaviorResult(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncProfileSettingTag : ProfileSettingTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncStopTimerTag : StopTimerTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncStartTimerTag : StartTimerTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncLoadLastProfileTag : LoadLastProfileTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncStopAllTimersTag : StopAllTimersTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }


    public class AsyncSafeMoveTo : SafeMoveToTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnFailureOrBehaviorResult(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncExploreDungeonTag : ExploreDungeonTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnFailureOrBehaviorResult(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    public class AsyncTownPortalTag : TownPortalTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnFailureOrBehaviorResult(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    /// <summary>
    /// WaitTag doesn't reset properly, which is probably why it never has worked 
    /// after the first loop in WHILE tag. So we'll have to replace the functionality.
    /// </summary>
    public class AsyncWaitTimerTag : WaitTimerTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }

        private readonly Stopwatch _waitStopWatch = new Stopwatch();

        public void Tick()
        {
            if (!_waitStopWatch.IsRunning)
            {
                _waitStopWatch.Reset();
                _waitStopWatch.Start();
            }

            var timeRemainingMs = WaitTime - _waitStopWatch.ElapsedMilliseconds;
            StatusText = "WaitTimer Running. Time Left: " + timeRemainingMs + " milliseconds";

            if (_waitStopWatch.ElapsedMilliseconds <= WaitTime)
            {
                _isDone = false;
                return;
            }

            _waitStopWatch.Stop();
            _isDone = true;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            return new Action(ret =>
            {
                // Prevent this behavior from being run until ReadyToRun has been set to True                
                if (!ReadyToRun)
                    _isDone = true;

                // by always returning success this behavior will loop and 
                // prevent other behaviors from executing until IsDone = true;
                return RunStatus.Success;
            });
        }
    }

    public class AsyncReloadProfileTag : ReloadProfileTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => base.CreateBehavior()
            );
        }
    }

    // ToggleTargetting is inconsistent with other tags in that does its thing in OnStart() method, 
    // so we have to override OnStart() to stop that happening and then run it later by calling base.Onstart from CreateBehavior.
    public class AsyncToggleTargetingTag : ToggleTargetingTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ReadyToRun { get; set; }
        public bool ForceDone { get; set; }
        public void Tick() { }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        public override void OnStart()
        {
        }

        protected Composite BaseOnStartComposite()
        {
            return new Action(ret => base.OnStart());
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone && ReadyToRun,
                ret => BaseOnStartComposite()
            );
        }
    }

    [XmlElement("AsyncIf")]
    public class AsyncIfTag : IfTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get
            {
                return !_readyToRun || ForceDone || _isDone || !GetConditionExec();
            }
        }

        private bool _initialized;
        private void Initialize()
        {
            _parsedConditions = ConditionParser.Parse(Condition);
            _initialized = true;
        }

        private List<Expression> _parsedConditions = new List<Expression>();

        public new bool GetConditionExec()
        {
            if (!_initialized)
                Initialize();

            var result = ConditionParser.Evaluate(_parsedConditions);
            
            _isDone = true;

            return result;
        }

        public List<ProfileBehavior> Children
        {
            get { return GetNodes().ToList(); }
            set { Body = value; }
        }        

        public override void ResetCachedDone()
        {
            foreach (ProfileBehavior behavior in Body)
            {
                behavior.ResetCachedDone();
            }
            _isDone = false;
        }

        private bool _readyToRun;
        public bool ReadyToRun
        {
            get { return _readyToRun; }
            set
            {
                _readyToRun = value; 
                Body.ForEach(b =>
                {
                    if(b is IAsyncProfileBehavior)
                        (b as IAsyncProfileBehavior).ReadyToRun = value;
                });            
            }
        }

        #region IAsyncProfileBehavior

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public bool ForceDone { get; set; }
        public void Tick() { }

        #endregion

    }

}
