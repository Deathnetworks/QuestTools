using System;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
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
                Logger.Log("Rift Complete Dialog is not visible");
                _isDone = true;
                return true;
            }

            if (UpgradeButton.IsVisible)
            {
                const string gemButtonUpgradeBase = "Root.NormalLayer.vendor_dialog_mainPage.riftReward_dialog.LayoutRoot.gemUpgradePane.items_list._content._stackpanel._tile"; // row0._item0";
                int gemsVisible = 0;
                for (int c = 0; c < 5; c++)
                {
                    string rowcol = gemButtonUpgradeBase + string.Format("row0._item{0}", c);
                    var gemUIbtn = UIElement.FromName(rowcol);
                    if (gemUIbtn != null && gemUIbtn.IsVisible)
                    {
                        gemsVisible++;
                    }
                }
                Random rand = new Random(DateTime.Now.Millisecond);
                var randGem = rand.Next(0, gemsVisible);
                string selectedGem = gemButtonUpgradeBase + string.Format("row0._item{0}", randGem);
                UIElement selectedGemButton = UIElement.FromName(selectedGem);
                if (selectedGemButton != null && selectedGemButton.IsVisible && selectedGemButton.IsEnabled)
                {
                    GameUI.SafeClickElement(selectedGemButton, "Gem Selection Icon");
                    await Coroutine.Sleep(250);
                    await Coroutine.Yield();
                }

                if (UpgradeButton != null && UpgradeButton.IsVisible && UpgradeButton.IsEnabled)
                {
                    GameUI.SafeClickElement(UpgradeButton, "Upgrade Button");
                    await Coroutine.Sleep(250);
                    await Coroutine.Yield();
                }
            }

            if (GameUI.IsElementVisible(ContinueButton) && ContinueButton.IsVisible && ContinueButton.IsEnabled)
            {
                GameUI.SafeClickElement(ContinueButton, "Continue Button");
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
            }

            if (ZetaDia.Me.AttemptUpgradeKeystone())
            {
                Logger.Log("Keystone Upgraded");
                await Coroutine.Sleep(250);
                await Coroutine.Yield();
            }
            else if (GameUI.IsElementVisible(UpgradeGemButton) && UpgradeGemButton.IsEnabled && UpgradeGemButton.IsEnabled)
            {
                var gems = ZetaDia.Actors.GetActorsOfType<ACDItem>().Where(item => item.ItemType == ItemType.LegendaryGem && item.InventorySlot == (InventorySlot)20);
                if (!gems.Any())
                    gems = ZetaDia.Actors.GetActorsOfType<ACDItem>().Where(item => item.ItemType == ItemType.LegendaryGem);
                if (gems.Any())
                {
                    Logger.Log("Upgrading Gem");
                    await CommonCoroutines.AttemptUpgradeGem(gems.FirstOrDefault());
                    await Coroutine.Sleep(250);
                    await Coroutine.Yield();
                }
            }

            return true;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
