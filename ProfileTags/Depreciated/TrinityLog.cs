using Zeta.Bot.Profile.Common;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Depreciated
{
    [XmlElement("TrinityLog")]
    public class TrinityLog : LogMessageTag
    {
        public TrinityLog() { }

        public override void OnStart()
        {
            Logger.LogError("TrinityLog is decpreciated. Use <LogMessage /> instead.");
            base.OnStart();
        }
    }
}
