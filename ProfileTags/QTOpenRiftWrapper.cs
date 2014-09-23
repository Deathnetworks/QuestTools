using System;
using System.Linq;
using Zeta.Bot.Profile.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("QTOpenRiftWrapper")]
    public class QTOpenRiftWrapper : OpenRiftTag
    {
        const int riftPortalSno = 396751;

        private bool _isDone;

        public override bool IsDone
        {
            get { return _isDone || base.IsDone; }
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        public override void OnStart()
        {
            if (!ZetaDia.IsInTown)
            {
                _isDone = true;
                Logger.Log("Cannot open rift outside of town");
                return;
            }

            var keyPriorityList = QuestToolsSettings.Instance.RiftKeyPriority.ToList();

            if (keyPriorityList.Count != 3)
                throw new ArgumentOutOfRangeException("RiftKeyPriority", "Expected 3 Rift keys, are settings are broken?");


            if (ZetaDia.Actors.GetActorsOfType<DiaObject>(true).Any(i => i.IsValid && i.ActorSNO == riftPortalSno))
            {
                Logger.Log("Rift Portal already open!");
                _isDone = true;
            }

            bool keyFound = false;
            foreach (var keyType in keyPriorityList)
            {
                if (keyType == RiftKeyUsePriority.Greater && HasGreaterRiftKeys)
                {
                    keyFound = true;
                    Logger.Log("Using Greater Rift Keystone to open the Rift Portal");
                    StartTiered = true;
                    UseHighest = QuestToolsSettings.Instance.UseHighestKeystone;
                    UseLowest = !UseHighest;
                    break;
                }
                if (keyType == RiftKeyUsePriority.Trial && HasTrialRiftKeys)
                {
                    keyFound = true;
                    Logger.Log("Using Trial Rift Keystone to open the Rift Portal");
                    StartTiered = true;
                    UseTrialStone = true;
                    break;
                }
                if (keyType == RiftKeyUsePriority.Normal && HasNormalRiftKeys)
                {
                    keyFound = true;
                    Logger.Log("Using Normal Rift Keystone to open the Rift Portal");
                    StartTiered = false;
                    UseTrialStone = false;
                    break;
                }
                StartTiered = false;
            }

            if (!keyFound)
            {
                // No rift keys... :(
                Logger.Log("No Rift Keys Found for QTOpenRiftWrapper :( Tag finished.");
                _isDone = true;
            }
            base.OnStart();
        }

        public bool HasGreaterRiftKeys
        {
            get
            {
                return ZetaDia.Actors.GetActorsOfType<ACDItem>().Any(i => i.IsValid && i.ItemType == ItemType.KeystoneFragment && i.TieredLootRunKeyLevel > 0);
            }
        }

        public bool HasTrialRiftKeys
        {
            get
            {
                return ZetaDia.Actors.GetActorsOfType<ACDItem>().Any(i => i.IsValid && i.ItemType == ItemType.KeystoneFragment && i.TieredLootRunKeyLevel == 0);
            }
        }

        public bool HasNormalRiftKeys
        {
            get
            {
                return ZetaDia.Actors.GetActorsOfType<ACDItem>().Any(i => i.IsValid && i.ItemType == ItemType.KeystoneFragment && i.TieredLootRunKeyLevel < 0);
            }
        }
    }
}
