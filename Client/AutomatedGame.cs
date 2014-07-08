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

namespace Client
{
    public class AutomatedGame : IGameUI, IGame, IDisposable
    {
        #region Properties
        public bool Running { get; private set; }
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
        ScheduledActions scheduledActions;
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
        #endregion

        public AutomatedGame(string hostname, int port, string username, string password, int realmId, int character)
        {
            this.RealmID = realmId;
            this.Character = character;
            scheduledActions = new ScheduledActions();
            World = new GameWorld();

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
                    scheduledAction.action();
                    if (scheduledAction.interval > TimeSpan.Zero)
                        ScheduleAction(scheduledAction.action, DateTime.Now + scheduledAction.interval, scheduledAction.interval);
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
                socket = new AuthSocket(this, Hostname, Port, Username, Password);
                socket.InitHandlers();
                // exit from loop if the socket connected successfully
                if (socket.Connect())
                    break;

                // try again later
                Thread.Sleep(10000);
            }
        }

        public override void Exit()
        {
            Connected = false;
            LoggedIn = false;
            Running = false;
        }

        public void SendPacket(OutPacket packet)
        {
            if (socket is WorldSocket)
                ((WorldSocket)socket).Send(packet);
        }

        public override void PresentRealmList(WorldServerList realmList)
        {
            ConnectTo(realmList[RealmID]);
        }

        public override void PresentCharacterList(Character[] characterList)
        {
            World.SelectedCharacter = characterList[Character];
            OutPacket packet = new OutPacket(WorldCommand.CMSG_PLAYER_LOGIN);
            packet.Write(World.SelectedCharacter.GUID);
            SendPacket(packet);
            LoggedIn = true;
            Player = new Player();
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

        public void ScheduleAction(Action action, TimeSpan interval = default(TimeSpan))
        {
            ScheduleAction(action, DateTime.Now, interval);
        }

        public void ScheduleAction(Action action, DateTime time, TimeSpan interval = default(TimeSpan))
        {
            scheduledActions.Add(new RepeatingAction(action, time, interval));
        }

        public void CreateCharacter()
        {
            OutPacket createCharacterPacket = new OutPacket(WorldCommand.CMSG_CHAR_CREATE);
            StringBuilder charName = new StringBuilder("Bot");
            foreach (char c in Username.Substring(3))
	        {
                charName.Append((char)(97 + int.Parse(c.ToString())));
	        }
            charName.Length = 12;
            createCharacterPacket.Write(charName.ToString().ToCString());
            byte race = 1; createCharacterPacket.Write(race);
            byte _class = 5; createCharacterPacket.Write(_class);
            byte gender = 0; createCharacterPacket.Write(gender);
            byte skin = 6; createCharacterPacket.Write(skin);
            byte face = 5; createCharacterPacket.Write(face);
            byte hairStyle = 0; createCharacterPacket.Write(hairStyle);
            byte hairColor = 1; createCharacterPacket.Write(hairColor);
            byte facialHair = 5; createCharacterPacket.Write(facialHair);
            byte outfitId = 0; createCharacterPacket.Write(outfitId);

            SendPacket(createCharacterPacket);
        }

        public void Dispose()
        {
            scheduledActions.Clear();

            Exit();

            Thread.Sleep(1000);

            if (socket != null)
                socket.Dispose();
        }

        public virtual void NoCharactersFound()
        { }
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
        public void MoveTo(Position destination)
        {
            throw new NotImplementedException();
        }

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
    }

    public class RepeatingAction
    {
        public Action action
        {
            get;
            set;
        }

        public DateTime scheduledTime
        {
            get;
            set;
        }

        public TimeSpan interval
        {
            get;
            set;
        }

        public RepeatingAction(Action action, DateTime scheduledTime, TimeSpan interval)
        {
            this.action = action;
            this.scheduledTime = scheduledTime;
            this.interval = interval;
        }
    }

    public class ScheduledActions : IList<RepeatingAction>
    {
        List<RepeatingAction> actions;

        public ScheduledActions()
        {
            actions = new List<RepeatingAction>();
        }

        public int IndexOf(RepeatingAction item)
        {
            return actions.IndexOf(item);
        }

        void IList<RepeatingAction>.Insert(int index, RepeatingAction item)
        {
            Add(item);
        }

        public void RemoveAt(int index)
        {
            actions.RemoveAt(index);
        }

        public RepeatingAction this[int index]
        {
            get
            {
                return actions[index];
            }
            set
            {
                actions[index] = value;
                Sort();

            }
        }

        public void Add(RepeatingAction item)
        {
            actions.Add(item);
            Sort();
        }

        public void Clear()
        {
            actions.Clear();
        }

        public bool Contains(RepeatingAction item)
        {
            return actions.Contains(item);
        }

        public void CopyTo(RepeatingAction[] array, int arrayIndex)
        {
            actions.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get 
            {
                return actions.Count;
            }
        }

        public bool IsReadOnly
        {
            get 
            {
                return false;
            }
        }

        public bool Remove(RepeatingAction item)
        {
            return actions.Remove(item);
        }

        public IEnumerator<RepeatingAction> GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        void Sort()
        {
            actions.Sort((a, b) => (int)(a.scheduledTime - b.scheduledTime).TotalMilliseconds);
        }
    }
}
