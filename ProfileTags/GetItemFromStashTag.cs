using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("GetItemFromStash")]
    public class GetItemFromStashTag : ProfileBehavior
    {
        private const int SharedStashSNO = 130400;

        [XmlAttribute("itemDynamicId")]
        public int ItemDynamicId { get; set; }

        [XmlAttribute("stackCount")]
        public int StackCount { get; set; }

        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || !IsActiveQuestStep; }
        }

        private static Vector3 StashLocation
        {
            get
            {
                switch (ZetaDia.CurrentLevelAreaId)
                {
                    case 19947: // Campaign A1 Hub
                        return new Vector3(2968.16f, 2789.63f, 23.94531f);
                    case 332339: // OpenWorld A1 Hub
                        return new Vector3(388.16f, 509.63f, 23.94531f);
                    case 168314: // A2 Hub
                        return new Vector3(323.0558f, 222.7048f, 0f);
                    case 92945: // A3/A4 Hub
                        return new Vector3(387.6834f, 382.0295f, 0f);
                    case 270011: // A5 Hub
                        return new Vector3(502.8296f, 739.7472f, 2.598635f);
                    default:
                        throw new ValueUnavailableException("Unknown LevelArea Id " + ZetaDia.CurrentLevelAreaId);
                }
            }
        }

        private static DiaGizmo SharedStash
        {
            get
            {
                return ZetaDia.Actors.GetActorsOfType<DiaGizmo>().FirstOrDefault(o => o.IsValid && o.ActorSNO == SharedStashSNO);
            }
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => GetItemFromStashRoutine());
        }

        private async Task<bool> GetItemFromStashRoutine()
        {
            if (!ZetaDia.IsInTown)
                await CommonCoroutines.UseTownPortal("Returning to Town to get Item");

            if (StashLocation.Distance2D(ZetaDia.Me.Position) > 10f)
                await CommonCoroutines.MoveAndStop(StashLocation, 10f, "Stash Location");

            if (StashLocation.Distance2D(ZetaDia.Me.Position) <= 10f && SharedStash != null && !UIElements.StashWindow.IsVisible)
            {
                SharedStash.Interact();
                await Coroutine.Sleep(500);
            }

            if (UIElements.StashWindow.IsVisible)
            {
               var itemList = ZetaDia.Me.Inventory.StashItems.ToList();
               if (itemList.All(item => item.DynamicId != ItemDynamicId))
               {
                   Logger.LogError("Unable to find item in stash with ItemId {0}", ItemDynamicId);
                   _isDone = true;
               }
            }

            return true;
        }

    }
}
