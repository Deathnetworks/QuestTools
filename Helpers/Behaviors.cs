using Zeta.TreeSharp;

namespace QuestTools.Helpers
{
    public static class Behaviors
    {
        public delegate bool IsDoneCondition(object ret);
        public delegate Composite CreateBehavior(object ret);

        //Condition Failure => return Success
        //Behavior Failure => return Success
        //Behavior Success => return Success
        public static Composite ExecuteReturnAlwaysSuccess(IsDoneCondition condition, CreateBehavior behavior)
        {
            return
            new DecoratorContinue(ret => condition.Invoke(null),
                new PrioritySelector(
                    behavior.Invoke(null),
                    new Zeta.TreeSharp.Action(ret => RunStatus.Success)
                )
            );
        }

        //Condition Failure => return Failure
        //Behavior Failure => return Failure
        //Behavior Success => return Success
        public static Composite ExecuteReturnFailureOrBehaviorResult(IsDoneCondition condition, CreateBehavior behavior)
        {
            return new Decorator(ret => condition.Invoke(null), behavior.Invoke(null));
        }

        //Condition Failure => return Success
        //Behavior Failure => return Failure
        //Behavior Success =>return Success
        public static Composite ExecuteReturnSuccessOrBehaviorResult(IsDoneCondition condition, CreateBehavior behavior)
        {
            return new DecoratorContinue(ret => condition.Invoke(null), behavior.Invoke(null));
        }


    }
}
