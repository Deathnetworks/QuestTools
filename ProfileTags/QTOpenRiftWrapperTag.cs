using System;
using System.Linq;
using System.Threading.Tasks;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot;
using Zeta.Bot.Profile.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("QTOpenRiftWrapper")]
    public class QTOpenRiftWrapperTag : OpenRiftTag, IAsyncProfileBehavior
    {
        const int RiftPortalSno = 345935; 

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
            try
            {
                Pulsator.OnPulse += Pulsator_OnPulse;
                if (!ZetaDia.IsInTown)
                {
                    _isDone = true;
                    Logger.Log("Cannot open rift outside of town");
                    return;
                }

                var keyPriorityList = QuestToolsSettings.Instance.RiftKeyPriority;

                if (keyPriorityList.Count != 3)
                    throw new ArgumentOutOfRangeException("RiftKeyPriority", "Expected 3 Rift keys, settings are broken?");

                if (ZetaDia.Actors.GetActorsOfType<DiaObject>(true).Any(i => i.IsValid && i.ActorSNO == RiftPortalSno))
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
            catch (Exception ex)
            {
                Logger.Error("Error in QTOpenRiftWrapper: " + ex);
            }
        }

        void Pulsator_OnPulse(object sender, EventArgs e)
        {
            CheckForRiftPortal();
        }
        public override void OnDone()
        {
            Pulsator.OnPulse -= Pulsator_OnPulse;
            base.OnDone();
        }
        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new ActionRunCoroutine(ret => MainCoroutine()),
                base.CreateBehavior()
            );
        }

        private async Task<bool> MainCoroutine()
        {
            if (ZetaDia.Me.IsParticipatingInTieredLootRun)
            {
                Logger.Log("Already in Greater Rift!");
                _isDone = true;
                return false;
            }

            return CheckForRiftPortal();
        }

        private bool CheckForRiftPortal()
        {
            var portals = ZetaDia.Actors.GetActorsOfType<GizmoPortal>(true).Where(p => p.IsValid && p.ActorSNO == RiftPortalSno);
            if (portals.Any())
            {
                Logger.Log("Rift portal already open!");
                _isDone = true;
                return false;
            }
            return true;
        }

        public bool HasGreaterRiftKeys
        {
            get
            {
                var greaterKey = ZetaDia.Me.Inventory.Backpack.Any(
                    i => i.IsValid && i.ItemType == ItemType.KeystoneFragment 
                        && i.TieredLootRunKeyLevel > 0
                        && (QuestToolsSettings.Instance.UseHighestKeystone || i.TieredLootRunKeyLevel <= QuestToolsSettings.Instance.MaxGreaterRiftKey)
                    );
                return greaterKey;
            }
        }

        public bool HasTrialRiftKeys
        {
            get
            {
                return ZetaDia.Me.Inventory.Backpack.Any(i => i.IsValid && i.ItemType == ItemType.KeystoneFragment && i.TieredLootRunKeyLevel == 0);
            }
        }

        public bool HasNormalRiftKeys
        {
            get
            {
                return ZetaDia.Me.Inventory.Backpack.Any(i => i.IsValid && i.ItemType == ItemType.KeystoneFragment && i.TieredLootRunKeyLevel < 0);
            }
        }

        #region IAsyncProfileBehavior

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
    }

        public void AsyncOnStart()
        {
            OnStart();
}

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }
}
