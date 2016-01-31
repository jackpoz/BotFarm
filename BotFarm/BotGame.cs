using BotFarm.Properties;
using Client;
using Client.UI;
using Client.World;
using Client.World.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.World.Definitions;
using Client.World.Entities;
using DetourCLI;
using MapCLI;
using DBCStoresCLI;
using BotFarm.AI;

namespace BotFarm
{
    class BotGame : AutomatedGame
    {
        public bool SettingUp
        {
            get;
            set;
        }

        public BotBehaviorSettings Behavior
        {
            get;
            private set;
        }

        #region Player members
        DateTime CorpseReclaim;
        ulong TraderGUID
        {
            get;
            set;
        }
        HashSet<ulong> TradedGUIDs = new HashSet<ulong>();
        #endregion

        public BotGame(string hostname, int port, string username, string password, int realmId, int character, BotBehaviorSettings behavior)
            : base(hostname, port, username, password, realmId, character)
        {
            this.Behavior = behavior;

            #region AutoResurrect
            if (Behavior.AutoResurrect)
            {
                // Resurrect if bot reaches 0 hp
                AddTrigger(new Trigger(new[] 
                { 
                    new UpdateFieldTriggerAction((int)UnitField.UNIT_FIELD_HEALTH, 0)
                }, () =>
                   {
                       CancelActionsByFlag(ActionFlag.Movement);
                       Resurrect();
                   }));

                // Resurrect sequence
                AddTrigger(new Trigger(new TriggerAction[] 
                { 
                    new UpdateFieldTriggerAction((int)PlayerField.PLAYER_FLAGS, (uint)PlayerFlags.PLAYER_FLAGS_GHOST, () =>
                        {
                            OutPacket corpseQuery = new OutPacket(WorldCommand.MSG_CORPSE_QUERY);
                            SendPacket(corpseQuery);
                        }),
                    new OpcodeTriggerAction(WorldCommand.MSG_CORPSE_QUERY, packet =>
                    {
                        var inPacket = packet as InPacket;
                        if (inPacket == null)
                            return false;

                        bool found = inPacket.ReadByte() != 0;
                        if (found)
                        {
                            var mapId = inPacket.ReadInt32();

                            var corpsePosition = new Position(inPacket.ReadSingle(),
                                                              inPacket.ReadSingle(),
                                                              inPacket.ReadSingle(),
                                                              0.0f,
                                                              inPacket.ReadInt32());
                            Player.CorpsePosition = corpsePosition.GetPosition();

                            if (mapId == corpsePosition.MapID)
                            {
                                MoveTo(corpsePosition);
                                return true;
                            }
                        }

                        return false;
                    }),
                    new CustomTriggerAction(TriggerActionType.DestinationReached, (inputs) =>
                    {
                        if (Player.IsGhost && (Player.CorpsePosition - Player).Length <= 39f)
                        {
                            if (DateTime.Now > CorpseReclaim)
                                return true;
                            else
                                ScheduleAction(() => HandleTriggerInput(TriggerActionType.DestinationReached, inputs), CorpseReclaim.AddSeconds(1));
                        }

                        return false;
                    },() => 
                      {
                          OutPacket reclaimCorpse = new OutPacket(WorldCommand.CMSG_RECLAIM_CORPSE);
                          reclaimCorpse.Write(Player.GUID);
                          SendPacket(reclaimCorpse);
                      })
                }, null));
            }
            #endregion

            #region Begger
            if (Behavior.Begger)
            {
                // Beg a player only once
                AddTrigger(new Trigger(new[]
                {
                    new AlwaysTrueTriggerAction(TriggerActionType.TradeCompleted)
                }, () => TradedGUIDs.Add(TraderGUID)));
            }
            #endregion
        }

        public override void Start()
        {
            base.Start();

            // Anti-kick for being afk
            ScheduleAction(() => DoTextEmote(TextEmote.Yawn), DateTime.Now.AddMinutes(5), new TimeSpan(0, 5, 0));

            #region Begger
            if (Behavior.Begger)
            {
                // Follow player trigger
                //  - find closest player and follow him begging for money with chat messages (unless its a bot)
                ScheduleAction(() =>
                {
                    if (TraderGUID != 0)
                        return;

                    CancelActionsByFlag(ActionFlag.Movement);
                    var target = FindClosestNonBotPlayer(obj => !TradedGUIDs.Contains(obj.GUID));
                    if (target != null)
                    {
                        DoSayChat("Please " + GetPlayerName(target) + ", give me some money");
                        Follow(target);
                    }
                }, DateTime.Now.AddSeconds(30), new TimeSpan(0, 0, 30));
            }
            #endregion

            #region FollowGroupLeader
            if (Behavior.FollowGroupLeader)
            {
                PushAI(new FollowGroupLeaderAI());
            }
            #endregion

            #region Explorer
            if (Behavior.Explorer)
            {
                AchievementExploreLocation targetLocation = null;
                List<AchievementExploreLocation> missingLocations = null;
                Position currentPosition = new Position();

                ScheduleAction(() =>
                {
                    if (!Player.IsAlive)
                        return;

                    if (targetLocation != null)
                    {
                        if (!HasExploreCriteria(targetLocation.CriteriaID) && (currentPosition - Player).Length > MovementEpsilon)
                        {
                            currentPosition = Player.GetPosition();
                            return;
                        }

                        targetLocation = null;
                    }

                    CancelActionsByFlag(ActionFlag.Movement);
                    currentPosition = Player.GetPosition();

                    if (missingLocations == null)
                        missingLocations = DBCStores.GetAchievementExploreLocations(Player.X, Player.Y, Player.Z, Player.MapID);

                    missingLocations = missingLocations.Where(loc => !HasExploreCriteria(loc.CriteriaID)).ToList();
                    if (missingLocations.Count == 0)
                        return;

                    float closestDistance = float.MaxValue;
                    var playerPosition = new Point(Player.X, Player.Y, Player.Z);
                    foreach (var missingLoc in missingLocations)
                    {
                        float distance = (missingLoc.Location - playerPosition).Length;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            targetLocation = missingLoc;
                        }
                    }

                    MoveTo(new Position(targetLocation.Location.X, targetLocation.Location.Y, targetLocation.Location.Z, 0f, Player.MapID));
                }, DateTime.Now.AddSeconds(30), new TimeSpan(0, 0, 5));
            }
            #endregion
        }

        public override void NoCharactersFound()
        {
            if (!SettingUp)
            {
                Log("Removing current bot because there are no characters");
                BotFactory.Instance.RemoveBot(this);
            }
            else
                CreateCharacter(Race.Human, Class.Priest);
        }

        public override void InvalidCredentials()
        {
            BotFactory.Instance.RemoveBot(this);
        }

        WorldObject FindClosestNonBotPlayer(Func<WorldObject, bool> additionalCheck = null)
        {
            return FindClosestObject(HighGuid.Player, obj =>
            {
                if (BotFactory.Instance.IsBot(obj))
                    return false;
                if (additionalCheck != null && !additionalCheck(obj))
                    return false;
                return true;
            });
        }

        #region Handlers
        [PacketHandler(WorldCommand.SMSG_GROUP_INVITE)]
        protected void HandlePartyInvite(InPacket packet)
        {
            if(Behavior.AutoAcceptGroupInvites)
                SendPacket(new OutPacket(WorldCommand.CMSG_GROUP_ACCEPT, 4));
        }

        [PacketHandler(WorldCommand.SMSG_RESURRECT_REQUEST)]
        protected void HandleResurrectRequest(InPacket packet)
        {
            var resurrectorGuid = packet.ReadUInt64();
            OutPacket response = new OutPacket(WorldCommand.CMSG_RESURRECT_RESPONSE);
            response.Write(resurrectorGuid);
            if (Behavior.AutoAcceptResurrectRequests)
            {
                response.Write((byte)1);
                SendPacket(response);

                OutPacket result = new OutPacket(WorldCommand.MSG_MOVE_TELEPORT_ACK);
                result.WritePacketGuid(Player.GUID);
                result.Write((UInt32)0);
                result.Write(DateTime.Now.Millisecond);
                SendPacket(result);
            }
            else
            {
                response.Write((byte)0);
                SendPacket(response);
            }
        }

        [PacketHandler(WorldCommand.SMSG_CORPSE_RECLAIM_DELAY)]
        protected void HandleCorpseReclaimDelay(InPacket packet)
        {
            CorpseReclaim = DateTime.Now.AddMilliseconds(packet.ReadUInt32());
        }

        [PacketHandler(WorldCommand.SMSG_TRADE_STATUS)]
        protected void HandleTradeStatus(InPacket packet)
        {
            if (Behavior.Begger)
            {
                TradeStatus status = (TradeStatus)packet.ReadUInt32();
                switch (status)
                {
                    case TradeStatus.BeginTrade:
                        TraderGUID = packet.ReadUInt64();
                        // Stop moving
                        CancelActionsByFlag(ActionFlag.Movement);
                        // Accept trade
                        OutPacket beginTrade = new OutPacket(WorldCommand.CMSG_BEGIN_TRADE);
                        SendPacket(beginTrade);
                        break;
                    case TradeStatus.Canceled:
                        EnableActionsByFlag(ActionFlag.Movement);
                        TraderGUID = 0;
                        break;
                    case TradeStatus.Accept:
                        OutPacket acceptTrade = new OutPacket(WorldCommand.CMSG_ACCEPT_TRADE);
                        SendPacket(acceptTrade);
                        break;
                    case TradeStatus.Tradecomplete:
                        DoSayChat("Thank you!");
                        EnableActionsByFlag(ActionFlag.Movement);
                        HandleTriggerInput(TriggerActionType.TradeCompleted, TraderGUID);
                        TraderGUID = 0;
                        break;
                }
            }
        }
        #endregion

        #region Actions
        public void MoveTo(Position destination)
        {
            if (destination.MapID != Player.MapID)
            {
                Log("Trying to move to another map", Client.UI.LogLevel.Warning);
                HandleTriggerInput(TriggerActionType.DestinationReached, false);
                return;
            }

            Path path = null;
            using(var detour = new DetourCLI.Detour())
            {
                List<MapCLI.Point> resultPath;
                var pathResult = detour.FindPath(Player.X, Player.Y, Player.Z,
                                        destination.X, destination.Y, destination.Z,
                                        Player.MapID, out resultPath);
                if (pathResult != PathType.Complete)
                {
                    Log($"Cannot reach destination, FindPath() returned {pathResult} : {destination.ToString()}", Client.UI.LogLevel.Warning);
                    HandleTriggerInput(TriggerActionType.DestinationReached, false);
                    return;
                }

                path = new Path(resultPath, Player.Speed, Player.MapID);
                var destinationPoint = path.Destination;
                destination.SetPosition(destinationPoint.X, destinationPoint.Y, destinationPoint.Z);
            }

            var remaining = destination - Player.GetPosition();
            // check if we even need to move
            if (remaining.Length < MovementEpsilon)
            {
                HandleTriggerInput(TriggerActionType.DestinationReached, true);
                return;
            }

            var direction = remaining.Direction;

            var facing = new MovementPacket(WorldCommand.MSG_MOVE_SET_FACING)
            {
                GUID = Player.GUID,
                flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                X = Player.X,
                Y = Player.Y,
                Z = Player.Z,
                O = path.CurrentOrientation
            };

            SendPacket(facing);
            Player.SetPosition(facing.GetPosition());

            var startMoving = new MovementPacket(WorldCommand.MSG_MOVE_START_FORWARD)
            {
                GUID = Player.GUID,
                flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                X = Player.X,
                Y = Player.Y,
                Z = Player.Z,
                O = path.CurrentOrientation
            };
            SendPacket(startMoving);

            var previousMovingTime = DateTime.Now;

            var oldRemaining = remaining;
            ScheduleAction(() =>
            {
                Point progressPosition = path.MoveAlongPath((float)(DateTime.Now - previousMovingTime).TotalSeconds);
                Player.SetPosition(progressPosition.X, progressPosition.Y, progressPosition.Z);
                previousMovingTime = DateTime.Now;

                remaining = destination - Player.GetPosition();
                if (remaining.Length > MovementEpsilon)
                {
                    oldRemaining = remaining;

                    var heartbeat = new MovementPacket(WorldCommand.MSG_MOVE_HEARTBEAT)
                    {
                        GUID = Player.GUID,
                        flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                        X = Player.X,
                        Y = Player.Y,
                        Z = Player.Z,
                        O = path.CurrentOrientation
                    };
                    SendPacket(heartbeat);
                }
                else
                {
                    var stopMoving = new MovementPacket(WorldCommand.MSG_MOVE_STOP)
                    {
                        GUID = Player.GUID,
                        X = Player.X,
                        Y = Player.Y,
                        Z = Player.Z,
                        O = path.CurrentOrientation
                    };
                    SendPacket(stopMoving);
                    Player.SetPosition(stopMoving.GetPosition());

                    CancelActionsByFlag(ActionFlag.Movement);

                    HandleTriggerInput(TriggerActionType.DestinationReached, true);
                }
            }, new TimeSpan(0, 0, 0, 0, 100), flags: ActionFlag.Movement);
        }

        public void Resurrect()
        {
            OutPacket repop = new OutPacket(WorldCommand.CMSG_REPOP_REQUEST);
            repop.Write((byte)0);
            SendPacket(repop);
        }
        #endregion

        #region Logging
        public override void Log(string message, LogLevel level = LogLevel.Info)
        {
            BotFactory.Instance.Log(Username + " - " + message, level);
        }

        public override void LogLine(string message, LogLevel level = LogLevel.Info)
        {
            BotFactory.Instance.Log(Username + " - " + message, level);
        }

        public override void LogException(string message)
        {
            BotFactory.Instance.Log(Username + " - " + message, LogLevel.Error);
        }

        public override void LogException(Exception ex)
        {
            BotFactory.Instance.Log(string.Format(Username + " - {0} {1}", ex.Message, ex.StackTrace), LogLevel.Error);
        }
        #endregion
    }
}
