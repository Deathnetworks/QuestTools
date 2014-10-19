using QuestTools.Helpers;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Bot.Settings;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags.Complex
{
    /// <summary>
    /// Give commands for the bot to do unusual stuff
    /// </summary>
    [XmlElement("Command")]
    public class CommandTag : ProfileBehavior, IEnhancedProfileBehavior
    {
        private bool _isDone;

        public enum ActionType
        {
            None = 0,
            StopBot,
            SetNormal,
            SetExpert,
            SetHard,
            SetMaster,
            SetTorment1,
            SetTorment2,
            SetTorment3,
            SetTorment4,
            SetTorment5,
            SetTorment6,
        }

        [XmlAttribute("type")]
        public ActionType Type { get; set; }

        [XmlAttribute("reason")]
        public string Reason { get; set; }

        public override bool IsDone
        {
            get { return QuestId > 1 && !IsActiveQuestStep || _isDone; }
        }

        public override void OnStart()
        {
            Logger.Log("Performing action {0}", Type);

            var profileName = ProfileManager.CurrentProfile.Path;
            var reason = string.IsNullOrEmpty(Reason) ? string.Empty : "\nReason='" + Reason + "'";

            switch (Type)
            {
                case ActionType.StopBot:
                    Logger.Warn("Profile '" + profileName + "' requested bot be stopped." + reason);
                    BotMain.Stop(true);
                    break;

                case ActionType.SetNormal:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Normal." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Normal;
                    break;

                case ActionType.SetHard:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Hard." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Hard;
                    break;

                case ActionType.SetMaster:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Master." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Master;
                    break;

                case ActionType.SetExpert:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Expert." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Expert;
                    break;

                case ActionType.SetTorment1:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Torment 1." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Torment1;
                    break;

                case ActionType.SetTorment2:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Torment 2." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Torment2;
                    break;

                case ActionType.SetTorment3:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Torment 3." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Torment3;
                    break;

                case ActionType.SetTorment4:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Torment 4." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Torment4;
                    break;

                case ActionType.SetTorment5:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Torment 5." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Torment5;
                    break;

                case ActionType.SetTorment6:
                    Logger.Warn("Profile '" + profileName + "' requested difficulty change to Torment 6." + reason);
                    CharacterSettings.Instance.GameDifficulty = GameDifficulty.Torment6;
                    break;
            }
            
        }

        protected override Composite CreateBehavior()
        {
            _isDone = true;
            return new Action(ret => RunStatus.Success);
        }

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