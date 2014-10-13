using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Complex
{
    
    /// <summary>
    /// Reorders child tags, useful for split-farming profiles with multiple bots 
    /// For example bounties or keys - each bot would start a different bounty
    /// </summary>
    [XmlElement("Shuffle")]
    public class ShuffleTag : BaseComplexNodeTag, IAsyncProfileBehavior
    {
        [XmlAttribute("order")]
        public OrderType Order { get; set; }

        public enum OrderType
        {
            Random = 0,
            Reverse
        }

        private bool _shuffled;
        private bool _isDone;
        public override bool IsDone
        {
            get
            {                
                var done = _isDone || QuestId > 0 && !IsActiveQuestStep;

                if(!_shuffled)
                    Shuffle();

                return done;
            }
        }

        public override bool GetConditionExec()
        {
            return false;
        }

        public void Shuffle()
        {
            Logger.Log("{0} Shuffling {1} tags", Order, Body.Count);

            switch (Order)
            {
                case OrderType.Reverse:

                    Body.Reverse();
                    break;

                default:

                    RandomShuffle(Body);
                    break;
            }

            _shuffled = true;
        }
            
        public static void RandomShuffle<T>(IList<T> list)
        {
            var rng = new Random();
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public override void ResetCachedDone()
        {
            _shuffled = false;
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IAsyncProfileBehavior

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }
}

