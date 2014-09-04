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
        private bool _debugEnabled;
        private bool _allowProfileReloading;
        private bool _allowProfileRestarts;
        private bool _skipCutScenes;

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
        [DefaultValue(true)]
        [Setting]
        public bool DebugEnabled
        {
            get
            {
                return _debugEnabled;
            }
            set
            {
                _debugEnabled = value;
                OnPropertyChanged("DebugEnabled");
            }
        }

        [XmlElement("AllowProfileReloading")]
        [DefaultValue(true)]
        [Setting]
        public bool AllowProfileReloading
        {
            get
            {
                return _allowProfileReloading;
            }
            set
            {
                _allowProfileReloading = value;
                OnPropertyChanged("AllowProfileReloading");
            }
        }

        [XmlElement("AllowProfileRestarts")]
        [DefaultValue(true)]
        [Setting]
        public bool AllowProfileRestarts
        {
            get
            {
                return _allowProfileRestarts;
            }
            set
            {
                _allowProfileRestarts = value;
                OnPropertyChanged("AllowProfileRestarts");
            }
        }


        [XmlElement("SkipCutScenes")]
        [DefaultValue(true)]
        [Setting]
        public bool SkipCutScenes
        {
            get
            {
                return _skipCutScenes;
            }
            set
            {
                _skipCutScenes = value;
                OnPropertyChanged("SkipCutScenes");
            }
        }

    }
}
