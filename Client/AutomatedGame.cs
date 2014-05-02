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

namespace Client
{
    public class AutomatedGame : IGame, IGameUI, IDisposable
    {
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

        SortedList<DateTime, RepeatingAction> scheduledActions;

        public GameWorld World
        {
            get { return _world; }
            private set { _world = value; }
        }

        private GameWorld _world;

        public AutomatedGame(string hostname, int port, string username, string password, int realmId, int character)
        {
            this.RealmID = realmId;
            this.Character = character;
            scheduledActions = new SortedList<DateTime, RepeatingAction>();
            World = new GameWorld();

            this.Hostname = hostname;
            this.Port = port;
            this.Username = username;
            this.Password = password;

            socket = new AuthSocket(this, Hostname, Port, Username, Password);
            socket.InitHandlers();
        }

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
                        await Task.Delay(500);
                    }
                });
        }

        public void Update()
        {
            if (World.SelectedCharacter == null)
                return;

            if (scheduledActions.Count == 0)
                return;

            // execute only 1 scheduled action at time in each Update().
            // actions are sorted by time so it's enough to check the first in the list
            var scheduledAction = scheduledActions.First();
            if (scheduledAction.Key <= DateTime.Now)
            {
                scheduledActions.RemoveAt(0);
                scheduledAction.Value.action();
                if (scheduledAction.Value.interval > TimeSpan.Zero)
                    ScheduleAction(scheduledAction.Value.action, DateTime.Now + scheduledAction.Value.interval, scheduledAction.Value.interval);
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

        public void Exit()
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

        public IGame Game
        {
            get
            {
                return this;
            }
            set
            {
            }
        }

        public LogLevel LogLevel
        {
            get
            {
                return Client.UI.LogLevel.Error;
            }
            set
            {
            }
        }

        public void PresentRealmList(WorldServerList realmList)
        {
            ConnectTo(realmList[RealmID]);
        }

        public void PresentCharacterList(Character[] characterList)
        {
            World.SelectedCharacter = characterList[Character];
            OutPacket packet = new OutPacket(WorldCommand.CMSG_PLAYER_LOGIN);
            packet.Write(World.SelectedCharacter.GUID);
            SendPacket(packet);
            LoggedIn = true;
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }

        public int Read()
        {
            throw new NotImplementedException();
        }

        public ConsoleKeyInfo ReadKey()
        {
            throw new NotImplementedException();
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
            Thread.Sleep(500);
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

        public void ScheduleAction(Action action, TimeSpan interval = default(TimeSpan))
        {
            ScheduleAction(action, DateTime.Now, interval);
        }

        public void ScheduleAction(Action action, DateTime time, TimeSpan interval = default(TimeSpan))
        {
            scheduledActions.Add(time, new RepeatingAction(action, interval));
        }

        #region Handlers
        public void DoTextEmote(TextEmote emote)
        {
            var packet = new OutPacket(WorldCommand.CMSG_TEXT_EMOTE);
            packet.Write((uint)emote);
            packet.Write((uint)0);
            packet.Write((ulong)0);
            SendPacket(packet);
        }
        #endregion

        #region Unused Methods
        public virtual void Log(string message, LogLevel level = LogLevel.Info)
        {
#if DEBUG_LOG
            Console.WriteLine(message);
#endif
        }

        public virtual void LogLine(string message, LogLevel level = LogLevel.Info)
        {
#if !DEBUG_LOG
            if (level > LogLevel.Debug)
#endif
            Console.WriteLine(Username + " - " + message);
        }

        public virtual void LogException(string message)
        {
            Console.WriteLine(Username + " - " + message);
        }

        public virtual void LogException(Exception ex)
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

        public void PresentChatMessage(ChatMessage message)
        {
        }
        #endregion

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
    }

    public class RepeatingAction
    {
        public Action action
        {
            get;
            set;
        }

        public TimeSpan interval
        {
            get;
            set;
        }

        public RepeatingAction(Action action, TimeSpan interval)
        {
            this.action = action;
            this.interval = interval;
        }
    }
}
