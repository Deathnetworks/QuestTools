using System;
using Zeta.Bot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("QuestToolsSetVariable")]
    public class QuestToolsVariableTag : ProfileBehavior
    {
        private bool isDone = false;
        public override bool IsDone { get { return !IsActiveQuestStep || isDone; } }

        [XmlAttribute("key")]
        public string Key { get; set; }
        [XmlAttribute("value")]
        public string Value { get; set; }

        public enum Keys
        {
            ReloadProfileOnDeath = 0,
            DebugLogging = 1,
        }

        public override void OnStart()
        {
            Logger.Log("QuestToolsSetVariable tag started, key={0} value={1}", this.Key, this.Value);
        }

        protected override Composite CreateBehavior()
        {
            return 
            new PrioritySelector(
                new Decorator(ret => SafeCompareKey(Key, Keys.ReloadProfileOnDeath),
                    new Sequence(
                        new Action(ret => QuestTools.ReloadProfileOnDeath = Boolean.Parse(Value)),
                        new Action(ret => Logger.Log("Reloading Profile on Death set to {0}", QuestTools.ReloadProfileOnDeath)),
                        new Action(ret => isDone = true)
                    )
                ),
                new Decorator(ret => SafeCompareKey(Key, Keys.DebugLogging),
                    new Sequence(
                        new Action(ret => QuestTools.EnableDebugLogging = Boolean.Parse(Value)),
                        new Action(ret => Logger.Log("Debug Logging set to {0}", QuestTools.EnableDebugLogging)),
                        new Action(ret => isDone = true)
                    )
                ),
                new Action(ret => Logger.Log("WARNING: No variable set, key {0} not found", Key)),
                new Action(ret => isDone = true)
            );
        }

        public bool SafeCompareKey(string input, Keys key)
        {
            bool result = input.ToUpper().Trim() == key.ToString().ToUpper().Trim();
            //Logging.Write("[QuestTools] Comparing Keys {0} and {1}, result is {2}", input, key.ToString(), result);
            return result;
        }

        public bool SafeCompareString(string string1, string string2)
        {
            
            bool result = string1.ToUpper().Trim() == string2.ToUpper().Trim();
            //Logging.Write("[QuestTools] Comparing Keys {0} and {1}, result is {2}", string1, string2.ToString(), result);
            return result;
        }

        public override void ResetCachedDone()
        {
            isDone = false;
            base.ResetCachedDone();
        }
    }
}
