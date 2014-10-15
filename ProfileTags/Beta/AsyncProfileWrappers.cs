using System;
using QuestTools.Helpers;
using QuestTools.ProfileTags.Beta;
using System.Collections.Generic;
using System.Linq;
using QuestTools.ProfileTags.Movement;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;
using ConditionParser = QuestTools.Helpers.ConditionParser;


namespace QuestTools.ProfileTags.Complex
{

    public class CompositeTag : ProfileBehavior, IAsyncProfileBehavior
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

        public AsyncCommonBehaviors.IsDoneCondition IsDoneDelegate;
        public Composite BehaviorDelegate { get; set; }
        protected override Composite CreateBehavior()
        {
            return BehaviorDelegate;
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncLeaveGameTag : LeaveGameTag, IAsyncProfileBehavior
    {
        public AsyncLeaveGameTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion     
    }

    public class AsyncLoadProfileTag : LoadProfileTag, IAsyncProfileBehavior
    {
        public AsyncLoadProfileTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncLogMessageTag : LogMessageTag, IAsyncProfileBehavior
    {
        public AsyncLogMessageTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncUseWaypointTag : UseWaypointTag, IAsyncProfileBehavior
    {
        public AsyncUseWaypointTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncWaitTimerTag : WaitTimerTag, IAsyncProfileBehavior
    {
        public AsyncWaitTimerTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncUseObjectTag : UseObjectTag, IAsyncProfileBehavior
    {
        public AsyncUseObjectTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncUsePowerTag : UsePowerTag, IAsyncProfileBehavior
    {
        public AsyncUsePowerTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }

    public class AsyncToggleTargetingTag : ToggleTargetingTag, IAsyncProfileBehavior
    {
        public AsyncToggleTargetingTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone || base.IsDone; }
        }

        public override void OnStart()
        {
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return AsyncCommonBehaviors.ExecuteReturnAlwaysSuccess(
                ret => !_isDone,
                ret => new Action(r => base.OnStart())
            );
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

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }


    [XmlElement("AsyncIf")]
    public class AsyncIfTag : IfTag, IAsyncProfileBehavior
    {
        public AsyncIfTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        private bool _initialized;
        private bool _firstRun = true;

        public bool ShouldRecheckCondition;

        public override bool IsDone
        {
            get
            {
                // If a QuestId is specified, it has to match
                if (QuestId > 1 && !IsActiveQuestStep)
                    return true;

                if (_isDone)
                    return true;

                Logger.Verbose("Children Finished? {0}", Body.All(p => p.IsDone));

                // End if children are finished
                if (Body.All(p => p.IsDone))
                {
                    _isDone = true;
                    return true;
                }

                Logger.Verbose("Should Check Condition? {0}", _firstRun || ShouldRecheckCondition);

                // Check Condition
                if (_firstRun || ShouldRecheckCondition)
                {
                    // End if condition is false
                    if (!GetConditionExec())
                    {
                        _isDone = true;
                        return true;
                    }
                    _firstRun = false;
                }

                return false;
            }
        }

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

            return ConditionParser.Evaluate(_parsedConditions);
        }

        public override void ResetCachedDone()
        {
            _firstRun = true;
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IAsyncProfileBehavior : INodeContainer

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
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

    [XmlElement("AsyncWhile")]
    public class AsyncWhileTag : WhileTag, IAsyncProfileBehavior
    {
        public AsyncWhileTag()
        {
            QuestId = QuestId <= 0 ? 1 : QuestId;
        }

        private bool _isDone;
        private bool _initialized;
        private bool _firstRun = true;

        public bool ShouldRecheckCondition;

        public override bool IsDone
        {
            get
            {
                // If a QuestId is specified, it has to match
                if (QuestId > 1 && !IsActiveQuestStep)
                    return true;

                if (_isDone)
                    return true;

                Logger.Verbose("Children Finished? {0}", Body.All(p => p.IsDone));

                // End if children are finished && condition is false, otherwise re-run them all
                if (Body.All(p => p.IsDone))
                {
                    if (GetConditionExec())
                    {
                        _isDone = false;                        
                        Body.ForEach(b => b.Run());
                        return true;
                    }
                  
                    _isDone = true;
                    return true;                  
                }

                Logger.Verbose("Should Check Condition? {0}", _firstRun || ShouldRecheckCondition);

                // Check Condition
                if (_firstRun || ShouldRecheckCondition)
                {
                    // End if condition is false
                    if (!GetConditionExec())
                    {
                        _isDone = true;
                        return true;
                    }
                    _firstRun = false;
                }

                return false;
            }
        }

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

            return ConditionParser.Evaluate(_parsedConditions);
        }

        public override void ResetCachedDone()
        {
            _firstRun = true;
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IAsyncProfileBehavior : INodeContainer

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
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

