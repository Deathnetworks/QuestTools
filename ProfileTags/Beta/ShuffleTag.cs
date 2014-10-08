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
    public class ShuffleTag : NewBaseComplexNodeTag
    {
        [XmlAttribute("order")]
        public OrderType Order { get; set; }

        public enum OrderType
        {
            Random = 0,
            Reverse
        }

        public override bool GetConditionExec()
        {
            var i = 0;
            var nodes = GetNodes().ToList();

            Logger.Log("{0} Shuffling {1} tags", Order, Body.Count);

            switch (Order)
            {
                case OrderType.Reverse:

                    Body.Reverse();
                    break;

                default:

                    Shuffle(Body);
                    break;
            }

            return true;
        }

        public static void Shuffle<T>(IList<T> list)
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

    }
}

