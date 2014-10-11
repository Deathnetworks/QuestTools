using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot.Profile;
using Zeta.TreeSharp;

namespace QuestTools.ProfileTags.Complex
{
    public interface IAsyncProfileBehavior
    {
        /// <summary>
        /// ProfileBehavior expects .Behavior to contain the composite
        /// UpdateBehavior() and OnStart() need to be exposed;        
        /// </summary>
        void AsyncUpdateBehavior();
        void AsyncOnStart();

        bool ReadyToRun { get; set; }
        bool ForceDone { get; set; }
        void Tick();
    }
}
