﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
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
        public static List<QueueItem> Shelf = new List<QueueItem>();

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

        /// <summary>
        /// Marks QueueItems as ready if their condition is True
        /// </summary>
        private static void CheckConditions()
        {
            if (Q.Any())
                Logger.Verbose("{0} in Queue", Q.Count);

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
                    child.Children = new List<Composite> { Next() };

                return true;

            }, new PrioritySelector());
        }

        /// <summary>
        /// Selects a composite to be run
        /// This is called every tick and returns a composite
        /// Which composite is returned changes based on the QueueItem currently being processed.
        /// </summary>
        private static Composite Next()       
        {   
            // 1. No active node
            if (_active == null)
            {
                // 1.1 Nothing in the Queue.
                if (!Q.Any())
                    return Continue;

                // 1.2 Start the next QueueItem that has passed its condition
                _active = Q.FirstOrDefault(n => n.ConditionPassed);
                Logger.Verbose("Starting QueueItem");
                if (_active != null)
                {
                    Q.Remove(_active);
                    if (_active.OnStart != null)
                        _active.OnStart.Invoke(_active);
                    return Loop;
                }

                // 1.3 Nothing has passed condition yet.
                return Continue;
            }
            
            // 2. We're currently processing a QueueItem
            // But havent started processing its nodes.
            if (_active.ActiveNode == null)
            {
                // 2.1 Handle starting the first Node
                _active.ActiveNode = _active.Nodes.First();
                _active.ActiveNode.Run();
                if (_active.OnNodeStart != null)
                    _active.OnNodeStart.Invoke(_active);
                return _active.ActiveNode.Behavior;
            }
            
            BotMain.StatusText = _active.ActiveNode.StatusText;

            // 3. We're currently processing a QueueItem
            // And the current node is Done
            if (_active.ActiveNode.IsDone)
            {
                // 3.1 Handle ActiveNode has finished                
                _active.CompletedNodes++;
                _active.ActiveNode.OnDone();
                Logger.Verbose("[{0}] Complete {1}/{2}", _active.Name, _active.CompletedNodes, _active.Nodes.Count);
                if (_active.OnNodeDone != null)
                    _active.OnNodeDone.Invoke(_active);
                
                // 3.2 All nodes are finished, so the QueueItem is now Done.
                if (_active.IsComplete)
                {
                    // 3.2.1 Handle all nodes are finished
                    if (_active.OnDone != null)
                        _active.OnDone.Invoke(_active);                    
                    Logger.Verbose("[{1}] Completed {0}", _active.CompletedNodes, _active.Name);                    

                    // 3.2.2 Traverse Upwards
                    // If this QueueItem is a child, we need to continue with its parent
                    // Parent gets taken off the shelf (unpaused) and set as the new active Queueitem.
                    var parent = Shelf.FirstOrDefault(i => i.ParentOf == _active.Id);
                    Logger.Verbose("All Nodes Complete ParentId={0} ThisId={1}", parent != null ? parent.Id.ToString() : "Null", _active.Id );
                    if (parent != null)
                    {
                        _active = parent;
                        Shelf.Remove(parent);
                        Logger.Verbose("ShelfCount={0}", Shelf.Count);
                        return Loop;
                    }

                    // 3.2.3 Shove it back at the bottom of the queue if it should be repeated
                    if (_active.Repeat)
                    {
                        var temp = _active;
                        _active.Reset();
                        _active = null;
                        Queue(temp);
                        CheckConditions();
                        return Loop;
                    }

                    // 3.2.4 No parent, No Repeat, so just end the QueueItem 
                    _active = null;
                    CheckConditions();
                    return Loop;
                }

                // 3.3 Handle start of next node
                var currentPosition = _active.Nodes.IndexOf(_active.ActiveNode);
                _active.ActiveNode = _active.Nodes.ElementAt(currentPosition + 1);
                _active.ActiveNode.Run();
                if (_active.OnNodeStart != null)
                    _active.OnNodeStart.Invoke(_active);
                return _active.ActiveNode.Behavior;
            }

            // 4.1 Traverse Downwards
            // We're currently processing a QueueItem
            // And the current node is NOT Done
            // And the current node has children
            Logger.Verbose("ShelfCount={0}", Shelf.Count);
            var children = _active.ActiveNode.GetChildren();
            if (children.Count > 0)
            {
                Logger.Log("Processing {0} Children of '{1}' ({2})", children.Count, _active.Name, _active.Id);

                // Copy QueueItem so we can resume it later.
                var queueItemToShelve = _active;

                // Wrap the children as a new QueueItem                
                var childQueueItem = new QueueItem
                {
                    Name = string.Format("Children of {0}", _active.Name),
                    Nodes = _active.ActiveNode.GetChildren()                    
                };

                // Store a references between parent and child
                queueItemToShelve.ParentOf = childQueueItem.Id;
                childQueueItem.ChildOf = _active.Id;

                // Pause the active QueueItem by moving it to the shelf
                Shelf.Add(queueItemToShelve);

                // Start working on the children.
                _active = childQueueItem;
                return Loop;
            }

            // Handle continuing an in-progress Node
            LogBehavior(_active);
            return _active.ActiveNode.Behavior;
        
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
            Reset(true);
        }

        private static void OnGameChangedHandler(object sender, EventArgs eventArgs)
        {
            Reset(true);
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

        public static void Reset(bool forceClearAll = false)
        {
            if (forceClearAll)
                Q.Clear();
            else
                Q.RemoveAll(i => !i.Persist);

            Shelf.Clear();

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

        public bool ConditionPassed { get; set; }

        public bool Persist { get; set; }

        public int ParentOf { get; set; }

        public int ChildOf { get; set; }

        public bool Repeat { get; set; }

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

        public void Reset()
        {
            ActiveNode = null;
            ConditionPassed = false;
            CompletedNodes = 0;
            ChildOf = 0;
            ParentOf = 0;
            Nodes.ForEach(n => n.ResetCachedDone(true));
        }
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