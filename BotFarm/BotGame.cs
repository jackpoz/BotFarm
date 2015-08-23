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
        public UInt64 GroupLeaderGuid { get; private set; }
        public List<UInt64> GroupMembersGuids = new List<UInt64>();
        DateTime CorpseReclaim;
        #endregion

        public BotGame(string hostname, int port, string username, string password, int realmId, int character, BotBehaviorSettings behavior)
            : base(hostname, port, username, password, realmId, character)
        {
            this.Behavior = behavior;

            if (Behavior.AutoResurrect)
            {
                // Resurrect if bot reaches 0 hp
                AddTrigger(new Trigger(new[] 
                { 
                    new UpdateFieldTriggerAction((int)UnitField.UNIT_FIELD_HEALTH, 0)
                }, () => Resurrect()));

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
        }

        public override void Start()
        {
            base.Start();

            ScheduleAction(() => DoTextEmote(TextEmote.Yawn), DateTime.Now.AddMinutes(5), new TimeSpan(0, 5, 0));
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

        #region Handlers
        [PacketHandler(WorldCommand.SMSG_GROUP_INVITE)]
        protected void HandlePartyInvite(InPacket packet)
        {
            if(Behavior.AutoAcceptGroupInvites)
                SendPacket(new OutPacket(WorldCommand.CMSG_GROUP_ACCEPT, 4));
        }

        [PacketHandler(WorldCommand.SMSG_GROUP_LIST)]
        protected void HandlePartyList(InPacket packet)
        {
            GroupType groupType = (GroupType)packet.ReadByte();
            packet.ReadByte();
            packet.ReadByte();
            packet.ReadByte();
            if (groupType.HasFlag(GroupType.GROUPTYPE_LFG))
            {
                packet.ReadByte();
                packet.ReadUInt32();
            }
            packet.ReadUInt64();
            packet.ReadUInt32();
            uint membersCount = packet.ReadUInt32();
            GroupMembersGuids.Clear();
            for(uint index = 0; index < membersCount; index++)
            {
                packet.ReadCString();
                UInt64 memberGuid = packet.ReadUInt64();
                GroupMembersGuids.Add(memberGuid);
                packet.ReadByte();
                packet.ReadByte();
                packet.ReadByte();
                packet.ReadByte();
            }
            GroupLeaderGuid = packet.ReadUInt64();
        }

        [PacketHandler(WorldCommand.SMSG_GROUP_DESTROYED)]
        protected void HandlePartyDisband(InPacket packet)
        {
            GroupLeaderGuid = 0;
            GroupMembersGuids.Clear();
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
        #endregion

        #region Actions
        public void MoveTo(Position destination)
        {
            const float MovementEpsilon = 1.0f;

            if (destination.MapID != Player.MapID)
            {
                Log("Trying to move to another map", Client.UI.LogLevel.Warning);
                HandleTriggerInput(TriggerActionType.DestinationReached, false);
                return;
            }

            Path path = null;
            using(var detour = new DetourCLI.Detour())
            {
                List<DetourCLI.Point> resultPath;
                bool successful = detour.FindPath(Player.X, Player.Y, Player.Z,
                                        destination.X, destination.Y, destination.Z,
                                        Player.MapID, out resultPath);
                if (!successful)
                {
                    HandleTriggerInput(TriggerActionType.DestinationReached, false);
                    return;
                }

                path = new Path(resultPath, Player.Speed);
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
                O = direction.O
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
                O = Player.O
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
                if (remaining.Length > MovementEpsilon && oldRemaining.Length >= remaining.Length)
                {
                    oldRemaining = remaining;

                    var heartbeat = new MovementPacket(WorldCommand.MSG_MOVE_HEARTBEAT)
                    {
                        GUID = Player.GUID,
                        flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                        X = Player.X,
                        Y = Player.Y,
                        Z = Player.Z,
                        O = Player.O
                    };
                    SendPacket(heartbeat);
                }
                else
                {
                    var stopMoving = new MovementPacket(WorldCommand.MSG_MOVE_STOP)
                    {
                        GUID = Player.GUID,
                        X = destination.X,
                        Y = destination.Y,
                        Z = destination.Z,
                        O = Player.O
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
