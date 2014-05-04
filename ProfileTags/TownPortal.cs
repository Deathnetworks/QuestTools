using System.Diagnostics;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    // TrinityTownRun forces a town-run request
    [XmlElement("TrinityTownPortal")]
    [XmlElement("TownPortal")]
    public class TownPortalTag : ProfileBehavior
    {
        public static int DefaultWaitTime = -1;

        [XmlAttribute("waitTime")]
        [XmlAttribute("wait")]
        public int WaitTime { get; set; }

        public static Stopwatch AreaClearTimer = null;
        public static Stopwatch PortalCastTimer = null;
        public static bool ForceClearArea = false;

        private double _startHealth = -1;

        private bool _isDone;

        public override bool IsDone
        {
            get { return _isDone || !IsActiveQuestStep; }
        }

        public TownPortalTag()
        {
            AreaClearTimer = new Stopwatch();
            PortalCastTimer = new Stopwatch();
        }

        public override void OnStart()
        {
            if (ZetaDia.IsInTown)
            {
                _isDone = true;
                return;
            }

            ForceClearArea = true;
            AreaClearTimer.Reset();
            AreaClearTimer.Start();
            DefaultWaitTime = 2500;
            if (WaitTime <= 0)
            {
                WaitTime = DefaultWaitTime;
            }
            _startHealth = ZetaDia.Me.HitpointsCurrent;
            Logger.Log("TownPortal started - clearing area, waitTime={0}, startHealth={1:0}", WaitTime, _startHealth);
        }

        protected override Composite CreateBehavior()
        {
            return new
            PrioritySelector(
                new Decorator(ret => ZetaDia.IsLoadingWorld,
                    new Action()
                ),
                new Decorator(ret => ZetaDia.IsInTown && !DataDictionary.ForceTownPortalLevelAreaIds.Contains(Player.LevelAreaId),
                    new Action(ret =>
                    {
                        ForceClearArea = false;
                        AreaClearTimer.Reset();
                        _isDone = true;
                    })
                ),
                new Decorator(ret => !ZetaDia.IsInTown && !ZetaDia.Me.CanUseTownPortal(),
                    new Action(ret =>
                    {
                        ForceClearArea = false;
                        AreaClearTimer.Reset();
                        _isDone = true;
                    })
                ),
                new Decorator(ret => ZetaDia.Me.HitpointsCurrent < _startHealth,
                    new Action(ret =>
                    {
                        _startHealth = ZetaDia.Me.HitpointsCurrent;
                        AreaClearTimer.Restart();
                        ForceClearArea = true;
                    })
                ),
                new Decorator(ret => AreaClearTimer.IsRunning,
                    new PrioritySelector(
                        new Decorator(ret => AreaClearTimer.ElapsedMilliseconds <= WaitTime,
                            new Action(ret => ForceClearArea = true) // returns RunStatus.Success
                        ),
                        new Decorator(ret => AreaClearTimer.ElapsedMilliseconds > WaitTime,
                            new Action(ret =>
                            {
                                Logger.Log("Town Portal timer finished");
                                ForceClearArea = false;
                                AreaClearTimer.Reset();
                            })
                        )
                    )
                ),

                new Decorator(ret => !ForceClearArea,
                    new PrioritySelector(

                        new Decorator(ret => ZetaDia.Me.Movement.IsMoving,
                            new Sequence(
                                CommonBehaviors.MoveStop(),
                                new Sleep(1000)
                            )
                        ),

                        new Decorator(ret => PortalCastTimer.IsRunning && PortalCastTimer.ElapsedMilliseconds >= 7000,
                            new Sequence(
                                new Action(ret =>
                                {
                                    Logger.Log("Stuck casting town portal, moving a little");
                                    Navigator.MoveTo(Navigator.StuckHandler.GetUnstuckPos(), "Unstuck Position");
                                    PortalCastTimer.Reset();
                                })
                            )
                        ),


                        new Decorator(ret => PortalCastTimer.IsRunning && ZetaDia.Me.LoopingAnimationEndTime > 0, // Already casting, just wait
                            new Action(ret => RunStatus.Success)
                        ),

                        new Sequence(
                            new Action(ret =>
                            {
                                PortalCastTimer.Restart();
                                GameEvents.FireWorldTransferStart();
                                ZetaDia.Me.UseTownPortal();
                            }),

                            new WaitContinue(3, ret => ZetaDia.Me != null && ZetaDia.Me.LoopingAnimationEndTime > 0,
                                new Sleep(100)
                            )
                        )
                    )
                )
            );
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}

