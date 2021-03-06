﻿using Zeta.Bot.Profile.Composites;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Depreciated
{
    [XmlElement("TrinityIfSNOInRange")]
    public class TrinityIfSNOInRangeTag : IfTag
    {
        public TrinityIfSNOInRangeTag() { }
        public override void OnStart()
        {
            Logger.Error("TrinityIfSNOInRange is decpreciated. Use <If condition=\"ActorExistsAt(actorId, x, y, z, range)\" /> instead.");
            base.OnStart();
        }
    }
}
