using Zeta.Bot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    /// <summary>
    /// Prevents a useonce tag ID ever being used again
    /// </summary>                    
    [XmlElement("UseStop")]
    [XmlElement("TrinityUseStop")]
    public class UseStopTag : ProfileBehavior
    {
        public UseStopTag() { }
        private bool isDone = false;

        public override bool IsDone
        {
            get { return isDone; }
        }

        protected override Composite CreateBehavior()
        {
            return
            new Sequence(
                new PrioritySelector(
                    new Decorator(ret => UseOnceTag.UseOnceIDs.Contains(ID),
                        new Action(ret => UseOnceTag.UseOnceCounter[ID] = -1)
                    ),
                    new Sequence(
                        new Action(ret => UseOnceTag.UseOnceIDs.Add(ID)),
                        new Action(ret => UseOnceTag.UseOnceCounter.Add(ID, -1))
                    )
                ),
                new Action(ret => isDone = true)
            );
        }

        [XmlAttribute("id")]
        public string ID{ get; set; }

        public override void ResetCachedDone()
        {
            isDone = false;
            base.ResetCachedDone();
        }
    }
}
