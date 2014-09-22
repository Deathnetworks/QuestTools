
using System;
using System.Collections.Generic;
using System.Linq;
using QuestTools.Helpers;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace QuestTools.Helpers
{
    public static class Conditions
    {
        public enum ConditionType
        {
            Unknown = 0,
            Variable,
            Method,
            BoolMethod,
        }

        #region Variable Conditions

        /// <summary>
        /// Conditions that take no paremeters and result in a variable 
        /// that must be compared to another value
        /// </summary>
        public enum VariableConditionType
        {
            Unknown = 0,
            CurrentWave,
            CurrentLevelAreaId,
            HighestKeyCountId,
            LowestKeyCountId,
        }

        public static bool CurrentWave(Expression exp)
        {
            return ConditionParser.EvalInt(exp.Operator, QuestTools.RiftTrial.CurrentWave, exp.Value.ChangeType<int>());
        }

        public static bool CurrentLevelAreaId(Expression exp)
        {
            return ConditionParser.EvalInt(exp.Operator, ZetaDia.CurrentLevelAreaId, exp.Value.ChangeType<int>());
        }

        public static bool HighestKeyCountId(Expression exp)
        {
            if (exp.Operator != OperatorType.Equal && exp.Operator != OperatorType.NotEqual)
                return false;

            var value = exp.Value.ChangeType<int>();

            int[] keyIds = { 364694, 364695, 364696, 364697 };
            int[] keyCounts = { 0, 0, 0, 0 };

            Func<ACDItem, int, bool> isKeyId = (acd, sno) => keyIds.Any(k => k == acd.ActorSNO);

            List<ACDItem> keys = ZetaDia.Me.Inventory.StashItems.Where(isKeyId).Concat(ZetaDia.Me.Inventory.Backpack.Where(isKeyId)).ToList();

            keys.ForEach(key => { keyCounts[Array.IndexOf(keyIds,key.ActorSNO)] += key.ItemStackQuantity; });

            int maxCount = keyCounts.Max();
            int highestKey = keyIds[Array.IndexOf(keyCounts,maxCount)];
            var isAllSameCount = keyCounts.All(count => count == keyCounts[0]);

            Logger.Log(string.Format("Key Counts: Act 1 => {0},  Act 2 => {1},  Act 3 => {2}, Act 4 => {3}", keyCounts[0], keyCounts[1], keyCounts[2], keyCounts[3]));

            return (value > 0) && !isAllSameCount && ConditionParser.EvalInt(exp.Operator, highestKey, value);
        }

        public static bool LowestKeyCountId(Expression exp)
        {
            if (exp.Operator != OperatorType.Equal && exp.Operator != OperatorType.NotEqual)
                return false;

            var value = exp.Value.ChangeType<int>();

            int[] keyIds = { 364694, 364695, 364696, 364697 };
            int[] keyCounts = { 0, 0, 0, 0 };

            Func<ACDItem, int, bool> isKeyId = (acd, sno) => keyIds.Any(k => k == acd.ActorSNO);

            List<ACDItem> keys = ZetaDia.Me.Inventory.StashItems.Where(isKeyId).Concat(ZetaDia.Me.Inventory.Backpack.Where(isKeyId)).ToList();

            keys.ForEach(key => { keyCounts[Array.IndexOf(keyIds, key.ActorSNO)] += key.ItemStackQuantity; });

            int minCount = keyCounts.Min();
            int lowestKey = keyIds[Array.IndexOf(keyCounts, minCount)];
            var isAllSameCount = keyCounts.All(count => count == keyCounts[0]);

            Logger.Log(string.Format("Key Counts: Act 1 => {0},  Act 2 => {1},  Act 3 => {2}, Act 4 => {3}", keyCounts[0], keyCounts[1], keyCounts[2], keyCounts[3]));

            return (value > 0) && !isAllSameCount && ConditionParser.EvalInt(exp.Operator, lowestKey, value);
        }

        #endregion

        #region Method Conditions

        /// <summary>
        /// Conditions that take paremeters and result in a variable 
        /// that must be compared to another value
        /// </summary>
        public enum MethodConditionType
        {
            Unknown = 0,
            ItemCount,
            GetBackpackItemCount,
            GetStashedItemCount,
            GetStackCount,
        }

        public static bool ItemCount(Expression exp)
        {
            if (exp.Params.ElementAtOrDefault(0) == null)
                return false;

            var itemId = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            List<ACDItem> items = ZetaDia.Me.Inventory.StashItems.Where(item => itemId == item.ActorSNO)
                .Concat(ZetaDia.Me.Inventory.Backpack.Where(item => itemId == item.ActorSNO)).ToList();

            var count = items.Select(i => i.ItemStackQuantity).Aggregate((a, b) => a + b);

            return ConditionParser.EvalInt(exp.Operator, count, exp.Value.ChangeType<int>());
        }

        public static bool GetBackpackItemCount(Expression exp)
        {
            if (exp.Params.ElementAtOrDefault(0) == null)
                return false;

            var itemId = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            return ConditionParser.EvalInt(exp.Operator, Zeta.Bot.ConditionParser.GetBackpackItemCount(itemId), exp.Value.ChangeType<int>());
        }

        public static bool GetStashedItemCount(Expression exp)
        {
            if (exp.Params.ElementAtOrDefault(0) == null)
                return false;

            var itemId = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            return ConditionParser.EvalInt(exp.Operator, Zeta.Bot.ConditionParser.GetStashedItemCount(itemId), exp.Value.ChangeType<int>());
        }

        public static bool GetStackCount(Expression exp)
        {
            if (exp.Params.ElementAtOrDefault(0) == null)
                return false;

            var powerId = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            return ConditionParser.EvalInt(exp.Operator, Zeta.Bot.ConditionParser.GetStackCount(powerId), exp.Value.ChangeType<int>());
        }

        #endregion

        #region Bool Method Conditions

        /// <summary>
        /// Conditions that take 1-n parameters and evaluate to true/false 
        /// without an additional comparison to value.
        /// </summary>
        public enum BoolMethodConditionType
        {
            HasBackpackItem,
            HasStashedItem,
            HasQuest,
            ActorExistsAt,
            MarkerExistsAt,
        }

        public static bool HasBackpackItem(Expression exp)
        {
            string param = exp.Params.ElementAtOrDefault(0);
            return param != null && Zeta.Bot.ConditionParser.HasBackpackItem(param.ChangeType<int>());
        }

        public static bool HasStashedItem(Expression exp)
        {
            string param = exp.Params.ElementAtOrDefault(0);
            return param != null && Zeta.Bot.ConditionParser.HasStashedItem(param.ChangeType<int>());
        }

        public static bool HasQuest(Expression exp)
        {
            string param = exp.Params.ElementAtOrDefault(0);
            return param != null && Zeta.Bot.ConditionParser.HasQuest(param.ChangeType<int>());
        }

        public static bool ActorExistsAt(Expression exp)
        {
            if (exp.Params.Count != 5)
                return false;

            var range = exp.Params.ElementAtOrDefault(4).ChangeType<float>();
            var id = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            var xToken = exp.Params.ElementAtOrDefault(1).ToLowerInvariant();
            var yToken = exp.Params.ElementAtOrDefault(2).ToLowerInvariant();
            var zToken = exp.Params.ElementAtOrDefault(3).ToLowerInvariant();

            var x = (xToken == "me.position.x") ? ZetaDia.Me.Position.X : xToken.ChangeType<float>();
            var y = (yToken == "me.position.y") ? ZetaDia.Me.Position.Y : yToken.ChangeType<float>();
            var z = (zToken == "me.position.z") ? ZetaDia.Me.Position.Z : zToken.ChangeType<float>();

            return Zeta.Bot.ConditionParser.ActorExistsAt(id, x, y, z, range);
        }

        public static bool MarkerExistsAt(Expression exp)
        {
            if (exp.Params.Count != 5)
                return false;

            var range = exp.Params.ElementAtOrDefault(4).ChangeType<float>();
            var id = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            var xToken = exp.Params.ElementAtOrDefault(1).ToLowerInvariant();
            var yToken = exp.Params.ElementAtOrDefault(2).ToLowerInvariant();
            var zToken = exp.Params.ElementAtOrDefault(3).ToLowerInvariant();

            var x = (xToken == "me.position.x") ? ZetaDia.Me.Position.X : xToken.ChangeType<float>();
            var y = (yToken == "me.position.y") ? ZetaDia.Me.Position.Y : yToken.ChangeType<float>();
            var z = (zToken == "me.position.z") ? ZetaDia.Me.Position.Z : zToken.ChangeType<float>();

            return Zeta.Bot.ConditionParser.MarkerExistsAt(id, x, y, z, range);
        }

        #endregion
    }
}
