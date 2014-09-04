using System;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using QuestTools.Helpers;
using QuestTools.Navigation;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.Game.Internals.SNO;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Movement
{
    [XmlElement("MoveToActor")]
    class MoveToActor : ProfileBehavior
    {
        public MoveToActor() { }

        private bool _done = false;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _done; }
        }

        [XmlAttribute("x")]
        public float X { get; set; }

        [XmlAttribute("y")]
        public float Y { get; set; }

        [XmlAttribute("z")]
        public float Z { get; set; }

        public Vector3 Position
        {
            get { return new Vector3(X, Y, Z); }
            set { X = value.X; Y = value.Y; Z = value.Z; }
        }

        /// <summary>
        /// Defines how close we need to be to the actor in order to interact with it. Default=10
        /// </summary>
        [XmlAttribute("interactRange")]
        public int InteractRange { get; set; }

        [XmlAttribute("straightLinePathing")]
        public bool StraightLinePathing { get; set; }

        [XmlAttribute("useNavigator")]
        public bool UseNavigator { get; set; }

        /// <summary>
        /// The ActorSNO of the object you're looking for - optional
        /// </summary>
        [XmlAttribute("actorId")]
        public int ActorId { get; set; }

        /// <summary>
        /// The number of interact attempts before giving up. Default=5
        /// </summary>
        [XmlAttribute("interactAttempts")]
        public int InteractAttempts { get; set; }

        /// <summary>
        /// The "safe" distance that we will request a dynamic nav point to. We will never actually reach this nav point as it's always going to be <see cref="pathPointLimit"/> away.
        /// If the target is closer than this distance, we will just move to the target.
        /// </summary>
        [XmlAttribute("pathPointLimit")]
        public int pathPointLimit { get; set; }

        /// <summary>
        /// Boolean defining special portal handling
        /// </summary>
        [XmlAttribute("isPortal")]
        public bool IsPortal { get; set; }

        /// <summary>
        /// Required if using IsPortal
        /// </summary>
        [XmlAttribute("destinationWorldId")]
        public int DestinationWorldId { get; set; }

        /// <summary>
        /// When searching for An ActorID at Position, what's the maximum distance from Position that will result in a valid Actor?
        /// </summary>
        [XmlAttribute("maxSearchDistance")]
        public int MaxSearchDistance { get; set; }

        /// <summary>
        /// This is the longest time this behavior can run for. Default is 180 seconds (3 minutes).
        /// </summary>
        [XmlAttribute("timeout")]
        public int Timeout { get; set; }

        /// <summary>
        /// If the given actor has an animation that is matching this, the behavior will end
        /// </summary>
        [XmlAttribute("endAnimation")]
        public string EndAnimation { get; set; }

        /// <summary>
        /// Finishes the tag when the Interactive Conversation button appears
        /// </summary>
        [XmlAttribute("exitWithConversation")]
        public bool ExitWithConversation { get; set; }

        // Special configuration if you want to tweak things:
        private bool verbose = false;
        private int interactWaitSeconds = 1;

        private int completedInteractions = 0;
        private int startingWorldId = 0;
        private DateTime lastInteract = DateTime.MinValue;
        private DateTime lastPositionUpdate = DateTime.UtcNow;
        private DateTime tagStartTime = DateTime.MinValue;
        private DiaObject actor = null;
        private Vector3 startInteractPosition = Vector3.Zero;
        private Vector3 lastPosition = Vector3.Zero;
        private QTNavigator QTNavigator = new QTNavigator();
        private MoveResult moveResult = MoveResult.Moved;
        private SNOAnim endAnimation = SNOAnim.Invalid;

        public override void OnStart()
        {
            if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || !ZetaDia.Me.IsValid)
                return;

            if (InteractRange == 0)
                InteractRange = 10;
            if (InteractAttempts == 0)
                InteractAttempts = 5;
            if (pathPointLimit == 0)
                pathPointLimit = 250;
            if (Timeout == 0)
                Timeout = 180;


            startingWorldId = ZetaDia.CurrentWorldId;
            tagStartTime = DateTime.UtcNow;

            if (!String.IsNullOrEmpty(EndAnimation))
            {
                try
                {
                    Enum.TryParse<Zeta.Game.SNOAnim>(EndAnimation, out endAnimation);
                }
                catch
                {
                    endAnimation = SNOAnim.Invalid;
                }
            }

            Navigator.Clear();

            verbose = true;

            completedInteractions = 0;
            startingWorldId = 0;
            lastInteract = DateTime.MinValue;
            actor = null;
            startInteractPosition = Vector3.Zero;
            lastPosition = ZetaDia.Me.Position;
            lastPositionUpdate = DateTime.UtcNow;

            Logger.Debug("Initialized {0}", Status());
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => MainCoroutine());
        }


        private async Task<bool> MainCoroutine()
        {
            if (ZetaDia.Me.IsDead)
                return false;

            if (!ZetaDia.Me.IsValid)
                return false;

            if (ZetaDia.IsLoadingWorld)
                return false;

            if (ZetaDia.IsLoadingWorld)
                return false;

            if (DateTime.UtcNow.Subtract(lastInteract).TotalMilliseconds < 500 && WorldHasChanged())
                return true;

            if (DateTime.UtcNow.Subtract(tagStartTime).TotalSeconds > Timeout)
            {
                End("Timeout of {0} seconds exceeded for Profile Behavior {1}", Timeout, Status());
                return true;
            }

            if (DungeonStonesPresent && GameUI.IsElementVisible(GameUI.GenericOK))
            {
                GameUI.SafeClickElement(GameUI.GenericOK);
                await Coroutine.Yield();
                await Coroutine.Sleep(3000);
            }

            GameUI.SafeClickUIButtons();

            SafeUpdateActor();

            if (Vector3.Distance(lastPosition, ZetaDia.Me.Position) > 5f)
            {
                lastPositionUpdate = DateTime.UtcNow;
                lastPosition = ZetaDia.Me.Position;
            }

            if ((actor == null || !actor.IsValid) && Position == Vector3.Zero && !WorldHasChanged())
            {
                EndDebug("ERROR: Could not find an actor or position to move to, finished! {0}", Status());
                return true;
            }
            if (IsPortal && WorldHasChanged())
            {
                if (DestinationWorldId > 0 && ZetaDia.CurrentWorldId != DestinationWorldId && ZetaDia.CurrentWorldId != startingWorldId)
                {
                    EndDebug("Error! We used a portal intending to go from WorldId={0} to WorldId={1} but ended up in WorldId={2} {3}",
                                                    startingWorldId, DestinationWorldId, ZetaDia.CurrentWorldId, Status());
                    return true;
                }
                EndDebug("Successfully used portal {0} to WorldId {1} {2}", ActorId, ZetaDia.CurrentWorldId, Status());
                return true;
            }
            if ((actor == null || !actor.IsValid) && ((MaxSearchDistance > 0 && WithinMaxSearchDistance()) || WithinInteractRange()))
            {
                EndDebug("Finished: Actor {0} not found, within InteractRange {1} and  MaxSearchDistance {2} of Position {3} {4}",
                                            ActorId, InteractRange, MaxSearchDistance, Position, Status());
                return true;
            }
            if (Position.Distance(ZetaDia.Me.Position) > 1500)
            {
                EndDebug("ERROR: Position distance is {0} - this is too far! {1}", Position.Distance(ZetaDia.Me.Position), Status());
                return true;
            }
            if (actor == null || !actor.IsValid)
            {
                if (MaxSearchDistance > 0 && !WithinMaxSearchDistance())
                {
                    Move(Position);
                    return true;
                }
                if (InteractRange > 0 && !WithinInteractRange())
                {
                    Move(Position);
                    return true;
                }
            }

            if (((!IsPortal && completedInteractions >= InteractAttempts && InteractAttempts > 0) || (IsPortal && WorldHasChanged()) || AnimationMatch()))
            {
                EndDebug("Successfully interacted with Actor {0} at Position {1}", actor.ActorSNO, actor.Position);
                return true;
            }
            if (InteractAttempts <= 0 && WithinInteractRange())
            {
                EndDebug("Actor is within interact range {0:0} - no interact attempts", actor.Distance);
            }
            if (completedInteractions >= InteractAttempts)
            {
                EndDebug("Interaction failed after {0} interact attempts", completedInteractions);
            }
            if (ExitWithConversation && GameUI.IsElementVisible(GameUI.TalktoInteractButton1))
            {
                GameUI.SafeClickElement(GameUI.TalktoInteractButton1, "Conversation Interaction Button 1");
                EndDebug("Clicked Conversation Interaction Button 1");
            }
            if (moveResult == MoveResult.ReachedDestination && actor == null)
            {
                EndDebug("Reached Destination, no actor found!");
            }
            if (!WithinInteractRange())
            {
                Move(actor.Position);
                return true;
            }

            bool doInteract = await Coroutine.Wait(interactWaitSeconds, ShouldWaitForInteraction);
            if (doInteract)
            {
                if ((WithinInteractRange() || DateTime.UtcNow.Subtract(lastPositionUpdate).TotalMilliseconds > 750) && completedInteractions < InteractAttempts)
                {
                    return await InteractRoutine();
                }
            }

            Logger.Debug("No action taken");
            return true;
        }

        private bool DungeonStonesPresent
        {
            get
            {
                return
                ZetaDia.Actors.GetActorsOfType<DiaGizmo>(true)
                    .Any(o =>
                        o.IsValid &&
                        o.ActorInfo.GizmoType == GizmoType.ReturnPortal &&
                        o.Position.Distance2D(ZetaDia.Me.Position) <= 30f);
            }
        }



        /// <summary>
        /// Will only update actor if found (useful for some portals which tend to disappear when you stand next to them)
        /// </summary>
        private void SafeUpdateActor()
        {
            DiaObject newActor = null;
            DiaUnit newUnit = null;

            // Find closest actor if we have a position and MaxSearchDistance (only actors within radius MaxSearchDistance from Position)
            if (Position != Vector3.Zero && MaxSearchDistance > 0)
            {
                newActor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                    .Where(o => o.ActorSNO == ActorId && o.Position.Distance(Position) <= MaxSearchDistance)
                    .OrderBy(o => !(o is DiaUnit && ((DiaUnit)o).IsQuestGiver))
                    .OrderBy(o => Position.Distance(o.Position)).FirstOrDefault();
            }
            // Otherwise just OrderBy distance from Position (any actor found)
            else if (Position != Vector3.Zero)
            {
                newActor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                   .Where(o => o.ActorSNO == ActorId)
                   .OrderBy(o => !(o is DiaUnit && ((DiaUnit)o).IsQuestGiver))
                   .OrderBy(o => Position.Distance(o.Position)).FirstOrDefault();
            }
            // If all else fails, get first matching Actor closest to Player
            else
            {
                newActor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                   .Where(o => o.ActorSNO == ActorId)
                   .OrderBy(o => !(o is DiaUnit && ((DiaUnit)o).IsQuestGiver))
                   .OrderBy(o => o.Distance).FirstOrDefault();
            }

            if (newActor != null && newActor.IsValid && newActor.Position != Vector3.Zero)
            {
                //if (!IsPortal || Position == Vector3.Zero)
                Position = newActor.Position;
                actor = newActor;

                if (newActor is GizmoLootContainer)
                {
                    bool chestOpen = false;
                    try
                    {
                        chestOpen = newActor.CommonData.GetAttribute<int>(ActorAttributeType.ChestOpen) != 0;
                    }
                    catch { }
                    if (chestOpen)
                        actor = null;
                }

                switch (newActor.ActorType)
                {
                    case ActorType.Monster:
                        {
                            newUnit = (DiaUnit)newActor;
                            if (!newUnit.IsDead)
                            {
                                actor = newActor;
                            }
                            else
                                actor = null;
                            break;
                        }
                }
            }
            else
            {
                actor = null;
            }
        }

        private bool ShouldWaitForInteraction()
        {
            return ZetaDia.Me.LoopingAnimationEndTime > 0 || Math.Abs(DateTime.UtcNow.Subtract(lastInteract).TotalSeconds) > interactWaitSeconds;
        }

        private async Task<bool> InteractRoutine()
        {
            if (Player.IsPlayerValid() && actor.IsValid)
            {

                if (DungeonStonesPresent && (GameUI.IsElementVisible(GameUI.GenericOK) || GameUI.IsElementVisible(UIElements.ConfirmationDialogOkButton)))
                {
                    GameUI.SafeClickElement(GameUI.GenericOK);
                    await Coroutine.Yield();

                    GameUI.SafeClickElement(UIElements.ConfirmationDialogOkButton);
                    await Coroutine.Yield();

                    await Coroutine.Sleep(3000);
                    return true;
                }


                if (startingWorldId <= 0)
                {
                    startingWorldId = ZetaDia.CurrentWorldId;
                }

                interactWaitSeconds = DungeonStonesPresent ? 4 : 1;
                LogInteraction();

                if (IsPortal)
                {
                    GameEvents.FireWorldTransferStart();
                }

                await Coroutine.Wait(TimeSpan.FromSeconds(3), () => IsChanneling);

                switch (actor.ActorType)
                {
                    case ActorType.Gizmo:
                        switch (actor.ActorInfo.GizmoType)
                        {
                            case GizmoType.BossPortal:
                            case GizmoType.Portal:
                            case GizmoType.ReturnPortal:
                                ZetaDia.Me.UsePower(SNOPower.GizmoOperatePortalWithAnimation, actor.Position);
                                break;
                            default:
                                ZetaDia.Me.UsePower(SNOPower.Axe_Operate_Gizmo, actor.Position);
                                break;
                        }
                        break;
                    case ActorType.Monster:
                        ZetaDia.Me.UsePower(SNOPower.Axe_Operate_NPC, actor.Position);
                        break;
                }

                // Doubly-make sure we interact
                actor.Interact();

                GameUI.SafeClickElement(GameUI.GenericOK);
                GameUI.SafeClickElement(UIElements.ConfirmationDialogOkButton);

                if (startInteractPosition == Vector3.Zero)
                    startInteractPosition = ZetaDia.Me.Position;

                lastPosition = ZetaDia.Me.Position;
                completedInteractions++;
                lastInteract = DateTime.UtcNow;
                if (DungeonStonesPresent || IsPortal)
                    await Coroutine.Sleep(500);

                return true;

            }
            return false;
        }
      

        private void LogInteraction()
        {
            Logger.Debug("Interacting with Object: {0} {1} attempt: {2}, lastInteractDuration: {3:0}",
                actor.ActorSNO, Status(), completedInteractions, Math.Abs(DateTime.UtcNow.Subtract(lastInteract).TotalSeconds));
        }

        private bool WithinMaxSearchDistance()
        {
            return ZetaDia.Me.Position.Distance(Position) < MaxSearchDistance;
        }

        private string interactReason = "";
        private bool WithinInteractRange()
        {
            if (!ZetaDia.IsInGame)
                return false;
            if (ZetaDia.IsLoadingWorld)
                return false;
            if (ZetaDia.Me == null)
                return false;
            if (!ZetaDia.Me.IsValid)
                return false;
            if (ZetaDia.Me.HitpointsCurrent <= 0)
                return false;

            if (actor != null && actor.IsValid)
            {
                float distance = ZetaDia.Me.Position.Distance2D(actor.Position);
                float radiusDistance = actor.Distance - actor.CollisionSphere.Radius;
                Vector3 radiusPoint = MathEx.CalculatePointFrom(actor.Position, ZetaDia.Me.Position, actor.CollisionSphere.Radius);
                if (moveResult == MoveResult.ReachedDestination)
                {
                    interactReason = "ReachedDestination";
                    return true;
                }
                if (distance < 7.5f)
                {
                    interactReason = "Distance < 7.5f";
                    return true;
                }
                if (distance < InteractRange && actor.InLineOfSight && !Navigator.Raycast(ZetaDia.Me.Position, radiusPoint))
                {
                    interactReason = "InLoSRaycast";
                    return true;
                }
                if (radiusDistance < 5f)
                {
                    interactReason = "Radius < 2.5f";
                    return true;
                }
                return false;
            }
            interactReason = "DefaultInteractRange";
            return ZetaDia.Me.Position.Distance(Position) < InteractRange;
        }

        private bool IsChanneling
        {
            get
            {
                if (!ZetaDia.Me.IsValid)
                    return false;
                if (!ZetaDia.Me.CommonData.IsValid)
                    return false;
                if (ZetaDia.Me.CommonData.AnimationState == AnimationState.Channeling)
                    return true;
                if (ZetaDia.Me.LoopingAnimationEndTime > 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Checks to see if animation on actor matches EndAnimation
        /// </summary>
        /// <returns></returns>
        private bool AnimationMatch()
        {
            try
            {
                bool match = endAnimation != SNOAnim.Invalid && actor.CommonData.CurrentAnimation == endAnimation;

                return match;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check to see if isPortal is true and destinationWorldId is defined and we've changed worlds
        /// </summary>
        /// <returns>True if we're in the new, desired world</returns>
        private bool WorldHasChanged()
        {
            try
            {
                if (IsPortal && DestinationWorldId > 0)
                {
                    // DestinationWorld Id matches
                    return completedInteractions > 0 && ZetaDia.CurrentWorldId == DestinationWorldId && ZetaDia.Me.Position.Distance(startInteractPosition) > InteractRange;
                }
                else if (IsPortal && (DestinationWorldId > 0 || DestinationWorldId == -1) && startingWorldId != 0)
                {
                    // WorldId Changed
                    return completedInteractions > 0 && ZetaDia.CurrentWorldId != startingWorldId;
                }
                else if (IsPortal && startInteractPosition != Vector3.Zero)
                {
                    // Player moved from the original interaction position (same world portals and such)
                    return completedInteractions > 0 && ZetaDia.Me.Position.Distance(startInteractPosition) > InteractRange;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        private bool Move(Vector3 NavTarget)
        {
            bool result = false;
            if (lastPosition != Vector3.Zero && lastPosition.Distance(ZetaDia.Me.Position) >= 1)
            {
                lastPosition = ZetaDia.Me.Position;
            }
            // DB 300+ always uses local nav! Yay :)
            if (NavTarget.Distance(ZetaDia.Me.Position) > pathPointLimit)
                NavTarget = MathEx.CalculatePointFrom(ZetaDia.Me.Position, NavTarget, NavTarget.Distance(ZetaDia.Me.Position) - pathPointLimit);

            if (StraightLinePathing)
            {
                Navigator.PlayerMover.MoveTowards(Position);
                moveResult = MoveResult.Moved;
            }
            else
            {
                moveResult = QTNavigator.MoveTo(NavTarget, Status(), true);
            }
            switch (moveResult)
            {
                case MoveResult.Moved:
                case MoveResult.ReachedDestination:
                case MoveResult.PathGenerated:
                case MoveResult.PathGenerating:
                    result = true;
                    break;
                case MoveResult.PathGenerationFailed:
                case MoveResult.UnstuckAttempt:
                case MoveResult.Failed:
                    break;
            }
            lastPosition = ZetaDia.Me.Position;

            if (QuestTools.EnableDebugLogging)
            {
                Logger.Debug("MoveResult: {0} {1}", moveResult.ToString(), Status());
            }
            return result;
        }
        private String Status()
        {
            String status = "";
            try
            {
                if (!QuestToolsSettings.Instance.DebugEnabled)
                    return status;

                if (verbose)
                {
                    status = String.Format(
                        "questId=\"{0}\" stepId=\"{1}\" actorId=\"{10}\" x=\"{2:0}\" y=\"{3:0}\" z=\"{4:0}\" interactRange=\"{5}\" interactAttempts={11} distance=\"{6:0}\" maxSearchDistance={7} rayCastDistance={8} lastPosition={9}, isPortal={12} destinationWorldId={13}, startInteractPosition={14} completedInteractAttempts={15} interactReason={16}",
                        ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, X, Y, Z, InteractRange,
                        (actor != null ? actor.Distance : Position.Distance(ZetaDia.Me.Position)),
                        this.MaxSearchDistance, this.pathPointLimit, this.lastPosition, this.ActorId, this.InteractAttempts, this.IsPortal, this.DestinationWorldId, startInteractPosition, completedInteractions, interactReason);
                }
                else
                {
                    status = String.Format("questId=\"{0}\" stepId=\"{1}\" x=\"{2:0}\" y=\"{3:0}\" z=\"{4:0}\" interactRange=\"{5}\" interactAttempts={11} maxSearchDistance={7} rayCastDistance={8} lastPosition={9}, actorId=\"{10}\" isPortal={11} destinationWorldId={12}",
                        ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, X, Y, Z, InteractRange,
                        this.MaxSearchDistance, this.pathPointLimit, this.lastPosition, this.ActorId, this.InteractAttempts, this.IsPortal, this.DestinationWorldId);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error in MoveToActor Status(): " + ex.ToString());
            }
            try
            {
                if (actor != null && actor.IsValid && actor.CommonData != null && actor.CommonData.Position != null)
                {
                    status += String.Format(" actorId=\"{0}\", Name={1} InLineOfSight={2} ActorType={3} Position= {4}",
                        actor.ActorSNO, actor.Name, actor.InLineOfSight, actor.ActorType, StringUtils.GetProfileCoordinates(actor.Position));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error in MoveToActor Status(): " + ex.ToString());
            }
            return status;

        }

        private void EndDebug(string message, params object[] args)
        {
            Logger.Debug(message, args);
            _done = true;

        }
        private void EndDebug(string message)
        {
            EndDebug(message, 0);
        }
        private void End(string message, params object[] args)
        {
            Logger.Log(message, args);
            _done = true;
        }
        private void End(string message)
        {
            End(message, 0);
        }

        public override void ResetCachedDone()
        {
            _done = false;
            actor = null;
            tagStartTime = DateTime.UtcNow;
            completedInteractions = 0;
            startingWorldId = 0;
            base.ResetCachedDone();
        }
    }
}
