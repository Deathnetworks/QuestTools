using Zeta.Bot.Profile.Composites;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Depreciated
{
    [XmlElement("TrinityIf")]
    public class TrinityIf : IfTag
    {
        public TrinityIf() { }
        public override void OnStart()
        {
            Logger.LogError("TrinityIf is decpreciated. Use <If condition=\"\" /> instead.");
            base.OnStart();
        }

    }
}
