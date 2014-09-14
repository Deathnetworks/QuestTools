using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Buddy.Coroutines;
using Org.BouncyCastle.Ocsp;
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
    public class CompleteGreaterRift : ProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone || !IsActiveQuestStep; }
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => CompleteGreaterRiftRoutine());
        }

        public UIElement VendorDialog { get { return UIElement.FromHash(0x244BD04C84DF92F1); } }
        public UIElement UpgradeKeystoneButton { get { return UIElement.FromHash(0x4BDE2D63B5C36134); } }
        public UIElement UpgradeGemButton { get { return UIElement.FromHash(0x826E5716E8D4DD05); } }
        public UIElement ContinueButton { get { return UIElement.FromHash(0x1A089FAFF3CB6576); } }
        public UIElement UpgradeButton { get { return UIElement.FromHash(0xD365EA84F587D2FE); } }
        public UIElement VendorCloseButton { get { return UIElement.FromHash(0xF98A8466DE237BD5); } }

        // Upgrade Keystone: [1D898330] Mouseover: 0x4BDE2D63B5C36134, Name: Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.rewardChoicePane.Container.advance_button
        // Upgrade Gem: [1D897180] Mouseover: 0x826E5716E8D4DD05, Name: Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.rewardChoicePane.Container.upgrade_button1
        // Continue Button: [1D895450] Mouseover: 0x1A089FAFF3CB6576, Name: Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.rewardChoicePane.Container.Continue
        // C1 R1 [227F5FB0] Mouseover: 0x680DF143A98CB58E, Name: Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.gemUpgradePane.items_list._content._stackpanel._tilerow0._item2
        // C2 R1 [1C5AABB0] Mouseover: 0x680DF043A98CB3DB, Name: Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.gemUpgradePane.items_list._content._stackpanel._tilerow0._item1
        // C3 R1 [21CDCF70] Mouseover: 0x680DEF43A98CB228, Name: Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.gemUpgradePane.items_list._content._stackpanel._tilerow0._item0        

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
                GameUI.SafeClickElement(VendorCloseButton);
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
            }

            if (QuestToolsSettings.Instance.UpgradeKeyStones && await UpgradeKeyStoneTask())
                return true;

            if (VendorDialog.IsVisible)
            {

                float minimumGemChance = QuestToolsSettings.Instance.MinimumGemChance;

                List<ACDItem> gems = ZetaDia.Actors.GetActorsOfType<ACDItem>()
                    .Where(item => item.ItemType == ItemType.LegendaryGem && GetUpgradeChance(item) > minimumGemChance)
                    .OrderByDescending(item => item.JewelRank).ToList();

                int selectedGemId = int.MaxValue;
                string selectedGemPreference = "";
                foreach (string gemName in QuestToolsSettings.Instance.GemPriority)
                {
                    //{0,"Equipped Gems"},
                    //{1,"Lowest Rank"},
                    //{2,"Highest Rank"},
                    //{405775,"Bane of the Powerful"},
                    //{405781,"Bane of the Trapped"},
                    //{405792,"Wreath of Lightning"},
                    //{405793,"Gem of Efficacious Toxin"},
                    //{405794,"Pain Enhancer"},
                    //{405795,"Mirinae, Teardrop of the Starweaver"},
                    //{405796,"Gogok of Swiftness"},
                    //{405797,"Invigorating Gemstone"},
                    //{405798,"Enforcer"},
                    //{405800,"Moratorium"},
                    //{405801,"Zei's Stone of Vengeance"},
                    //{405802,"Simplicity's Strength"},
                    //{405803,"Boon of the Hoarder"},
                    //{405804,"Taeguk"},

                    selectedGemId = DataDictionary.LegendaryGems.FirstOrDefault(kv => kv.Value == gemName).Key;

                    // Map to known gem type or dynamic priority
                    if (selectedGemId == int.MaxValue)
                    {
                        Logger.LogError("Invalid Gem Name: {0}", gemName);
                        continue;
                    }

                    // Equipped Gems
                    if (selectedGemId == 0)
                    {
                        selectedGemPreference = gemName;
                        gems = gems.Where(item => item.InventorySlot == (InventorySlot)20).ToList();
                        break;
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
                        gems = gems.Where(i => i.ActorSNO == selectedGemId).Take(1).ToList();
                        break;
                    }

                    // No gem found... skip!
                }

                if (!gems.Any())
                {
                    _isDone = true;
                    return true;
                }

                if (selectedGemId < 10)
                {
                    Logger.Log("Using gem priority of {0}", selectedGemPreference);
                }

                var bestgem = gems.First();

                Logger.Log("Upgrading Gem {0} ({1}) - {2:##.##}% {3} ", bestgem.Name, bestgem.JewelRank, GetUpgradeChance(bestgem) * 100, IsGemEquipped(bestgem) ? "Equipped" : string.Empty);
                await CommonCoroutines.AttemptUpgradeGem(gems.FirstOrDefault());
                await Coroutine.Sleep(250);
                GameUI.SafeClickElement(VendorCloseButton);
                await Coroutine.Yield();
            }

            return true;
        }

        // xz jv was here
        public static Func<ACDItem, bool> IsGemEquipped = gem => (gem.InventorySlot == (InventorySlot)20);
        public static Func<ACDItem, float> GetUpgradeChance = gem =>
        {
            var delta = ZetaDia.Actors.Me.InTieredLootRunLevel - gem.JewelRank;

            if (delta >= 10) return 1f;

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

        private async Task<bool> UpgradeKeyStoneTask()
        {
            if (GameUI.IsElementVisible(UpgradeKeystoneButton) && UpgradeKeystoneButton.IsEnabled && ZetaDia.Me.AttemptUpgradeKeystone())
            {
                Logger.Log("Keystone Upgraded");
                GameUI.SafeClickElement(VendorCloseButton);
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
                return true;
            }
            return false;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
