using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.TreeSharp;

namespace QuestTools.ProfileTags.Complex
{
    public interface IAsyncProfileBehavior
    {
        /// <summary>
        /// Returns the parent/original CreateBehaviorComposite
        /// </summary>
        /// <returns></returns>
        Composite AsyncGetBaseBehavior();

        /// <summary>
        /// returns the CreateBehavior composite
        /// </summary>
        /// <returns></returns>
        Composite AsyncGetBehavior();

        /// <summary>
        /// ProfileBehavior expects .Behavior to contain the composite so we have to
        /// populate .Behavior using ProfileBehavior.UpdateBehavior().
        /// AsyncUpdateBehavior should expose this method.
        /// Call ResetCachedDone after this.
        /// </summary>
        void AsyncUpdateBehavior();
        
        bool ReadyToRun { get; set; }
        bool ForceDone { get; set; }
        void Tick();
    }    
}