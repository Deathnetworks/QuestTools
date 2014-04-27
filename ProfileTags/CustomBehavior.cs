using System;
using System.Diagnostics;
using System.Linq;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("CustomBehavior")]
    class CustomBehavior : ProfileBehavior
    {
        private bool isDone = false;
        /// <summary>
        /// Setting this to true will cause the Tree Walker to continue to the next profile tag
        /// </summary>
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || isDone; }
        }

        /// <summary>
        /// Initializes this instance of CustomBehavior
        /// </summary>
        /// <returns></returns>
        private bool init()
        {
            Navigator.Clear();

            Logger.Log("CustomBehavior Initialized for questId=\"{0}\" stepId=\"{1}\"", QuestId, StepId);

            return true;
        }
        private bool initDone = false;

        private DiaObject a2dun_Aqd_Jeweler_Altar_Empty = null;
        private Vector3 a2_57335_44_v0 = Vector3.Zero;
        private Vector3 a2_57335_44_v1 = Vector3.Zero;
        private Vector3 a2_57335_44_v2 = Vector3.Zero;
        private bool a2_57335_44_v1_done = false;
        private bool a2_57335_44_v2_done = false;

        // A1 - The Broken Blade - Navigation stuck Transition from Crypt of the Ancients to Warriors Rest
        private DiaObject a1_72738_12_portal = null;
        private Vector3 a1_72738_12_v0 = Vector3.Zero;
        private Vector3 a1_72738_12_v1 = Vector3.Zero;
        private Vector3 a1_72738_12_v2 = Vector3.Zero;
        private bool a1_72738_12_v1_done = false;
        private bool a1_72738_12_v2_done = false;

        [XmlAttribute("printInfo")]
        public bool PrintInfo { get; set; }

        /// <summary>
        /// Main SafeMoveTo behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            if (!initDone)
                initDone = init();
            try
            {
                return new PrioritySelector(
                    new Decorator(ret => PrintInfo,
                        new Sequence(
                            new Action(ret => PrintInfoToLog()),
                            new Action(ret => isDone = true)
                        )
                    ),
                    new Sequence(
                        new Action(ret => CheckSetTimer(ret)),
                        new Decorator(ret => isValid(),
                            new PrioritySelector(
                                A1_Q72738_S12(),
                                A2_Q57335_S44(),
                                new Sequence(
                                    new Action(ret => isDone = true),
                                    new Action(ret => Logger.Debug("Unknown Quest {0} Step {1} World {2}", QuestId, StepId, ZetaDia.CurrentWorldId))
                                )
                            )
                        )
                    )
                );

            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                return new Action(ret => isDone = true);
            }
        }

        private void PrintInfoToLog()
        {
            string info = string.Format("questId=\"{0}\" stepId=\"{1}\" {2} CurrentWorldId=={3} and CurrentLevelAreaId=={4}",
                ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, QuestTools.GetProfilePosition(ZetaDia.Me.Position), ZetaDia.CurrentWorldId, ZetaDia.CurrentLevelAreaId);
            Logger.Log(info);
        }

        private Stopwatch timer = new Stopwatch();
        private int timeoutSeconds = 60;
        private RunStatus CheckSetTimer(object ctx)
        {
            if (!timer.IsRunning)
            {
                timer.Start();
                return RunStatus.Failure;
            }
            else if (timer.IsRunning && timer.Elapsed.TotalSeconds > timeoutSeconds)
            {
                Logger.Log("Timeout exceeded for CustomBehavior questId=\"{0}\" stepId=\"{1}\"", QuestId, StepId);
                isDone = true;
                return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        private Decorator DecoratorIDCheck()
        {
            return
                new Decorator(ret => !QuestAndStepMatch(),
                    new Action(delegate
                    {
                        Logger.Log("Invalid questId=\"{0}\" stepId=\"{1}\" x=\"{2}\" y=\"{3}\" z=\"{4}\" actorId=\"{5}\" ", QuestId, StepId);
                        isDone = true;
                    }
                    )
                );
        }

        private bool QuestAndStepMatch()
        {
            return QuestId == ZetaDia.CurrentQuest.QuestSNO && StepId == ZetaDia.CurrentQuest.StepId;
        }

        private Decorator A1_Q72738_S12()
        {
            return new Decorator(ret => QuestId == 72738 && StepId == 12 && ZetaDia.CurrentWorldId == 71150,
                new Sequence(
                    new Action(delegate
                    {
                        A1_Q72738_S12_init();

                        return RunStatus.Success;
                    }
                    ),
                    new PrioritySelector(
                        new Decorator(ret => !a1_72738_12_v1_done,
                            new Action(delegate
                            {
                                if (ZetaDia.Me.Position.Distance(a1_72738_12_v1) > 5)
                                {
                                    Navigator.PlayerMover.MoveTowards(a1_72738_12_v1);
                                }
                                else
                                {
                                    a1_72738_12_v1_done = true;
                                }
                            }
                            )
                        ),
                        new Decorator(ret => !a1_72738_12_v2_done,
                            new Action(delegate
                            {
                                if (ZetaDia.Me.Position.Distance(a1_72738_12_v2) > 5)
                                {
                                    Navigator.PlayerMover.MoveTowards(a1_72738_12_v2);
                                }
                                else
                                {
                                    a1_72738_12_v2_done = true;
                                }
                            }
                            )
                        ),
                        new Action(delegate
                            {
                                isDone = true;
                                Logger.Debug("Quest {0} Step {1} custom behavior finished!", QuestId, StepId);
                            }
                        )
                    )
                )
            );
        }

        private void A1_Q72738_S12_init()
        {
            /*
           * Gizmo ActorSNO: 176008 Name: g_Portal_Square_Orange-3754 Type: Portal Position: x="308" y="881" z="17"  
           * x="352" y="881" z="20" == +44, 0, +3
           * x="356" y="838" z="20" == +48, -43, +3
           */
            if (a1_72738_12_portal == null || !a1_72738_12_portal.IsValid)
            {
                a1_72738_12_portal = ZetaDia.Actors.GetActorsOfType<GizmoPortal>(true, false)
                    .Where(o => o.ActorSNO == 176008).OrderBy(o => o.Distance).FirstOrDefault();
            }

            if (a1_72738_12_portal != null && a1_72738_12_v0 == Vector3.Zero)
            {
                a1_72738_12_v0 = a1_72738_12_portal.Position;
            }

            if (a1_72738_12_portal != null && a1_72738_12_v1 == Vector3.Zero)
            {
                a1_72738_12_v1 = new Vector3(a1_72738_12_v0.X + 44, a1_72738_12_v0.Y + 0, a1_72738_12_v0.Z + 3);
            }

            if (a1_72738_12_portal != null && a1_72738_12_v2 == Vector3.Zero)
            {
                a1_72738_12_v2 = new Vector3(a1_72738_12_v0.X + 48, a1_72738_12_v0.Y - 43, a1_72738_12_v0.Z + 3);
            }
        }


        private Decorator A2_Q57335_S44()
        {
            /* Act 2, Quest 57335, Step 44
            * [13:36:16.312 D] [QuestTools] Object ActorSNO: 213704 Name: a2dun_Aqd_Jeweler_Altar_Empty-59135 Type: None 
            * Position: x="261" y="520" z="-1"  (261, 520, -1,) Animation: Invalid 
            * has Attributes: TeamID=10, ScreenAttackRadiusConstant=1.114636E+09, TurnRateScalar=1.065353E+09, TurnAccelScalar=1.065353E+09, TurnDeccelScalar=1.065353E+09, 
            * UnequippedTime=1, CoreAttributesFromItemBonusMultiplier=1.065353E+09, 
            */
            /*
                Altar location:
                261, 520, -1 (0, 0, 0)

                Stuck at:
                270, 520, -1 (-9, 0, 0)

                Correct location:
                266, 534, -1 (-5, -14, 0)

                Final Destination:
                161, 529, 0 (100, -9, 0)
             */
            return new Decorator(ret => QuestId == 57335 && StepId == 44 && ZetaDia.CurrentWorldId == 192640,
                new Sequence(
                    new Action(delegate
                    {
                        A2_Q57335_S44_init();

                        return RunStatus.Success;
                    }
                    ),
                    new PrioritySelector(
                        new Decorator(ret => !a2_57335_44_v1_done,
                            new Action(delegate
                            {
                                if (ZetaDia.Me.Position.Distance(a2_57335_44_v1) > 5)
                                {
                                    Navigator.PlayerMover.MoveTowards(a2_57335_44_v1);
                                }
                                else
                                {
                                    a2_57335_44_v1_done = true;
                                }
                            }
                            )
                        ),
                        new Decorator(ret => !a2_57335_44_v2_done,
                            new Action(delegate
                            {
                                if (ZetaDia.Me.Position.Distance(a2_57335_44_v2) > 5)
                                {
                                    Navigator.PlayerMover.MoveTowards(a2_57335_44_v2);
                                }
                                else
                                {
                                    a2_57335_44_v2_done = true;
                                }
                            }
                            )
                        ),
                        new Action(delegate
                            {
                                isDone = true;
                                Logger.Debug("Quest {0} Step {1} custom behavior finished!", QuestId, StepId);
                            }
                        )
                    )
                )
            );
        }

        private void A2_Q57335_S44_init()
        {
            if (a2dun_Aqd_Jeweler_Altar_Empty == null || !a2dun_Aqd_Jeweler_Altar_Empty.IsValid)
            {
                a2dun_Aqd_Jeweler_Altar_Empty = ZetaDia.Actors.GetActorsOfType<DiaObject>(false, false)
                    .Where(o => o.ActorSNO == 213704).FirstOrDefault();
            }

            if (a2dun_Aqd_Jeweler_Altar_Empty != null && a2_57335_44_v0 == Vector3.Zero)
            {
                a2_57335_44_v0 = a2dun_Aqd_Jeweler_Altar_Empty.Position;
            }

            if (a2dun_Aqd_Jeweler_Altar_Empty != null && a2_57335_44_v1 == Vector3.Zero)
            {
                a2_57335_44_v1 = new Vector3(a2_57335_44_v0.X + 5, a2_57335_44_v0.Y + 14, a2_57335_44_v0.Z);
            }

            if (a2dun_Aqd_Jeweler_Altar_Empty != null && a2_57335_44_v2 == Vector3.Zero)
            {
                a2_57335_44_v2 = new Vector3(a2_57335_44_v0.X - 100, a2_57335_44_v0.Y + 9, a2_57335_44_v0.Z);
            }
        }


        /// <summary>
        /// Are we still in game and ready to go?
        /// </summary>
        /// <returns></returns>
        private bool isValid()
        {
            return ZetaDia.Me.IsValid &&
                ZetaDia.IsInGame &&
                !ZetaDia.IsLoadingWorld &&
                !ZetaDia.Me.IsDead;
        }

        public override void ResetCachedDone()
        {
            a2_57335_44_v1_done = false;
            a2_57335_44_v2_done = false;

            isDone = false;
            base.ResetCachedDone();
        }
        public override void OnDone()
        {
            isDone = true;
            base.OnDone();
        }
    }
}
