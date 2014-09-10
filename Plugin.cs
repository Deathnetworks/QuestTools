using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuestTools.Helpers;
using QuestTools.UI;
using Zeta.Common.Plugins;

namespace QuestTools
{
    public class Plugin : IPlugin
    {
        private const string NAME = "QuestTools";
        private const string AUTHOR = "rrrix";
        private const string DESCRIPTION = "Advanced Demonbuddy Profile Support";

        public Version Version { get { return QuestTools.PluginVersion; } }
        internal static DateTime LastPluginPulse = DateTime.MinValue;
        public static double GetMillisecondsSincePulse()
        {
            return DateTime.UtcNow.Subtract(LastPluginPulse).TotalMilliseconds;
        }

        public void OnPulse()
        {
            LastPluginPulse = DateTime.UtcNow;
            QuestTools.Pulse();
        }
        public void OnEnabled()
        {
            Logger.Log("v{0} Enabled", Version);

            BotEvents.WireUp();

            TabUi.InstallTab();
        }

        public void OnDisabled()
        {
            Logger.Log("v{0} Disabled", Version);
            BotEvents.UnWire();

            TabUi.RemoveTab();
        }

        public void OnShutdown() { }

        public string Author
        {
            get { return AUTHOR; }
        }

        public string Description
        {
            get { return DESCRIPTION; }
        }

        public System.Windows.Window DisplayWindow
        {
            get { return ConfigWindow.GetDisplayWindow(); }
        }

        public string Name
        {
            get { return NAME; }
        }

        public void OnInitialize()
        {

        }

        public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }
    }

}
