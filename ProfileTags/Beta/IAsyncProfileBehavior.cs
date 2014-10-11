using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot.Profile;
using Zeta.TreeSharp;

namespace QuestTools.ProfileTags.Complex
{    
    public interface IAsyncProfileBehavior
    {
        /// <summary>
        /// ProfileBehavior expects .Behavior to contain the composite 
        /// Call this.UpdateBehavior() in implementation.    
        /// </summary>
        void AsyncUpdateBehavior();

        /// <summary>
        /// Many tags use OnStart for setup and default params
        /// Call this.OnStart() in implementation.
        /// </summary>
        void AsyncOnStart();

        /// <summary>
        /// Sets a behavior to Done
        /// Set _isDone = true in implementation.
        /// Call Done() on children if INodeContainer
        /// </summary>
        void Done();

        ///// <summary>
        ///// When true, should prevent a tag from Running
        ///// </summary>
        //bool ReadyToRun { get; set; }
    }
}
