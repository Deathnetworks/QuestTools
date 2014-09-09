using System.Linq;
using QuestTools.Helpers;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    // TrinityTownRun forces a town-run request
    [XmlElement("TownRun")]
    [XmlElement("TrinityTownRun")]
    public class TownRunTag : ProfileBehavior
    {
        public TownRunTag() { }

        private bool _isDone;

        public override bool IsDone
        {
            get { return _isDone; }
        }

        /// <summary>
        /// The number of free bag slots we must have in order to NOT do a town run
        /// </summary>
        [XmlAttribute("minFreeBagSlots")]
        public int MinFreeBagSlots { get; set; }

        /// <summary>
        /// The minimum durability we must have in order to NOT do a townrun
        /// </summary>
        [XmlAttribute("minDurabilityPercent")]
        public int MinDurability { get; set; }

        /// <summary>
        /// Returns true when the number of free slots is less than the MinFreeBagSlots
        /// </summary>
        /// <returns></returns>
        public bool CheckMinBagSlots()
        {
            return GetFreeSlots() < MinFreeBagSlots;
        }

        public int GetFreeSlots()
        {
            const int maxFreeSlots = 60;
            int slotsTaken = 0;

            if (MinFreeBagSlots == 0)
                return maxFreeSlots;

            foreach (var item in ZetaDia.Me.Inventory.Backpack)
            {
                slotsTaken++;
                if (item.IsTwoSquareItem)
                    slotsTaken++;
            }

            return maxFreeSlots - slotsTaken;
        }

        /// <summary>
        /// returns True when the lowest durability item is less than the min durabilty
        /// </summary>
        /// <returns></returns>
        public bool CheckDurability()
        {
            if (MinDurability == 0)
                return false;

            return GetMinDurability() <= MinDurability;
        }

        public float GetMinDurability()
        {
            return ZetaDia.Me.Inventory.Equipped.Min(i => i.DurabilityPercent) * 100;
        }

        public override void OnStart()
        {
            Logger.Log("TrinityTownRun, freeBagSlots={0} minDurabilityPercent={1}", MinFreeBagSlots, MinDurability);
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => CheckDurability() || CheckMinBagSlots(),
                    new Sequence(
                        new Action(ret => Logger.Log("Town-run request received, will town-run at next possible moment.")),
                //new Action(ret => Trinity.ForceVendorRunASAP = true),
                        new Action(ret => TrinityApi.SetField("Trinity", "ForceVendorRunASAP", true)),
                        new Action(ret => _isDone = true)
                    )
                ),
                    new Sequence(
                        new Action(ret => Logger.Log("Skipping TrinityTownRun")),
                        new Action(ret => _isDone = true)
                    )
            );
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
