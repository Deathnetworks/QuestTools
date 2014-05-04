using Zeta.Bot.Profile.Common;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Depreciated
{
    [XmlElement("TrinityLoadProfile")]
    public class TrinityLoadProfile : LoadProfileTag
    {
        public TrinityLoadProfile() { }

        public override void OnStart()
        {
            Logger.LogError("TrinityLoadProfile is decpreciated. Use <LoadProfile /> instead.");
            base.OnStart();
        }
    }
}
