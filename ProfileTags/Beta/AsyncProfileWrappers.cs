using QuestTools.ProfileTags.Beta;
using QuestTools.ProfileTags.Movement;
using System.Diagnostics;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags.Complex
{

    public class AsyncEmptyProfileBehavior : ProfileBehavior, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
        }

        public AsyncEmptyProfileBehavior()
        {
            BehaviorDelegate = DefaultBehavior;
        }

        public AsyncEmptyProfileBehavior(Composite behavior)
        {
            BehaviorDelegate = behavior;
        }

        private Composite DefaultBehavior 
        { 
            get 
            {
                return new Action(ret =>
                {
                    if (!ReadyToRun) _isDone = true;
                    return RunStatus.Success;
                });
            } 
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

    internal class AsyncLeaveGameTag : LeaveGameTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncLoadProfileTag : LoadProfileTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncLogMessageTag : LogMessageTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncUseWaypointTag : UseWaypointTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncOffsetMoveTag : OffsetMoveTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncMoveToActorTag : MoveToActor, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncMoveToMapMarkerTag : MoveToMapMarker, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncUseStopTag : UseStopTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncSafeMoveTo : SafeMoveToTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncExploreDungeonTag : ExploreDungeonTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncTownPortalTag : TownPortalTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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
    internal class AsyncWaitTimerTag : WaitTimerTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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

    internal class AsyncReloadProfileTag : ReloadProfileTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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
    internal class AsyncToggleTargetingTag : ToggleTargetingTag, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone || ForceDone; }
        }

        public Composite BaseBehavior()
        {
            return base.CreateBehavior();
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
}
