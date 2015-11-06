using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.UI;
using Client.Authentication;
using Client.World;
using Client.Chat;
using Client;
using Client.World.Network;
using Client.Authentication.Network;
using System.Threading;
using System.Numerics;
using Client.Chat.Definitions;
using Client.World.Definitions;
using System.Diagnostics;
using Client.World.Entities;
using System.Collections;

namespace Client
{
    public class AutomatedGame : IGameUI, IGame
    {
        #region Properties
        public bool Running { get; set; }
        GameSocket socket;
        public BigInteger Key { get; private set; }
        public string Hostname { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public bool LoggedIn { get; private set; }
        public int RealmID { get; private set; }
        public int Character { get; private set; }
        public bool Connected { get; private set; }
        TaskCompletionSource<bool> loggedOutEvent = new TaskCompletionSource<bool>();
        ScheduledActions scheduledActions;
        ActionFlag disabledActions;
        public GameWorld World
        {
            get;
            private set;
        }
        public Player Player
        {
            get;
            protected set;
        }
        public override LogLevel LogLevel
        {
            get
            {
                return Client.UI.LogLevel.Error;
            }
            set
            {
            }
        }
        public override IGame Game
        {
            get
            {
                return this;
            }
            set
            {
            }
        }
        UpdateObjectHandler updateObjectHandler;

        protected Dictionary<ulong, WorldObject> Objects
        {
            get;
            private set;
        }
        #endregion

        public AutomatedGame(string hostname, int port, string username, string password, int realmId, int character)
        {
            this.RealmID = realmId;
            this.Character = character;
            scheduledActions = new ScheduledActions();
            updateObjectHandler = new UpdateObjectHandler(this);
            Triggers = new List<Trigger>();
            World = new GameWorld();
            Player = new Player();
            Player.OnFieldUpdated += OnFieldUpdate;
            Objects = new Dictionary<ulong, WorldObject>();

            this.Hostname = hostname;
            this.Port = port;
            this.Username = username;
            this.Password = password;

            socket = new AuthSocket(this, Hostname, Port, Username, Password);
            socket.InitHandlers();
        }

        #region Basic Methods
        public void ConnectTo(WorldServerInfo server)
        {
            if (socket is AuthSocket)
                Key = ((AuthSocket)socket).Key;

            socket.Dispose();

            socket = new WorldSocket(this, server);
            socket.InitHandlers();

            if (socket.Connect())
            {
                socket.Start();
                Connected = true;
            }
            else
                Reconnect();
        }

        public virtual void Start()
        {
            // the initial socket is an AuthSocket - it will initiate its own asynch read
            Running = socket.Connect();

            Task.Run(async () =>
                {
                    while (Running)
                    {
                        // main loop here
                        Update();
                        await Task.Delay(100);
                    }
                });
        }

        public override void Update()
        {
            if (World.SelectedCharacter == null)
                return;

            while (scheduledActions.Count != 0)
            {
                var scheduledAction = scheduledActions.First();
                if (scheduledAction.scheduledTime <= DateTime.Now)
                {
                    scheduledActions.RemoveAt(0);
                    if (scheduledAction.interval > TimeSpan.Zero)
                        ScheduleAction(scheduledAction.action, DateTime.Now + scheduledAction.interval, scheduledAction.interval, scheduledAction.flags);
                    scheduledAction.action();
                }
                else
                    break;
            }
        }

        public void Reconnect()
        {
            Connected = false;
            LoggedIn = false;
            while (Running)
            {
                socket.Disconnect();
                scheduledActions.Clear();
                ResetTriggers();
                socket = new AuthSocket(this, Hostname, Port, Username, Password);
                socket.InitHandlers();
                // exit from loop if the socket connected successfully
                if (socket.Connect())
                    break;

                // try again later
                Thread.Sleep(10000);
            }
        }

        public override async Task Exit()
        {
            ClearTriggers();
            if (LoggedIn)
            {
                OutPacket logout = new OutPacket(WorldCommand.CMSG_LOGOUT_REQUEST);
                SendPacket(logout);
                await loggedOutEvent.Task;
            }
            else
            {
                Connected = false;
                LoggedIn = false;
                Running = false;
            }
        }

        public void SendPacket(OutPacket packet)
        {
            if (socket is WorldSocket)
            {
                ((WorldSocket)socket).Send(packet);
                HandleTriggerInput(TriggerActionType.Opcode, packet);
            }
        }

        public override void PresentRealmList(WorldServerList realmList)
        {
            if (RealmID >= realmList.Count)
            {
                LogException("Invalid RealmID '" + RealmID + "' specified in the configs");
                Environment.Exit(1);
            }

            LogLine("Connecting to realm " + realmList[RealmID].Name);
            ConnectTo(realmList[RealmID]);
        }

        public override void PresentCharacterList(Character[] characterList)
        {
            World.SelectedCharacter = characterList[Character];
            OutPacket packet = new OutPacket(WorldCommand.CMSG_PLAYER_LOGIN);
            packet.Write(World.SelectedCharacter.GUID);
            SendPacket(packet);
            LoggedIn = true;
            Player.GUID = World.SelectedCharacter.GUID;
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override int Read()
        {
            throw new NotImplementedException();
        }

        public override ConsoleKeyInfo ReadKey()
        {
            throw new NotImplementedException();
        }

        public void ScheduleAction(Action action, TimeSpan interval = default(TimeSpan), ActionFlag flags = ActionFlag.None)
        {
            ScheduleAction(action, DateTime.Now, interval, flags);
        }

        public void ScheduleAction(Action action, DateTime time, TimeSpan interval = default(TimeSpan), ActionFlag flags = ActionFlag.None)
        {
            if (Running && (flags == ActionFlag.None || !disabledActions.HasFlag(flags)))
                scheduledActions.Add(new RepeatingAction(action, time, interval, flags));
        }

        public void CancelActionsByFlag(ActionFlag flag)
        {
            scheduledActions.RemoveByFlag(flag);
        }

        public void DisableActionsByFlag(ActionFlag flag)
        {
            disabledActions |= flag;
            CancelActionsByFlag(flag);
        }

        public void EnableActionsByFlag(ActionFlag flag)
        {
            disabledActions &= ~flag;
        }

        public void CreateCharacter(Race race, Class classWow)
        {
            Log("Creating new character");
            OutPacket createCharacterPacket = new OutPacket(WorldCommand.CMSG_CHAR_CREATE);
            StringBuilder charName = new StringBuilder("Bot");
            foreach (char c in Username.Substring(3).Take(9))
	        {
                charName.Append((char)(97 + int.Parse(c.ToString())));
	        }

            // Ensure Name rules are applied
            char previousChar = '\0';
            for (int i = 0; i < charName.Length; i++ )
            {
                if (charName[i] == previousChar)
                    charName[i]++;
                previousChar = charName[i];
            }

            createCharacterPacket.Write(charName.ToString().ToCString());
            createCharacterPacket.Write((byte)race);
            createCharacterPacket.Write((byte)classWow);
            createCharacterPacket.Write((byte)Gender.Male);
            byte skin = 6; createCharacterPacket.Write(skin);
            byte face = 5; createCharacterPacket.Write(face);
            byte hairStyle = 0; createCharacterPacket.Write(hairStyle);
            byte hairColor = 1; createCharacterPacket.Write(hairColor);
            byte facialHair = 5; createCharacterPacket.Write(facialHair);
            byte outfitId = 0; createCharacterPacket.Write(outfitId);

            SendPacket(createCharacterPacket);
        }

        public async Task Dispose()
        {
            Running = false;
            scheduledActions.Clear();

            await Exit();

            if (socket != null)
                socket.Dispose();
        }

        public virtual void NoCharactersFound()
        { }

        public virtual void InvalidCredentials()
        { }

        protected WorldObject FindClosestObject(HighGuid highGuidType, Func<WorldObject, bool> additionalCheck = null)
        {
            float closestDistance = float.MaxValue;
            WorldObject closestObject = null;

            foreach (var obj in Objects.Values.ToList())
            {
                if (!obj.IsType(highGuidType))
                    continue;

                if (additionalCheck != null && !additionalCheck(obj))
                    continue;

                if (obj.MapID != Player.MapID)
                    continue;

                float distance = (obj - Player).Length;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                }
            }

            return closestObject;
        }

        protected string GetPlayerName(WorldObject obj)
        {
            return GetPlayerName(obj.GUID);
        }

        protected string GetPlayerName(ulong guid)
        {
            string name;
            if (Game.World.PlayerNameLookup.TryGetValue(guid, out name))
                return name;
            else
                return "";
        }
        #endregion

        #region Commands
        public void DoSayChat(string message)
        {
            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Say);
            var race = World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(message.ToCString());
            SendPacket(response);
        }

        public void DoPartyChat(string message)
        {
            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Party);
            var race = World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(message.ToCString());
            SendPacket(response);
        }

        public void DoGuildChat(string message)
        {
            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Guild);
            var race = World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(message.ToCString());
            SendPacket(response);
        }

        public void DoWhisperChat(string message, string player)
        {
            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Whisper);
            var race = World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(player.ToCString());
            response.Write(message.ToCString());
            SendPacket(response);
        }

        public void Tele(string teleport)
        {
            DoSayChat(".tele " + teleport);
        }

        public void CastSpell(int spellid, bool chatLog = true)
        {
            DoSayChat(".cast " + spellid);
            if (chatLog)
                DoSayChat("Casted spellid " + spellid);
        }

        #endregion

        #region Actions
        public void DoTextEmote(TextEmote emote)
        {
            var packet = new OutPacket(WorldCommand.CMSG_TEXT_EMOTE);
            packet.Write((uint)emote);
            packet.Write((uint)0);
            packet.Write((ulong)0);
            SendPacket(packet);
        }

        public void SetFacing(float orientation)
        {
            if (!Player.GetPosition().IsValid)
                return;
            var packet = new OutPacket(WorldCommand.MSG_MOVE_SET_FACING);
            packet.WritePacketGuid(Player.GUID);
            packet.Write((UInt32)0); //flags
            packet.Write((UInt16)0); //flags2
            packet.Write((UInt32)0); //time
            Player.O = orientation;
            packet.Write(Player.X);
            packet.Write(Player.Y);
            packet.Write(Player.Z);
            packet.Write(Player.O);
            packet.Write((UInt32)0); //fall time
            SendPacket(packet);
        }
        #endregion

        #region Packet Handlers
        [PacketHandler(WorldCommand.SMSG_LOGIN_VERIFY_WORLD)]
        protected void HandleLoginVerifyWorld(InPacket packet)
        {
            Player.MapID = (int)packet.ReadUInt32();
            Player.X = packet.ReadSingle();
            Player.Y = packet.ReadSingle();
            Player.Z = packet.ReadSingle();
            Player.O = packet.ReadSingle();
        }

        [PacketHandler(WorldCommand.SMSG_NEW_WORLD)]
        protected void HandleNewWorld(InPacket packet)
        {
            Player.MapID = (int)packet.ReadUInt32();
            Player.X = packet.ReadSingle();
            Player.Y = packet.ReadSingle();
            Player.Z = packet.ReadSingle();
            Player.O = packet.ReadSingle();

            OutPacket result = new OutPacket(WorldCommand.MSG_MOVE_WORLDPORT_ACK);
            SendPacket(result);
        }

        [PacketHandler(WorldCommand.SMSG_TRANSFER_PENDING)]
        protected void HandleTransferPending(InPacket packet)
        {
            Player.ResetPosition();
            var newMap = packet.ReadUInt32();
        }

        [PacketHandler(WorldCommand.MSG_MOVE_TELEPORT_ACK)]
        protected void HandleMoveTeleportAck(InPacket packet)
        {
            var packGuid = packet.ReadPackedGuid();
            packet.ReadUInt32();
            var movementFlags = packet.ReadUInt32();
            var extraMovementFlags = packet.ReadUInt16();
            var time = packet.ReadUInt32();
            Player.X = packet.ReadSingle();
            Player.Y = packet.ReadSingle();
            Player.Z = packet.ReadSingle();
            Player.O = packet.ReadSingle();

            OutPacket result = new OutPacket(WorldCommand.MSG_MOVE_TELEPORT_ACK);
            result.WritePacketGuid(Player.GUID);
            result.Write((UInt32)0);
            result.Write(time);
            SendPacket(result);
        }

        [PacketHandler(WorldCommand.SMSG_CHAR_CREATE)]
        protected void HandleCharCreate(InPacket packet)
        {
            var response = (CommandDetail)packet.ReadByte();
            if (response == CommandDetail.CHAR_CREATE_SUCCESS)
                SendPacket(new OutPacket(WorldCommand.CMSG_CHAR_ENUM));
            else
                NoCharactersFound();
        }

        [PacketHandler(WorldCommand.SMSG_LOGOUT_RESPONSE)]
        protected void HandleLogoutResponse(InPacket packet)
        {
            bool logoutOk = packet.ReadUInt32() == 0;
            bool instant = packet.ReadByte() != 0;

            if(instant || !logoutOk)
            {
                Connected = false;
                LoggedIn = false;
                Running = false;
            }
        }

        [PacketHandler(WorldCommand.SMSG_LOGOUT_COMPLETE)]
        protected void HandleLogoutComplete(InPacket packet)
        {
            Connected = false;
            LoggedIn = false;
            Running = false;
            loggedOutEvent.SetResult(true);
        }

        [PacketHandler(WorldCommand.SMSG_UPDATE_OBJECT)]
        protected void HandleUpdateObject(InPacket packet)
        {
            updateObjectHandler.HandleUpdatePacket(packet);
        }

        [PacketHandler(WorldCommand.SMSG_COMPRESSED_UPDATE_OBJECT)]
        protected void HandleCompressedUpdateObject(InPacket packet)
        {
            updateObjectHandler.HandleUpdatePacket(packet.Inflate());
        }

        [PacketHandler(WorldCommand.SMSG_MONSTER_MOVE)]
        protected void HandleMonsterMove(InPacket packet)
        {
            updateObjectHandler.HandleMonsterMovementPacket(packet);
        }

        [PacketHandler(WorldCommand.MSG_MOVE_START_FORWARD)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_BACKWARD)]
        [PacketHandler(WorldCommand.MSG_MOVE_STOP)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_STRAFE_LEFT)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_STRAFE_RIGHT)]
        [PacketHandler(WorldCommand.MSG_MOVE_STOP_STRAFE)]
        [PacketHandler(WorldCommand.MSG_MOVE_JUMP)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_TURN_LEFT)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_TURN_RIGHT)]
        [PacketHandler(WorldCommand.MSG_MOVE_STOP_TURN)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_PITCH_UP)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_PITCH_DOWN)]
        [PacketHandler(WorldCommand.MSG_MOVE_STOP_PITCH)]
        [PacketHandler(WorldCommand.MSG_MOVE_SET_RUN_MODE)]
        [PacketHandler(WorldCommand.MSG_MOVE_SET_WALK_MODE)]
        [PacketHandler(WorldCommand.MSG_MOVE_FALL_LAND)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_SWIM)]
        [PacketHandler(WorldCommand.MSG_MOVE_STOP_SWIM)]
        [PacketHandler(WorldCommand.MSG_MOVE_SET_FACING)]
        [PacketHandler(WorldCommand.MSG_MOVE_SET_PITCH)]
        [PacketHandler(WorldCommand.MSG_MOVE_HEARTBEAT)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_ASCEND)]
        [PacketHandler(WorldCommand.MSG_MOVE_STOP_ASCEND)]
        [PacketHandler(WorldCommand.MSG_MOVE_START_DESCEND)]
        protected void HandleMove(InPacket packet)
        {
            updateObjectHandler.HandleMovementPacket(packet);
        }

        class UpdateObjectHandler
        {
            AutomatedGame game;

            uint blockCount;
            ObjectUpdateType updateType;
            ulong guid;
            TypeID objectType;
            ObjectUpdateFlags flags;
            MovementInfo movementInfo;
            Dictionary<UnitMoveType, float> movementSpeeds;
            SplineFlags splineFlags;
            float splineFacingAngle;
            ulong splineFacingTargetGuid;
            Vector3 splineFacingPointX;
            int splineTimePassed;
            int splineDuration;
            uint splineId;
            float splineVerticalAcceleration;
            int splineEffectStartTime;
            List<Vector3> splinePoints;
            SplineEvaluationMode splineEvaluationMode;
            Vector3 splineEndPoint;

            ulong transportGuid;
            Vector3 position;
            Vector3 transportOffset;
            float o;
            float corpseOrientation;

            uint lowGuid;
            ulong targetGuid;
            uint transportTimer;
            uint vehicledID;
            float vehicleOrientation;
            long goRotation;

            Dictionary<int, uint> updateFields;

            List<ulong> outOfRangeGuids;

            public UpdateObjectHandler(AutomatedGame game)
            {
                this.game = game;
                movementSpeeds = new Dictionary<UnitMoveType, float>();
                splinePoints = new List<Vector3>();
                updateFields = new Dictionary<int, uint>();
                outOfRangeGuids = new List<ulong>();
            }

            public void HandleUpdatePacket(InPacket packet)
            {
                blockCount = packet.ReadUInt32();
                for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    ResetData();

                    updateType = (ObjectUpdateType)packet.ReadByte();

                    switch (updateType)
                    {
                        case ObjectUpdateType.UPDATETYPE_VALUES:
                            guid = packet.ReadPackedGuid();
                            ReadValuesUpdateData(packet);
                            break;
                        case ObjectUpdateType.UPDATETYPE_MOVEMENT:
                            guid = packet.ReadPackedGuid();
                            ReadMovementUpdateData(packet);
                            break;
                        case ObjectUpdateType.UPDATETYPE_CREATE_OBJECT:
                        case ObjectUpdateType.UPDATETYPE_CREATE_OBJECT2:
                            guid = packet.ReadPackedGuid();
                            objectType = (TypeID)packet.ReadByte();
                            ReadMovementUpdateData(packet);
                            ReadValuesUpdateData(packet);
                            break;
                        case ObjectUpdateType.UPDATETYPE_OUT_OF_RANGE_OBJECTS:
                            var guidCount = packet.ReadUInt32();
                            for (var guidIndex = 0; guidIndex < guidCount; guidIndex++)
                                outOfRangeGuids.Add(packet.ReadPackedGuid());
                            break;
                        case ObjectUpdateType.UPDATETYPE_NEAR_OBJECTS:
                            break;
                    }

                    HandleUpdateData();
                }
            }

            public void HandleMovementPacket(InPacket packet)
            {
                ResetData();
                updateType = ObjectUpdateType.UPDATETYPE_MOVEMENT;
                guid = packet.ReadPackedGuid();
                ReadMovementInfo(packet);
                HandleUpdateData();
            }

            public void HandleMonsterMovementPacket(InPacket packet)
            {
                ResetData();
                updateType = ObjectUpdateType.UPDATETYPE_MOVEMENT;
                guid = packet.ReadPackedGuid();
                byte unk = packet.ReadByte();
                WorldObject worldObject = game.Objects[guid];
                worldObject.Set(packet.ReadVector3());
            }

            void ResetData()
            {
                updateType = ObjectUpdateType.UPDATETYPE_VALUES;
                guid = 0;
                lowGuid = 0;
                movementSpeeds.Clear();
                splinePoints.Clear();
                updateFields.Clear();
                outOfRangeGuids.Clear();
                movementInfo = null;
            }

            void ReadMovementUpdateData(InPacket packet)
            {
                flags = (ObjectUpdateFlags)packet.ReadUInt16();
                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_LIVING))
                {
                    ReadMovementInfo(packet);

                    movementSpeeds = new Dictionary<UnitMoveType,float>();
                    movementSpeeds[UnitMoveType.MOVE_WALK] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_RUN] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_RUN_BACK] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_SWIM] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_SWIM_BACK] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_FLIGHT] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_FLIGHT_BACK] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_TURN_RATE] = packet.ReadSingle();
                    movementSpeeds[UnitMoveType.MOVE_PITCH_RATE] = packet.ReadSingle();

                    if (movementInfo.Flags.HasFlag(MovementFlags.MOVEMENTFLAG_SPLINE_ENABLED))
                    {
                        splineFlags = (SplineFlags)packet.ReadUInt32();
                        if (splineFlags.HasFlag(SplineFlags.Final_Angle))
                            splineFacingAngle = packet.ReadSingle();
                        else if (splineFlags.HasFlag(SplineFlags.Final_Target))
                            splineFacingTargetGuid = packet.ReadUInt64();
                        else if (splineFlags.HasFlag(SplineFlags.Final_Point))
                            splineFacingPointX = packet.ReadVector3();

                        splineTimePassed = packet.ReadInt32();
                        splineDuration = packet.ReadInt32();
                        splineId = packet.ReadUInt32();
                        packet.ReadSingle();
                        packet.ReadSingle();
                        splineVerticalAcceleration = packet.ReadSingle();
                        splineEffectStartTime = packet.ReadInt32();
                        uint splineCount = packet.ReadUInt32();
                        for (uint index = 0; index < splineCount; index++)
                            splinePoints.Add(packet.ReadVector3());
                        splineEvaluationMode = (SplineEvaluationMode)packet.ReadByte();
                        splineEndPoint = packet.ReadVector3();
                    }
                }
                else if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_POSITION))
                {
                    transportGuid = packet.ReadPackedGuid();
                    position = packet.ReadVector3();
                    transportOffset = packet.ReadVector3();
                    o = packet.ReadSingle();
                    corpseOrientation = packet.ReadSingle();
                }
                else if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_STATIONARY_POSITION))
                {
                    position = packet.ReadVector3();
                    o = packet.ReadSingle();
                }

                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_UNKNOWN))
                    packet.ReadUInt32();

                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_LOWGUID))
                    lowGuid = packet.ReadUInt32();

                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_HAS_TARGET))
                    targetGuid = packet.ReadPackedGuid();

                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_TRANSPORT))
                    transportTimer = packet.ReadUInt32();

                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_VEHICLE))
                {
                    vehicledID = packet.ReadUInt32();
                    vehicleOrientation = packet.ReadSingle();
                }

                if (flags.HasFlag(ObjectUpdateFlags.UPDATEFLAG_ROTATION))
                    goRotation = packet.ReadInt64();
            }

            void ReadMovementInfo(InPacket packet)
            {
                movementInfo = new MovementInfo(packet);
            }

            private void ReadValuesUpdateData(InPacket packet)
            {
                byte blockCount = packet.ReadByte();
                int[] updateMask = new int[blockCount];
                for (var i = 0; i < blockCount; i++)
                    updateMask[i] = packet.ReadInt32();
                var mask = new BitArray(updateMask);

                for (var i = 0; i < mask.Count; ++i)
                {
                    if (!mask[i])
                        continue;

                    updateFields[i] = packet.ReadUInt32();
                }
            }

            private void HandleUpdateData()
            {
                if (guid == game.Player.GUID)
                {
                    foreach (var pair in updateFields)
                        game.Player[pair.Key] = pair.Value;
                }
                else
                {
                    switch (updateType)
                    {
                        case ObjectUpdateType.UPDATETYPE_VALUES:
                            {
                                WorldObject worldObject = game.Objects[guid];
                                foreach (var pair in updateFields)
                                    worldObject[pair.Key] = pair.Value;
                                break;
                            }
                        case ObjectUpdateType.UPDATETYPE_MOVEMENT:
                            {
                                if (movementInfo != null)
                                {
                                    WorldObject worldObject = game.Objects[guid];
                                    worldObject.Set(movementInfo.Position);
                                    worldObject.O = movementInfo.O;
                                }
                                break;
                            }
                        case ObjectUpdateType.UPDATETYPE_CREATE_OBJECT:
                        case ObjectUpdateType.UPDATETYPE_CREATE_OBJECT2:
                            {
                                WorldObject worldObject = new WorldObject();
                                worldObject.GUID = guid;
                                if (movementInfo != null)
                                {
                                    worldObject.Set(movementInfo.Position);
                                    worldObject.O = movementInfo.O;
                                }
                                worldObject.MapID = game.Player.MapID;
                                foreach (var pair in updateFields)
                                    worldObject[pair.Key] = pair.Value;

#if DEBUG
                                if (game.Objects.ContainsKey(guid))
                                    game.Log(updateType + " called with guid " + guid + " already added", LogLevel.Error);
#endif
                                game.Objects[guid] = worldObject;

                                if (worldObject.IsType(HighGuid.Player))
                                {
                                    OutPacket nameQuery = new OutPacket(WorldCommand.CMSG_NAME_QUERY);
                                    nameQuery.Write(guid);
                                    game.SendPacket(nameQuery);
                                }
                                break;
                            }
                        default:
                            break;
                    }
                }

                foreach (var outOfRangeGuid in outOfRangeGuids)
                {
                    WorldObject worldObject;
                    if (game.Objects.TryGetValue(outOfRangeGuid, out worldObject))
                    {
                        worldObject.ResetPosition();
                        game.Objects.Remove(outOfRangeGuid);
                    }
                }
            }
        }

        [PacketHandler(WorldCommand.SMSG_DESTROY_OBJECT)]
        protected void HandleDestroyObject(InPacket packet)
        {
            ulong guid = packet.ReadUInt64();
            WorldObject worldObject;
            if (Objects.TryGetValue(guid, out worldObject))
            {
                worldObject.ResetPosition();
                Objects.Remove(guid);
            }
        }
        #endregion

        #region Unused Methods
        public override void Log(string message, LogLevel level = LogLevel.Info)
        {
#if DEBUG_LOG
            Console.WriteLine(message);
#endif
        }

        public override void LogLine(string message, LogLevel level = LogLevel.Info)
        {
#if !DEBUG_LOG
            if (level > LogLevel.Debug)
#endif
            Console.WriteLine(Username + " - " + message);
        }

        public override void LogDebug(string message)
        {
            LogLine(message, LogLevel.Debug);
        }

        public override void LogException(string message)
        {
            Console.WriteLine(Username + " - " + message);
        }

        public override void LogException(Exception ex)
        {
            Console.WriteLine(string.Format(Username + " - {0} {1}", ex.Message, ex.StackTrace));
        }

        public IGameUI UI
        {
            get
            {
                return this;
            }
        }

        public override void PresentChatMessage(ChatMessage message)
        {
        }
        #endregion

        #region Triggers Handling
        List<Trigger> Triggers;

        public void AddTrigger(Trigger trigger)
        {
            Triggers.Add(trigger);
        }

        public void AddTriggers(IEnumerable<Trigger> triggers)
        {
            Triggers.AddRange(triggers);
        }

        public void ClearTriggers()
        {
            Triggers.Clear();
        }

        public void ResetTriggers()
        {
            Triggers.ForEach(trigger => trigger.Reset());
        }

        public void HandleTriggerInput(TriggerActionType type, params object[] inputs)
        {
            Triggers.ForEach(trigger => trigger.HandleInput(type, inputs));
        }

        void OnFieldUpdate(object s, UpdateFieldEventArg e)
        {
            HandleTriggerInput(TriggerActionType.UpdateField, e);
        }
        #endregion
    }

    class MovementInfo
    {
        public MovementFlags Flags;
        public MovementFlags2 Flags2;
        public uint Time;
        public Vector3 Position;
        public float O;

        public ulong TransportGuid;
        public Vector3 TransportPosition;
        public float TransportO;
        public ulong TransportTime;
        public byte TransportSeat;
        public ulong TransportTime2;

        public float Pitch;

        public ulong FallTime;

        public float JumpZSpeed;
        public float JumpSinAngle;
        public float JumpCosAngle;
        public float JumpXYSpeed;

        public float SplineElevation;

        public MovementInfo(InPacket packet)
        {
            Flags = (MovementFlags)packet.ReadUInt32();
            Flags2 = (MovementFlags2)packet.ReadUInt16();
            Time = packet.ReadUInt32();
            Position = packet.ReadVector3();
            O = packet.ReadSingle();

            if (Flags.HasFlag(MovementFlags.MOVEMENTFLAG_ONTRANSPORT))
            {
                TransportGuid = packet.ReadPackedGuid();
                TransportPosition = packet.ReadVector3();
                TransportO = packet.ReadSingle();
                TransportTime = packet.ReadUInt32();
                TransportSeat = packet.ReadByte();
                if (Flags2.HasFlag(MovementFlags2.MOVEMENTFLAG2_INTERPOLATED_MOVEMENT))
                    TransportTime2 = packet.ReadUInt32();
            }

            if (Flags.HasFlag(MovementFlags.MOVEMENTFLAG_SWIMMING) || Flags.HasFlag(MovementFlags.MOVEMENTFLAG_FLYING)
                || Flags2.HasFlag(MovementFlags2.MOVEMENTFLAG2_ALWAYS_ALLOW_PITCHING))
                Pitch = packet.ReadSingle();

            FallTime = packet.ReadUInt32();

            if (Flags.HasFlag(MovementFlags.MOVEMENTFLAG_FALLING))
            {
                JumpZSpeed = packet.ReadSingle();
                JumpSinAngle = packet.ReadSingle();
                JumpCosAngle = packet.ReadSingle();
                JumpXYSpeed = packet.ReadSingle();
            }

            if (Flags.HasFlag(MovementFlags.MOVEMENTFLAG_SPLINE_ELEVATION))
                SplineElevation = packet.ReadSingle();
        }
    }
}
