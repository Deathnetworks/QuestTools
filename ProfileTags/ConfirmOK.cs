using Zeta.Bot.Profile;
using Zeta.Game.Internals;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("ConfirmOK")]
    class ConfirmOK : ProfileBehavior
    {
        private bool isDone = false;
        public override bool IsDone
        {
            get { return isDone; }
        }

        protected override Composite CreateBehavior()
        {
            return 
            new PrioritySelector(
                new Decorator(ret => GameUI.IsElementVisible(UIElements.ConfirmationDialogOkButton),
                    new Sequence(
                        new Action(ret => Logger.Log("Clicking ConfirmationDialogOkButton")),
                        new Action(ret => GameUI.SafeClickElement(UIElements.ConfirmationDialogOkButton, "ConfirmationDialogOKButton")),
                        new Action(ret => isDone = true)
                    )
                ),
                new Decorator(ret => GameUI.IsElementVisible(GameUI.GenericOK),
                    new Sequence(
                        new Action(ret => Logger.Log("Clicking GenericOK")),
                        new Action(ret => GameUI.SafeClickElement(UIElements.ConfirmationDialogOkButton, "GenericOK")),
                        new Action(ret => isDone = true)
                    )
                ),
                new Action(ret => isDone = true)
            );            
        }

        public override void ResetCachedDone()
        {
            isDone = false;
            base.ResetCachedDone();
        }

    }
}
