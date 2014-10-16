using QuestTools.Helpers;
using QuestTools.ProfileTags;
using System.Linq;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using ConditionParser = Zeta.Bot.ConditionParser;

namespace QuestTools
{
    public static class CustomConditions
    {
        public static void Initialize()
        {
            ScriptManager.RegisterShortcutsDefinitions((typeof(CustomConditions)));
        }
        
        public static int CurrentWave
        {
            get { return RiftTrial.CurrentWave; }
        }
        
        public static string CurrentSceneName
        {
            get { return ZetaDia.Me.CurrentScene.Name; }            
        }

        public static string CurrentDifficulty
        {
            get { return ZetaDia.Service.Hero.CurrentDifficulty.ToString(); }
        }

        public static string CurrentClass
        {
            get { return ZetaDia.Service.Hero.Class.ToString(); }
        }

        public static int CurrentHeroLevel
        {
            get { return ZetaDia.Service.Hero.Level; }
        }

        public static int HighestKeyCountId
        {
            get { return !Keys.IsAllSameCount ? Keys.HighestKeyId : 0; }
        }

        public static int LowestKeyCountId
        {
            get { return !Keys.IsAllSameCount ? Keys.LowestKeyId : 0; }
        }

        public static bool IsInGame
        {
            get { return ZetaDia.IsInGame; }
        }

        public static int ItemCount(int actorId)
        {
            var items = ZetaDia.Me.Inventory.StashItems.Where(item => actorId == item.ActorSNO)
                .Concat(ZetaDia.Me.Inventory.Backpack.Where(item => actorId == item.ActorSNO)).ToList();

            return items.Select(i => i.ItemStackQuantity).Aggregate((a, b) => a + b);
        }

        public static bool ActorAnimationCountReached(int actorId, string animationName, int count)
        {
            if (ActorHistory.GetActorAnimationCount(actorId, animationName) <= count) 
                return false;

            ActorHistory.UnitsWithAnimationTracking.Remove(actorId);
            return true;
        }

        public static string ProfileSetting(string key)
        {
            string value;
            return ProfileSettingTag.ProfileSettings.TryGetValue(key, out value) ? value : string.Empty;
        }

        public static bool ActorIsAlive(int actorId)
        {
            var actor = ZetaDia.Actors.GetActorsOfType<DiaUnit>().FirstOrDefault(a => a.IsValid && a.ActorSNO == actorId && a.IsAlive);

            // Its possible that by the time other tags run this actor will have disappeared from actors collection
            // to make sure we dont lose track of it, it needs to be recorded in the history.
            ActorHistory.UpdateActor(actor);

            return actor != null;
        }

        public static bool ActorFound(int actorId)
        {
            return ActorHistory.HasBeenSeen(actorId) || ActorIsAlive(actorId);
        }

        public static bool ActorExistsNearMe(int actorId, float range)
        {
            return ConditionParser.ActorExistsAt(actorId, ZetaDia.Me.Position.X, ZetaDia.Me.Position.Y, ZetaDia.Me.Position.Z, range);
        }

        public static bool KeyAboveMedian(int actorId)
        {
            return Keys.GetKeyCount(actorId) > Keys.Median;
        }

        public static bool KeyBelowMedian(int actorId)
        {
            return Keys.GetKeyCount(actorId) < Keys.Median;
        }

        public static bool KeyAboveUpperFence(int actorId)
        {
            return Keys.GetKeyCount(actorId) > Keys.UpperFence;
        }
        public static bool KeyBelowLowerFence(int actorId)
        {
            return Keys.GetKeyCount(actorId) < Keys.LowerFence;
        }

        public static bool KeyAboveUpperQuartile(int actorId)
        {
            return Keys.GetKeyCount(actorId) > Keys.UpperQuartile;
        }
        
        public static bool KeyBelowLowerQuartile(int actorId)
        {
            return Keys.GetKeyCount(actorId) < Keys.LowerQuartile;
        }           

    }
}
