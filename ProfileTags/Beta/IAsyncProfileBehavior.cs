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
        Composite BaseBehavior();
        bool ReadyToRun { get; set; }
        bool ForceDone { get; set; }
        void Tick();
    }    
}