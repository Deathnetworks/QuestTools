using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestTools
{
    class SettingsModel
    {
        public static QuestToolsSettings Settings { get { return QuestToolsSettings.Instance; } }

        public Dictionary<int, string> LegendaryGems { get { return DataDictionary.LegendaryGems; } } 
    }
}
