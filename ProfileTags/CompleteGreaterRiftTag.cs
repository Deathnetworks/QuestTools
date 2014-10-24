using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Buddy.Coroutines;
using Org.BouncyCastle.Ocsp;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Logic;
using Zeta.Bot.Profile;
using Zeta.Bot.Profile.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("CompleteGreaterRift")]
    public class CompleteGreaterRiftTag : ProfileBehavior, IEnhancedProfileBehavior
    {
        private bool _isDone;
        private bool _isGemsOnly;
        public override bool IsDone
        {
            get { return _isDone || !IsActiveQuestStep; }
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => CompleteGreaterRiftRoutine());
        }

        public static UIElement VendorDialog { get { return UIElement.FromHash(0x244BD04C84DF92F1); } }
        public static UIElement UpgradeKeystoneButton { get { return UIElement.FromHash(0x4BDE2D63B5C36134); } }
        public static UIElement UpgradeGemButton { get { return UIElement.FromHash(0x826E5716E8D4DD05); } }
        public static UIElement ContinueButton { get { return UIElement.FromHash(0x1A089FAFF3CB6576); } }
        public static UIElement UpgradeButton { get { return UIElement.FromHash(0xD365EA84F587D2FE); } }
        public static UIElement VendorCloseButton { get { return UIElement.FromHash(0xF98A8466DE237BD5); } }

        public async Task<bool> CompleteGreaterRiftRoutine()
        {
            if (!GameUI.IsElementVisible(VendorDialog))
            {
                Logger.Log("Rift Vendor Dialog is not visible");
                _isDone = true;
                return true;
            }

            if (GameUI.IsElementVisible(ContinueButton) && ContinueButton.IsVisible && ContinueButton.IsEnabled)
            {
                GameUI.SafeClickElement(ContinueButton, "Continue Button");
                GameUI.SafeClickElement(VendorCloseButton, "Vendor Window Close Button");
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
            }

            if (QuestToolsSettings.Instance.UpgradeKeyStones && await UpgradeKeyStoneTask())
                return true;

            if (!await UpgradeGemsTask()) //Attempt to upgrade gems, if there are no gems within the minimum upgrade range -> Upgrade KeyStone
            {
                await UpgradeKeyStoneTask();
            }

            return true;
        }

        private async Task<bool> UpgradeKeyStoneTask()
        {
            if (GameUI.IsElementVisible(UpgradeKeystoneButton) && UpgradeKeystoneButton.IsEnabled && ZetaDia.Me.AttemptUpgradeKeystone())
            {
                Logger.Log("Keystone Upgraded");
                GameUI.SafeClickElement(VendorCloseButton, "Vendor Window Close Button");
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
                return true;
            }

            _isGemsOnly = true;
            return false;
        }

        private async Task<bool> UpgradeGemsTask()
        {
            if (VendorDialog.IsVisible)
            {

                float minimumGemChance = QuestToolsSettings.Instance.MinimumGemChance;

                List<ACDItem> gems = ZetaDia.Actors.GetActorsOfType<ACDItem>()
                    .Where(item => item.ItemType == ItemType.LegendaryGem)
                    .Where(item => GetUpgradeChance(item) >= minimumGemChance && GetUpgradeChance(item) > 0.00f)
                    .OrderByDescending(item => GetUpgradeChance(item))
                    .ThenByDescending(item => item.JewelRank).ToList();

                if (gems.Count == 0 && !_isGemsOnly) //No gems that can be upgraded - upgrade keystone
                {
                    return false;
                }

                if (gems.Count == 0 && _isGemsOnly)
                {
                    gems = ZetaDia.Actors.GetActorsOfType<ACDItem>()
                    .Where(item => item.ItemType == ItemType.LegendaryGem)
                    .Where(item => GetUpgradeChance(item) > 0.00f)
                    .OrderByDescending(item => GetUpgradeChance(item))
                    .ThenByDescending(item => item.JewelRank).ToList();
                }

                _isGemsOnly = true;

                int selectedGemId = int.MaxValue;
                string selectedGemPreference = "";
                foreach (string gemName in QuestToolsSettings.Instance.GemPriority)
                {
                    selectedGemId = DataDictionary.LegendaryGems.FirstOrDefault(kv => kv.Value == gemName).Key;

                    // Map to known gem type or dynamic priority
                    if (selectedGemId == int.MaxValue)
                    {
                        Logger.Error("Invalid Gem Name: {0}", gemName);
                        continue;
                    }

                    // Equipped Gems
                    if (selectedGemId == 0)
                    {
                        selectedGemPreference = gemName;
                        if (gems.Any(IsGemEquipped))
                        {
                            gems = gems.Where(item => item.InventorySlot == (InventorySlot)20).ToList();
                            break;
                        }
                    }

                    // Lowest Rank
                    if (selectedGemId == 1)
                    {
                        selectedGemPreference = gemName;
                        gems = gems.OrderBy(item => item.JewelRank).ToList();
                        break;
                    }

                    // Highest Rank
                    if (selectedGemId == 2)
                    {
                        selectedGemPreference = gemName;
                        gems = gems.OrderByDescending(item => item.JewelRank).ToList();
                        break;
                    }

                    // Selected gem
                    if (gems.Any(i => i.ActorSNO == selectedGemId))
                    {
                        selectedGemPreference = gemName;
                        if (gems.Any(i => i.ActorSNO == selectedGemId))
                        {
                            gems = gems.Where(i => i.ActorSNO == selectedGemId).Take(1).ToList();
                            break;
                        }

                    }

                    // No gem found... skip!
                }

                if (selectedGemId < 10)
                {
                    Logger.Log("Using gem priority of {0}", selectedGemPreference);
                }

                var bestgem = gems.FirstOrDefault();

                if (bestgem != null && await CommonCoroutines.AttemptUpgradeGem(bestgem))
                {
                    await Coroutine.Sleep(250);
                    GameUI.SafeClickElement(VendorCloseButton, "Vendor Window Close Button");
                    await Coroutine.Yield();
                    return true;
                }
                else
                {
                    /*
                     * Demonbuddy MAY randomly fail to upgrade the selected gem. This is a workaround, in case we get stuck...
                     */

                    var randomGems = ZetaDia.Actors.GetActorsOfType<ACDItem>()
                                        .Where(item => item.ItemType == ItemType.LegendaryGem)
                                        .OrderBy(item => item.JewelRank).ToList();
                    Random random = new Random(DateTime.UtcNow.Millisecond);
                    int i = random.Next(0, randomGems.Count - 1);
                    var randomGem = gems[i];
                    Logger.Error("Gem Upgrade failed! Upgrading random Gem {0} ({1}) - {2:##.##}% {3} ", randomGem.Name, randomGem.JewelRank, GetUpgradeChance(randomGem) * 100, IsGemEquipped(randomGem) ? "Equipped" : string.Empty);
                    if (await CommonCoroutines.AttemptUpgradeGem(randomGem))
                    {
                    }
                    else
                    {
                        Logger.Error("Random gem upgrade also failed. Something... seriously... wrong... ");
                    }
                    return true;
                }
            }

            return false;
        }

        // xz jv was here
        public static Func<ACDItem, bool> IsGemEquipped = gem => (gem.InventorySlot == (InventorySlot)20);
        public static Func<ACDItem, float> GetUpgradeChance = gem =>
        {
            var delta = ZetaDia.Actors.Me.InTieredLootRunLevel - gem.JewelRank;

            if (delta >= 10) return 1f;
            if (delta <= -15) return 0f; //Diablo3 disables upgrades for -15 levels difference

            switch (delta)
            {
                case 9: return 0.9f;
                case 8: return 0.8f;
                case 7: return 0.7f;
                case 6: return 0.6f;
                case 5: return 0.6f;
                case 4: return 0.6f;
                case 3: return 0.6f;
                case 2: return 0.6f;
                case 1: return 0.6f;
                case 0: return 0.6f;
                case -1: return 0.3f;
                case -2: return 0.15f;
                case -3: return 0.08f;
                case -4: return 0.04f;
                case -5: return 0.02f;
                default: return 0.01f;
            }
        };


        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
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
