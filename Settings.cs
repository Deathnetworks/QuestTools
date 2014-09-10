using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using QuestTools.Navigation;
using Zeta.Common.Xml;
using Zeta.Game;
using Zeta.XmlEngine;

namespace QuestTools
{
    [XmlElement("QuestToolsSettings")]
    class QuestToolsSettings : XmlSettings
    {
        public enum RiftUpgradePriority
        {
            RiftKey,
            Gem
        }

        public enum RiftKeyUsePriority
        {
            Normal,
            Trial,
            Greater
        }

        private static QuestToolsSettings _instance;
        private bool _debugEnabled;
        private bool _allowProfileReloading;
        private bool _allowProfileRestarts;
        private bool _skipCutScenes;
        private bool _forceRouteMode;
        private RouteMode _routeMode;

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
            base(Path.Combine(SettingsDirectory, BattleTagName, "QuestTools", "QuestToolsSettings.xml"))
        {
            if (_riftKeyUsePriority == null)
            {
                _riftKeyUsePriority = new List<RiftKeyUsePriority>();
                foreach (var use in Enum.GetValues(typeof(RiftKeyUsePriority)).Cast<RiftKeyUsePriority>())
                {
                    _riftKeyUsePriority.Add(use);
                }
            }
            if (_gemPriority == null)
            {
                _gemPriority = DataDictionary.LegendaryGems.Select(g => g.Value).ToList();
            }
        }

        public static QuestToolsSettings Instance
        {
            get { return _instance ?? (_instance = new QuestToolsSettings()); }
        }

        [XmlElement("ForceRouteMode")]
        [DefaultValue(false)]
        [Setting]
        public bool ForceRouteMode
        {
            get
            {
                return _forceRouteMode;
            }
            set
            {
                _forceRouteMode = value;
                OnPropertyChanged("ForceRouteMode");
            }
        }

        [XmlElement("RouteMode")]
        [DefaultValue(RouteMode.Default)]
        [Setting]
        public RouteMode RouteMode
        {
            get
            {
                return _routeMode;
            }
            set
            {
                _routeMode = value;
                OnPropertyChanged("RouteMode");
            }
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

        // 2.1 Rift Settings below

        private List<RiftKeyUsePriority> _riftKeyUsePriority;
        [XmlElement("RiftKeyPriority")]
        public List<RiftKeyUsePriority> RiftKeyPriority
        {
            get
            {
                return _riftKeyUsePriority;
            }
            set
            {
                _riftKeyUsePriority = value;
                OnPropertyChanged("RiftKeyPriority");
            }
        }

        private List<string> _gemPriority;
        [XmlElement("GemPriority")]
        public List<string> GemPriority
        {
            get
            {
                return _gemPriority;
            }
            set
            {
                _gemPriority = value;
                OnPropertyChanged("GemPriority");
            }
        }
        private float _minimumGemChance;
        [XmlElement("MinimumGemChance")]
        [DefaultValue(0.6f)]
        public float MinimumGemChance
        {
            get
            {
                return _minimumGemChance;
            }
            set
            {
                _minimumGemChance = value;
                OnPropertyChanged("MinimumGemChance");
            }
        }

        private bool _upgradeKeyStones;
        [XmlElement("UpgradeKeyStones")]
        [DefaultValue(true)]
        public bool UpgradeKeyStones
        {
            get
            {
                return _upgradeKeyStones;
            }
            set
            {
                _upgradeKeyStones = value;
                OnPropertyChanged("UpgradeKeyStones");
            }
        }
    }
}
