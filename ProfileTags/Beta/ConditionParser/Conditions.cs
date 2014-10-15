
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Beta.ConditionParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using ScriptManager = Zeta.Common.ScriptManager;

namespace QuestTools.Helpers
{
    public static class Conditions
    {
        /// <summary>
        /// Conditions that use namespace members
        /// </summary>
        #region Namespace Conditions

        [Condition(Type = ExpressionType.Namespace)]
        public static bool ZetaNamespace(Expression exp)
        {
            // Need to add back the single quotes that tokenizer removed... or this will fail:
            // Zeta.Bot.Settings.GlobalSettings.Instance.LastProfile.Contains('Zerg')
            if (exp.Params.Any() && exp.Keyword.ToLowerInvariant().Contains("zeta"))
            {
                for (int i = 0; i < exp.Params.Count; i++)
                {
                    if (!exp.Params[i].StartsWith("'") && !exp.Params[i].EndsWith("'"))
                        exp.Params[i] = "'" + exp.Params[i] + "'";
                }                    
            }

            exp.ParserId = "DBParser";

            return ScriptManager.GetCondition(exp.ToString()).Invoke();
        }

        #endregion

        /// <summary>
        /// Conditions that compare a variable against something
        /// ie. ZetaDia.ActInfo.ActiveBounty != 0
        /// </summary>
        #region  VariableConditions

        [Condition(Type = ExpressionType.Variable)]
        public static bool CurrentWave(Expression exp)
        {
            return ParserUtils.EvalInt(exp.Operator, RiftTrial.CurrentWave, exp.Value.ChangeType<int>());
        }

        [Condition(Type = ExpressionType.Variable)]
        public static bool CurrentSceneName(Expression exp)
        {
            return ParserUtils.EvalString(exp.Operator, ZetaDia.Me.CurrentScene.Name, exp.Value);
        }

        [Condition(Type = ExpressionType.Variable)]
        public static bool Difficulty(Expression exp)
        {
            return ParserUtils.EvalString(exp.Operator, ZetaDia.Service.Hero.CurrentDifficulty.ToString(), exp.Value);
        }

        [Condition(Type = ExpressionType.Variable)]
        public static bool ActorClass(Expression exp)
        {
            return ParserUtils.EvalString(exp.Operator, ZetaDia.Service.Hero.Class.ToString(), exp.Value);
        }

        [Condition(Type = ExpressionType.Variable)]
        public static bool HeroLevel(Expression exp)
        {
            return ParserUtils.EvalInt(exp.Operator, ZetaDia.Service.Hero.Level, exp.Value.ChangeType<int>());
        }

        [Condition(Type = ExpressionType.Variable)]
        public static bool HighestKeyCountId(Expression exp)
        {
            if (exp.Operator != OperatorType.Equal && exp.Operator != OperatorType.NotEqual)
                return false;

            var value = exp.Value.ChangeType<int>();

            return (value > 0) && !Keys.IsAllSameCount && ParserUtils.EvalInt(exp.Operator, Keys.LowestKeyId, value);
        }

        [Condition(Type = ExpressionType.Variable)]
        public static bool LowestKeyCountId(Expression exp)
        {
            if (exp.Operator != OperatorType.Equal && exp.Operator != OperatorType.NotEqual)
                return false;

            var value = exp.Value.ChangeType<int>();

            return (value > 0) && !Keys.IsAllSameCount && ParserUtils.EvalInt(exp.Operator, Keys.LowestKeyId, value);
        }

        #endregion

        /// <summary>
        /// Conditions that take no parameters, have no comparison and evaluate to true/false
        /// </summary>
        #region BoolVariable Conditions

        [Condition(Type = ExpressionType.BoolVariable)]
        public static bool IsInGame(Expression exp)
        {
            return ZetaDia.IsInGame;
        }

        #endregion

        /// <summary>
        /// Conditions that take paremeters and result in a variable, must be compared to another value
        /// ItemCount(23423) >= 20
        /// </summary>
        #region Method Conditions

        [Condition(Type = ExpressionType.Method)]
        public static bool ItemCount(Expression exp)
        {
            if (exp.Params.ElementAtOrDefault(0) == null)
                return false;

            var itemId = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            List<ACDItem> items = ZetaDia.Me.Inventory.StashItems.Where(item => itemId == item.ActorSNO)
                .Concat(ZetaDia.Me.Inventory.Backpack.Where(item => itemId == item.ActorSNO)).ToList();

            var count = items.Select(i => i.ItemStackQuantity).Aggregate((a, b) => a + b);

            return ParserUtils.EvalInt(exp.Operator, count, exp.Value.ChangeType<int>());
        }

        [Condition(Type = ExpressionType.Method)]
        public static bool ActorAnimationCount(Expression exp)
        {
            if (exp.Params.Count != 2 && exp.Params.ElementAtOrDefault(0) == null || exp.Params.ElementAtOrDefault(1) == null)
                return false;

            var actorId = exp.Params.ElementAtOrDefault(0).ChangeType<int>();
            var animationName = exp.Params.ElementAtOrDefault(1);

            var animationCount = ActorHistory.GetActorAnimationCount(actorId, animationName);

            Logger.Log("ActorId={0} Animation={1} Count={2}", actorId, animationName, animationCount);

            var result = ParserUtils.EvalInt(exp.Operator, animationCount, exp.Value.ChangeType<int>());

            if (result)
            {
                ActorHistory.UnitsWithAnimationTracking.Remove(actorId);
            }

            return result;
        }

        [Condition(Type = ExpressionType.Method)]
        public static bool ProfileSetting(Expression exp)
        {
            if (exp.Params.Count != 1 && exp.Params.ElementAtOrDefault(0) == null && !String.IsNullOrEmpty(exp.Value))
                return false;

            var settingName = exp.Params.ElementAt(0);

            string value;

            if (ProfileSettingTag.ProfileSettings.TryGetValue(settingName, out value))
            {
                //Logger.Log("Setting Condition={0} {1} {2} CurrentValue={3}", settingName, exp.Operator, exp.Value, value);
                return ParserUtils.EvalString(exp.Operator, exp.Value, value);
            }            

            return false;
        }

        #endregion

        /// <summary>
        /// Conditions that take 1-n parameters and evaluate to true/false 
        /// ie. HasBackpackItem(54564)
        /// </summary>
        #region BoolMethodConditions

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool ActorIsAlive(Expression exp)
        {
            string param = exp.Params.ElementAtOrDefault(0);
            
            if(param == null || String.IsNullOrEmpty(param))
                return false;

            var actorId = param.ChangeType<int>();

            var actor = ZetaDia.Actors.GetActorsOfType<DiaUnit>().FirstOrDefault(a => a.IsValid && a.ActorSNO == actorId && a.IsAlive);

            // Its possible that by the time other tags run this actor will have disappeared from actors collection
            // to make sure we dont lose track of it, it needs to be recorded in the history.
            ActorHistory.UpdateActor(actor);

            return actor != null;
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool ActorFound(Expression exp)
        {
            if (!exp.Params.Any())
                return false;

            return ActorHistory.HasBeenSeen(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) || ActorIsAlive(exp);
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool ActorExistsNearMe(Expression exp)
        {
            if (exp.Params.Count != 2 || exp.Params.ElementAtOrDefault(0) == null || exp.Params.ElementAtOrDefault(1) == null)
                return false;

            var range = exp.Params.ElementAtOrDefault(1).ChangeType<float>();
            var id = exp.Params.ElementAtOrDefault(0).ChangeType<int>();

            return Zeta.Bot.ConditionParser.ActorExistsAt(id, ZetaDia.Me.Position.X, ZetaDia.Me.Position.Y, ZetaDia.Me.Position.Z, range);
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool KeyAboveMedian(Expression exp)
        {
            if (!ParserUtils.IsValidParams(exp.Params, 1))
                return false;

            return Keys.GetKeyCount(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) > Keys.Median;
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool KeyBelowMedian(Expression exp)
        {
            if (!ParserUtils.IsValidParams(exp.Params, 1))
                return false;

            return Keys.GetKeyCount(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) < Keys.Median;
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool KeyAboveUpperFence(Expression exp)
        {
            if (!ParserUtils.IsValidParams(exp.Params, 1))
                return false;

            return Keys.GetKeyCount(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) > Keys.UpperFence;
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool KeyBelowLowerFence(Expression exp)
        {
            if (!ParserUtils.IsValidParams(exp.Params, 1))
                return false;

            return Keys.GetKeyCount(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) < Keys.LowerFence;
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool KeyAboveUpperQuartile(Expression exp)
        {
            if (!ParserUtils.IsValidParams(exp.Params, 1))
                return false;

            return Keys.GetKeyCount(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) > Keys.UpperQuartile;
        }

        [Condition(Type = ExpressionType.BoolMethod)]
        public static bool KeyBelowLowerQuartile(Expression exp)
        {
            if (!ParserUtils.IsValidParams(exp.Params, 1))
                return false;

            return Keys.GetKeyCount(exp.Params.ElementAtOrDefault(0).ChangeType<int>()) < Keys.LowerQuartile;
        }

        #endregion

        #region Miscellaneous Conditions

        [Condition(Type = ExpressionType.Boolean)]
        public static bool GetBoolean(Expression exp)
        {
            bool result;
            Boolean.TryParse(exp.Value, out result);
            return result;
        }             

        #endregion
    }
}
