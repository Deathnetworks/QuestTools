using System.ComponentModel;
using System.Configuration;
using System.IO;
using Zeta.Common.Xml;
using Zeta.Game;
using Zeta.XmlEngine;

namespace QuestTools
{
    [XmlElement("QuestToolsSettings")]
    class QuestToolsSettings : XmlSettings
    {
        private static QuestToolsSettings _instance;
        private bool debugEnabled;
        private bool allowProfileReloading;
        private bool reloadProfileOnDeath;
        private bool allowProfileRestarts;

        private static string _battleTagName;
        public static string BattleTagName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_battleTagName) && ZetaDia.Service.Hero.IsValid)
                    _battleTagName = ZetaDia.Service.Hero.BattleTagName;
                return _battleTagName;
            }
        }

        public QuestToolsSettings() :
            base(Path.Combine(SettingsDirectory, "QuestTools", BattleTagName, "QuestToolsSettings.xml"))
        {
        }

        public static QuestToolsSettings Instance
        {
            get { return _instance ?? (_instance = new QuestToolsSettings()); }
        }

        [XmlElement("DebugEnabled")]
        [DefaultValue(false)]
        [Setting]
        public bool DebugEnabled
        {
            get
            {
                return debugEnabled;
            }
            set
            {
                debugEnabled = value;
                OnPropertyChanged("DebugEnabled");
            }
        }

        [XmlElement("AllowProfileReloading")]
        [DefaultValue(false)]
        [Setting]
        public bool AllowProfileReloading
        {
            get
            {
                return allowProfileReloading;
            }
            set
            {
                allowProfileReloading = value;
                OnPropertyChanged("AllowProfileReloading");
            }
        }

        [XmlElement("ReloadProfileOnDeath")]
        [DefaultValue(false)]
        [Setting]
        public bool ReloadProfileOnDeath
        {
            get
            {
                return reloadProfileOnDeath;
            }
            set
            {
                reloadProfileOnDeath = value;
                OnPropertyChanged("ReloadProfileOnDeath");
            }
        }

        [XmlElement("AllowProfileRestarts")]
        [DefaultValue(true)]
        [Setting]
        public bool AllowProfileRestarts
        {
            get
            {
                return allowProfileRestarts;
            }
            set
            {
                allowProfileRestarts = value;
                OnPropertyChanged("AllowProfileRestarts");
            }
        }

    }
}
