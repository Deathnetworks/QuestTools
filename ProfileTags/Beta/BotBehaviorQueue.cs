using System;
using System.Windows;
using Org.BouncyCastle.Security;
using QuestTools.ProfileTags.Complex;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.Helpers
{
    public static class BotBehaviorQueue
    {
        public delegate bool ShouldRunCondition(List<ProfileBehavior> profileBehaviors);

        public static HashSet<KeyValuePair<List<ProfileBehavior>, ShouldRunCondition>> ProfileBehaviorQueue = new HashSet<KeyValuePair<List<ProfileBehavior>, ShouldRunCondition>>(); 
        public static bool IsInitialized;
        public static bool HooksInserted;

        private static Composite _activeBehavior;
        private static ProfileBehavior _activeProfileBehavior;
        private static List<ProfileBehavior> _nodesToExecute = new List<ProfileBehavior>();
        private static Decorator _hook; 
        private static bool _wired;
        private static bool QueueIsInactive;
        private static DateTime _lastCheckedConditionsTime = DateTime.MinValue;
        private static int _conditionCount;
        private static Dictionary<ShouldRunCondition, string> _conditionNames = new Dictionary<ShouldRunCondition, string>();
        
        private const int MinimumCheckInterval = 50;

        public static void Initailize()
        {
            WireUp();
            
            if(!HooksInserted)
                InsertHooks();

            IsInitialized = true;
        }

        public static void WireUp()
        {
            if (_wired) return;

            BotMain.OnStart += OnStartHandler;
            GameEvents.OnGameChanged += OnGameChangedHandler;
            TreeHooks.Instance.OnHooksCleared += OnHooksClearedHandler;
            BrainBehavior.OnScheduling += OnSchedulingHandler;
            ProfileManager.OnProfileLoaded += OnProfileLoaded;

            _wired = true;
        }

        private static void OnProfileLoaded(object sender, EventArgs eventArgs)
        {
            Reset();
        }

        private static void OnStartHandler(IBot bot)
        {
            InsertHooks();
            Reset();
        }

        private static void OnGameChangedHandler(object sender, EventArgs eventArgs)
        {
            Reset();
        }

        private static void OnHooksClearedHandler(object sender, EventArgs eventArgs)
        {
            InsertHooks();
        }

        private static void OnSchedulingHandler(object sender, EventArgs eventArgs)
        {
            if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || ZetaDia.Me == null || !ZetaDia.Me.IsValid ||  ZetaDia.IsPlayingCutscene)
                return;

            if (DateTime.UtcNow.Subtract(_lastCheckedConditionsTime).TotalMilliseconds < MinimumCheckInterval)
                return;

            _lastCheckedConditionsTime = DateTime.UtcNow;

            CheckConditions();
        }

        private static bool hasTriggered;

        public static void UnWire()
        {
            if (!_wired) return;

            BotMain.OnStart -= OnStartHandler;
            GameEvents.OnGameChanged -= OnGameChangedHandler;
            TreeHooks.Instance.OnHooksCleared -= OnHooksClearedHandler;
            BrainBehavior.OnScheduling -= OnSchedulingHandler;
            ProfileManager.OnProfileLoaded -= OnProfileLoaded;

            _wired = false;
        }

        private static void InsertHooks()
        {
            Logger.Debug("Inserting BotBehaviorQueue Hook");
            _hook = BotBehaviorMasterHookComposite();
            TreeHooks.Instance.InsertHook("BotBehavior", 0, _hook);
            HooksInserted = true;
        }

        /// <summary>
        /// BotBehaviorMasterHookComposite remains in BotBehavior at all times, 
        /// but the behaviors within it are switched out when UpdateHookContents() is called
        /// </summary>
        private static void UpdateHookContents()
        {
            //Logger.Debug("Updating BotBehaviorQueue");

            var groupCompositeParent = _hook.DecoratedChild as PrioritySelector;

            if (groupCompositeParent == null)
                return;

            var groupCompositeNodes = groupCompositeParent.Children.First() as Sequence;

            if (groupCompositeNodes == null)
                return;

            groupCompositeNodes.Children.Clear();

            GetNodeComposites().ForEach(node => groupCompositeNodes.Children.Add(node));
        }

        /// <summary>
        /// Adds some ProfileBehaviors to the BotBehaviorQueue
        /// </summary>
        /// <param name="profileBehaviors">List of ProfileBehaviors that should be executed when condition is satisfied</param>
        /// <param name="condition">bool delegate is invoked every tick to check if the attached profileBehaviors should be run</param>
        public static void Queue(IEnumerable<ProfileBehavior> profileBehaviors, ShouldRunCondition condition, string name = "")
        {
            if(!IsInitialized)
                Initailize();

            var behaviorsList = profileBehaviors.ToList();

            behaviorsList.ForEach(b =>
            {
                if (b.QuestId == 0)
                    b.QuestId = 1;
                if (b.StepId == 0)
                    b.StepId = 1;
            });

            var pair = new KeyValuePair<List<ProfileBehavior>, ShouldRunCondition>(behaviorsList, condition);

            ProfileBehaviorQueue.Add(pair);

            if (!string.IsNullOrEmpty(name))
                _conditionNames.Add(condition,name);
        }

        /// <summary>
        /// Adds some ProfileBehaviors to the BotBehaviorQueue
        /// </summary>
        /// <param name="profileBehaviors">List of ProfileBehaviors that should be executed when condition is satisfied</param>
        public static void Queue(IEnumerable<ProfileBehavior> profileBehaviors, string name = "")
        {
            Queue(profileBehaviors, ret => true, name);
        }

        /// <summary>
        /// Adds some ProfileBehaviors to the BotBehaviorQueue
        /// </summary>
        public static void Queue(ProfileBehavior behavior, string name = "")
        {
            Queue(new List<ProfileBehavior> { behavior } , ret => true, name);
        }

        /// <summary>
        /// Adds a ProfileBehavior to the BotBehaviorQueue
        /// </summary>
        /// <param name="profileBehavior">List of ProfileBehaviors that should be executed when condition is satisfied</param>
        /// <param name="condition">bool delegate is invoked every tick to check if the attached profileBehaviors should be run</param>
        public static void Queue(ProfileBehavior profileBehavior, ShouldRunCondition condition)
        {
            Queue(new List<ProfileBehavior> { profileBehavior }, condition);
        }

        ///// <summary>
        ///// Adds a ProfileBehavior to the BotBehaviorQueue
        ///// </summary>
        ///// <param name="profileBehavior">List of ProfileBehaviors that should be executed when condition is satisfied</param>
        //public static void Queue(ProfileBehavior profileBehavior, string name="")
        //{
        //    Queue(new List<ProfileBehavior> { profileBehavior }, ret => true, name);
        //}

        ///// <summary>
        ///// Experimental
        ///// </summary>
        //public static void Queue(Composite behavior, ShouldRunCondition condition)
        //{
        //    var profileBehavior = new AsyncEmptyProfileBehavior()
        //    {
        //        BehaviorDelegate = behavior
        //    };
        //    Queue(new List<ProfileBehavior> { profileBehavior }, condition);
        //}

        ///// <summary>
        ///// Experimental
        ///// </summary>
        //public static void Queue(Composite behavior)
        //{
        //    Queue(behavior, ret => true);
        //}

        /// <summary>
        /// Parent Composite that gets injected to BotBehavior hook
        /// </summary>
        private static Decorator BotBehaviorMasterHookComposite()
        {
            return new Decorator(ret => !IsDone,
                new PrioritySelector(
                    new Sequence(DefaultAction())
                )
            );
        }

        private static Composite DefaultAction()
        {
            return new Action(ret => RunStatus.Failure);
        }

        /// <summary>
        /// Grabs the next ProfileBehavior from the queue and prepares it to be run
        /// </summary>
        private static IEnumerable<Composite> GetNodeComposites()
        {
            if (_nodesToExecute.Any())
            {
                QueueIsInactive = false;

                //Logger.Log("NodeToExecute Count={0}",_nodesToExecute.Count);

                _activeProfileBehavior = _nodesToExecute.First();

                _nodesToExecute.Remove(_activeProfileBehavior);

                //if (_activeProfileBehavior is IAsyncProfileBehavior)
                //{
                //    //(_activeProfileBehavior as IAsyncProfileBehavior).ReadyToRun = true;

                //    //// Properly populate the .Behavior field of a ProfileBehavior
                //    //if (_activeProfileBehavior.Behavior == null)
                //    //    (_activeProfileBehavior as IAsyncProfileBehavior).AsyncUpdateBehavior();

                //    //// Reset everything back through to the base
                //    //_activeProfileBehavior.ResetCachedDone();

                    
                //}
                //else
                //{
                //    Logger.Warn("Tag {0} is not yet supported by BotBehaviorQueue", _activeProfileBehavior.GetType());
                //}

                _activeProfileBehavior.Run();

                _activeBehavior = _activeProfileBehavior.Behavior;
                
                if (QuestTools.EnableDebugLogging)
                    LogBehavior(_activeProfileBehavior);

                return new[]
                {
                    _activeBehavior
                };

            }

            return new [] { DefaultAction() };

        }

        /// <summary>
        /// Check to see if there is anything in the queue that needs to be run
        /// Return of false will prevent treewalker from executing behaviors via Decorator in BotBehaviorMasterHookComposite
        /// </summary>
        private static bool IsDone
        {
            get
            {
                if (QuestTools.EnableDebugLogging)
                    LogBehavior(_activeProfileBehavior);
                
                if (IsCurrentNodeFinished)
                    UpdateHookContents();

                if (_activeProfileBehavior is IAsyncProfileBehavior)
                {
                    BotMain.StatusText = _activeProfileBehavior.StatusText;
                    //(_activeProfileBehavior as IAsyncProfileBehavior).Tick();
                }

                if (IsQueueActive) return false;

                if(_activeProfileBehavior!=null && QuestTools.EnableDebugLogging)
                    Log("Finished Running Behaviors");

                _activeProfileBehavior = null;
                QueueIsInactive = true;
                UpdateHookContents();

                return true;
            }
        }

        public static bool IsQueueActive
        {
            get { return _nodesToExecute.Any() || (_activeProfileBehavior != null && !_activeProfileBehavior.IsDone);  }
        }

        public static bool IsCurrentNodeFinished
        {
            get { return _nodesToExecute.Any() && (_activeProfileBehavior == null || _activeProfileBehavior.IsDone); }
        }

        // Avoid reflection derp [QuestTools][<>c__DisplayClass17] showing up in log message
        private static void Log(string message, params object[] args)
        {
            Logger.Log(message,args);
        }

        private static void LogBehavior(ProfileBehavior profileBehavior)
        {
            if (profileBehavior != null)
            {
                Logger.Log("Tag {0} IsDone={1} IsDoneCache={2} LastStatus={3}",
                    profileBehavior.GetType(),
                    profileBehavior.IsDone,
                    profileBehavior.IsDoneCache,
                    profileBehavior.Behavior!=null ? profileBehavior.Behavior.LastStatus.ToString() : "null"
                    );
            }            
        }

        /// <summary>
        /// Evaluates every condition in the ProfileBehaviorQueue.
        /// If any of them are true, they are chucked into the 'ready to go' bucket
        /// </summary>
        private static void CheckConditions()
        {
            var unreadyProfileBehaviors = new HashSet<KeyValuePair<List<ProfileBehavior>, ShouldRunCondition>>();

            if (QuestTools.EnableDebugLogging && ProfileBehaviorQueue.Any() && _conditionCount != ProfileBehaviorQueue.Count)
            {
                //Logger.Log("{0} Conditions will be monitored...", ProfileBehaviorQueue.Count);
                _conditionCount = ProfileBehaviorQueue.Count;
            }
                

            ProfileBehaviorQueue.ForEach(pair =>
            {
                var nodes = pair.Key;
                var condition = pair.Value;

                if (condition != null && condition.Invoke(nodes))
                {
                    if (QuestTools.EnableDebugLogging)
                    {
                        string name;
                        _conditionNames.TryGetValue(condition, out name);
                        Log("{1} Success! Running {0} Behaviors", nodes.Count, (!string.IsNullOrEmpty(name)) ? name : "Unnamed");
                    }

                    nodes.ForEach(_nodesToExecute.Add);
                }
                else
                {                    
                    //Logger.Debug("Condition Failed!");
                    unreadyProfileBehaviors.Add(pair);
                }
            });

            ProfileBehaviorQueue = unreadyProfileBehaviors;
     
        }

        public static void Reset()
        {
            if (QuestTools.EnableDebugLogging)
                Logger.Debug("Resetting BotBehaviorQueue");

            ProfileBehaviorQueue.Clear();
            _nodesToExecute.Clear();
            QueueIsInactive = true;
            _activeProfileBehavior = null;
            _activeBehavior = null;
            _conditionNames.Clear();
        }

    }
}
