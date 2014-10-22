using System;
using QuestTools.Helpers;
using QuestTools.ProfileTags;
using System.Linq;
using Zeta.Bot.Settings;
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

        public static bool IsCastingOrLoading()
        {
            return 

                ZetaDia.Me != null && 
                ZetaDia.Me.IsValid && 
                ZetaDia.Me.CommonData != null && 
                ZetaDia.Me.CommonData.IsValid &&
                (
                    ZetaDia.IsLoadingWorld ||
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.Casting || 
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.Channeling || 
                    ZetaDia.Me.CommonData.AnimationState == AnimationState.Transform || 
                    ZetaDia.Me.CommonData.AnimationState.ToString() == "13"
                );
        }   

        public static bool CurrentWave(int waveNumber)
        {
            return RiftTrial.CurrentWave == waveNumber;
        }
        
        public static bool CurrentSceneName(string sceneName)
        {
            return ZetaDia.Me.CurrentScene.Name.ToLowerInvariant() == sceneName.ToLowerInvariant();          
        }

        public static bool CurrentDifficulty(string difficulty)
        {
            GameDifficulty d;
            return Enum.TryParse(difficulty, true, out d) && CharacterSettings.Instance.GameDifficulty == d;
        }

        public static bool CurrentDifficultyLessThan(string difficulty)
        {
            GameDifficulty d;
            if (Enum.TryParse(difficulty, true, out d))
            {
                var currentIndex = (int) CharacterSettings.Instance.GameDifficulty;
                var testIndex = (int) d;

                return currentIndex < testIndex;
            }
            return false;
        }

        public static bool CurrentDifficultyGreaterThan(string difficulty)
        {
            GameDifficulty d;
            if (Enum.TryParse(difficulty, true, out d))
            {
                var currentIndex = (int)CharacterSettings.Instance.GameDifficulty;
                var testIndex = (int)d;

                return currentIndex > testIndex;
            }
            return false;
        }

        public static bool CurrentClass(string actorClass)
        {
            ActorClass a;
            return Enum.TryParse(actorClass, true, out a) && ZetaDia.Service.Hero.Class == a;
        }

        public static bool CurrentHeroLevel(int level)
        {
            return ZetaDia.Service.Hero.Level == level;
        }

        public static bool HighestKeyCountId(int id)
        {
            return !Keys.IsAllSameCount && Keys.HighestKeyId == id;
        }

        public static bool LowestKeyCountId(int id)
        {
            return !Keys.IsAllSameCount && Keys.LowestKeyId == id;
        }

        public static int ItemCount(int actorId)
        {
            var items = ZetaDia.Me.Inventory.StashItems.Where(item => actorId == item.ActorSNO)
                .Concat(ZetaDia.Me.Inventory.Backpack.Where(item => actorId == item.ActorSNO)).ToList();

            return items.Select(i => i.ItemStackQuantity).Aggregate((a, b) => a + b);
        }

        public static bool ItemCountGreaterThan(int actorId, int amount)
        {
            return ItemCount(actorId) > amount;
        }

        public static bool ItemCountLessThan(int actorId, int amount)
        {
            return ItemCount(actorId) < amount;
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

        public static bool UsedOnce(string id)
        {
            return UseOnceTag.UseOnceIDs.Contains(id);
        }


    }
}
