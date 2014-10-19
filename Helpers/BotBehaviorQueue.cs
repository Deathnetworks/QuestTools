using QuestTools.ProfileTags.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.Helpers
{
    /// <summary>
    /// Runs ProfileBehaviors using the BotBehavior hook
    /// Usage: BotBehaviorQueue.Queue(myProfileBehavior); 
    /// </summary>
    public static class BotBehaviorQueue
    {
        private const int MinimumCheckInterval = 250;
        public delegate bool ShouldRunCondition(List<ProfileBehavior> profileBehaviors);
        private static readonly List<QueueItem> Q = new List<QueueItem>();
        private static bool _hooksInserted;
        private static bool _wired;
        private static Decorator _hook;
        private static DateTime _lastCheckedConditionsTime = DateTime.MinValue;
        internal static QueueItemEqualityComparer QueueItemComparer = new QueueItemEqualityComparer();
        private static QueueItem _active;

        static BotBehaviorQueue()
        {
            WireUp();

            if (!_hooksInserted)
                InsertHooks();
        }

        public static int Count
        {
            get { return Q.Count; }
        }

        public static bool IsActive
        {
            get { return Q.Any() || (_active != null && _active.ActiveNode != null && !_active.ActiveNode.IsDone); }
        }

        public static bool IsEnabled
        {
            get { return _hooksInserted && _wired; }
        }
        //    OnDone = me => Logger.Log("[{1}] Completed {0}", me.CompletedNodes, me.Name),
        //    OnNodeDone = me => Logger.Log("[{0}] Complete {1}/{2}", me.Name, me.CompletedNodes, me.Nodes.Count),

        /// <summary>
        /// Selects a composite to be run
        /// </summary>
        private static Composite Next        
        {
            get
            {                                
                if (_active == null)
                {
                    if (!Q.Any())
                        return Continue;

                    // Handle start new QueueItem.
                    _active = Q.FirstOrDefault(n => n.ConditionPassed);
                    if (_active != null)
                    {
                        Q.Remove(_active);
                        if (_active.OnStart != null)
                            _active.OnStart.Invoke(_active);
                        return Loop;
                    }

                    return Continue;
                }

                
                if (_active.ActiveNode == null)
                {
                    // Handle starting first Node
                    _active.ActiveNode = _active.Nodes.First();
                    SetNodeToRun();
                    if (_active.OnNodeStart != null)
                        _active.OnNodeStart.Invoke(_active);
                    return _active.ActiveNode.Behavior;
                }

                BotMain.StatusText = _active.ActiveNode.StatusText;

                if (_active.ActiveNode.IsDone)
                {
                    // Handle ActiveNode has finished
                    _active.CompletedNodes++;
                    _active.ActiveNode.OnDone();
                    Logger.Debug("[{0}] Complete {1}/{2}", _active.Name, _active.CompletedNodes, _active.Nodes.Count);
                    if (_active.OnNodeDone != null)
                        _active.OnNodeDone.Invoke(_active);

                    // Handle all nodes are finished
                    if (_active.IsComplete)
                    {
                        if (_active.OnDone != null)
                            _active.OnDone.Invoke(_active);
                        Logger.Debug("[{1}] Completed {0}", _active.CompletedNodes, _active.Name);
                        _active = null;

                        CheckConditions();
                        return Loop;
                    }

                    // Handle start of next node
                    var currentPosition = _active.Nodes.IndexOf(_active.ActiveNode);
                    _active.ActiveNode = _active.Nodes.ElementAt(currentPosition + 1);
                    SetNodeToRun();
                    if (_active.OnNodeStart != null)
                        _active.OnNodeStart.Invoke(_active);
                    return _active.ActiveNode.Behavior;
                }

                // Handle continuing an in-progress Node
                Logger.Log("Contining: {0}", _active.ActiveNode.GetType());
                return _active.ActiveNode.Behavior;
            }
        }

        /// <summary>
        /// Calls UpdateBehavior() and OnStart() so that profile runs in the tree properly
        /// </summary>
        private static void SetNodeToRun()
        {
            ProfileUtils.RecurseBehaviors(_active.ActiveNode.GetChildren(), (node, index, type) =>
            {
                if (node is IEnhancedProfileBehavior)
                    (node as IEnhancedProfileBehavior).Update();

                return node;
            });

            _active.ActiveNode.Run();
        }

        /// <summary>
        /// Marks QueueItems as ready if their condition is True
        /// </summary>
        private static void CheckConditions()
        {
            if (Q.Any())
                Logger.Debug("{0} in Queue", Q.Count);

            Q.ForEach(node =>
            {
                if (node.ConditionPassed || !node.Condition.Invoke(node.Nodes)) return;

                node.ConditionPassed = true;

                Log("Triggered {1} with {0} Behaviors to be run at next opportunity",
                    node.Nodes.Count, (!string.IsNullOrEmpty(node.Name)) ? node.Name : "Unnamed");
            });
        }

        /// <summary>
        /// Magic self-updating composite
        /// </summary>
        /// <returns></returns>
        private static Decorator CreateMasterHook()
        {
            return new Decorator(ret =>
            {
                var child = _hook.DecoratedChild as PrioritySelector;
                if (child != null)
                    child.Children = new List<Composite> { Next };

                return true;

            }, new PrioritySelector());
        }

        private static Composite Continue
        {
            get { return new Action(ret => RunStatus.Failure); }
        }

        private static Composite Loop
        {
            get { return new Action(ret => RunStatus.Success); }
        }

        #region Methods for Queueing Items

        public static void Queue(IEnumerable<ProfileBehavior> profileBehaviors, string name = "")
        {
            Queue(profileBehaviors, ret => true, name);
        }

        public static void Queue(ProfileBehavior profileBehavior, ShouldRunCondition condition)
        {
            Queue(new List<ProfileBehavior> { profileBehavior }, condition);
        }

        public static void Queue(ProfileBehavior behavior, string name = "")
        {
            Queue(new List<ProfileBehavior> { behavior }, ret => true, name);
        }

        public static void Queue(IEnumerable<ProfileBehavior> profileBehaviors, ShouldRunCondition condition, string name = "")
        {
            var item = new QueueItem
            {
                Name = name,
                Nodes = profileBehaviors.ToList(),
                Condition = condition,
            };
            Queue(item);
        }

        public static void Queue(IEnumerable<QueueItem> items)
        {
            items.ForEach(Queue);
        }

        public static void Queue(QueueItem item)
        {
            if (!item.Nodes.Any())
            {
                Logger.Debug("Item {0} was queued without any nodes", item.Name);
                return;
            }

            if (QueueItemComparer.Equals(item, _active) || Q.Contains(item, QueueItemComparer))
            {
                Logger.Verbose("Discarding Duplicate Queue Request Name='{0}' Id='{1}'", item.Name, item.Id);
                return;
            }

            if (item.Condition == null)
                item.Condition = ret => true;

            ProfileUtils.AsyncReplaceTags(item.Nodes);
            Q.Add(item);           
        }

        #endregion

        #region Binding, Events, Hooks Etc

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
            if (!BotMain.IsRunning || !ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || ZetaDia.Me == null || !ZetaDia.Me.IsValid || ZetaDia.IsPlayingCutscene)
                return;

            if (DateTime.UtcNow.Subtract(_lastCheckedConditionsTime).TotalMilliseconds < MinimumCheckInterval)
                return;

            _lastCheckedConditionsTime = DateTime.UtcNow;

            CheckConditions();
        }

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
            _hook = CreateMasterHook();
            TreeHooks.Instance.InsertHook("BotBehavior", 0, _hook);
            _hooksInserted = true;
        }

        #endregion

        #region Utilities

        private static void Log(string message, params object[] args)
        {
            Logger.Log(message, args);
        }

        private static void LogBehavior(QueueItem item)
        {
            if (item != null && item.ActiveNode != null)
            {
                Logger.Log("{4}Tag {0} IsDone={1} IsDoneCache={2} LastStatus={3}",
                    item.ActiveNode.GetType(),
                    item.ActiveNode.IsDone,
                    item.ActiveNode.IsDoneCache,
                    item.ActiveNode.Behavior != null ? item.ActiveNode.Behavior.LastStatus.ToString() : "null",
                    string.IsNullOrEmpty(item.Name) ? string.Empty : "[" + item.Name + "] "
                    );
            }
        }

        public static void Reset()
        {
            Q.Clear();
            _active = null;
        }

        #endregion

    }

    public class QueueItem
    {
        public int Id { get; private set; }

        public string Name { get; set; }

        public int CompletedNodes = 0;

        public bool IsComplete { get { return CompletedNodes == Nodes.Count; } }

        public BotBehaviorQueue.ShouldRunCondition Condition;

        private List<ProfileBehavior> _nodes = new List<ProfileBehavior>();

        public ProfileBehavior ActiveNode { get; set; }

        public delegate void QueueItemDelegate(QueueItem item);

        public QueueItemDelegate OnNodeStart { get; set; }

        public QueueItemDelegate OnNodeDone { get; set; }

        public QueueItemDelegate OnDone { get; set; }

        public QueueItemDelegate OnStart { get; set; }

        public List<ProfileBehavior> Nodes
        {
            get { return _nodes; }
            set
            {
                var hash = value.Aggregate(0, (current, node) => current ^ node.GetHashCode());

                if (Name != null)
                    hash = hash ^ Name.GetHashCode();

                Id = hash;

                _nodes = value;
            }
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public bool ConditionPassed { get; set; }
    }

    public class QueueItemEqualityComparer : IEqualityComparer<QueueItem>
    {
        public bool Equals(QueueItem x, QueueItem y)
        {
            if (ReferenceEquals(x, y)) 
                return true;

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) 
                return false;

            return x.Name == y.Name;
        }

        public int GetHashCode(QueueItem obj)
        {
            return ReferenceEquals(obj, null) ? 0 : obj.Id;
        }
    }




}