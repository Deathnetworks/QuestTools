﻿using QuestTools.Helpers;
using QuestTools.ProfileTags.Beta;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Bot.Profile.Composites;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;
using ConditionParser = QuestTools.Helpers.ConditionParser;


namespace QuestTools.ProfileTags.Complex
{

    public class CompositeTag : ProfileBehavior, IAsyncProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get
            {
                var delegateIsDone = IsDoneDelegate != null && IsDoneDelegate.Invoke(null);
                return (QuestId > 1 && !IsActiveQuestStep) || _isDone || delegateIsDone;
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
        private bool _isDone;
        public override bool IsDone
        {
            get { return (QuestId > 1 && !IsActiveQuestStep) || _isDone || base.IsDone; }
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
        private bool _isDone;
        public override bool IsDone
        {
            get { return (QuestId > 1 && !IsActiveQuestStep) || _isDone || base.IsDone; }
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
        private bool _isDone;
        public override bool IsDone
        {
            get { return (QuestId > 1 && !IsActiveQuestStep) || _isDone || base.IsDone; }
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
        private bool _isDone;
        public override bool IsDone
        {
            get { return (QuestId > 1 && !IsActiveQuestStep) || _isDone || base.IsDone; }
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
        private bool _isDone;
        public override bool IsDone
        {
            get { return (QuestId > 1 && !IsActiveQuestStep) || _isDone || base.IsDone; }
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
        private bool _isDone;
        public override bool IsDone
        {
            get { return (QuestId > 1 && !IsActiveQuestStep) || _isDone || base.IsDone; }
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

