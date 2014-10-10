using System.Collections.Generic;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("ProfileSetting")] 
    public class ProfileSettingTag : ProfileBehavior 
    { 
        private bool isDone; 
        public override bool IsDone 
        { 
            get { return isDone; } 
        } 

        [XmlAttribute("name")]	
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        public static Dictionary<string,string> ProfileSettings = new Dictionary<string, string>();
        public static bool Initialized;
        public static void Initialize()
        {
            BotMain.OnStart += bot => ProfileSettings.Clear();
            GameEvents.OnGameChanged += (sender, args) => ProfileSettings.Clear();
            Initialized = true;
        }

        protected override Composite CreateBehavior() 
        { 
            return new Action(ret =>
            {
                if (!Initialized)
                    Initialize();

                if (ProfileSettings.ContainsKey(Name))
                    ProfileSettings[Name] = Value;
                else
                    ProfileSettings.Add(Name,Value);

                Logger.Log("Setting Condition={0} to {1}", Name, Value);

				isDone = true;
                return RunStatus.Failure;
            }); 
        } 
    } 
}



