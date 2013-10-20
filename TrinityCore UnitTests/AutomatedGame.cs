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

namespace TrinityCore_UnitTests
{
    class AutomatedGame : IGame, IGameUI, IDisposable
    {
        bool Running;

        GameSocket socket;

        public BigInteger Key { get; private set; }
        public string Username { get; private set; }
        public bool LoggedIn { get; private set; }
        public int RealmID { get; private set; }
        public int Character { get; private set; }

        Queue<Action> scheduledActions;

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
            scheduledActions = new Queue<Action>();
            World = new GameWorld();

            this.Username = username;

            socket = new AuthSocket(this, hostname, port, username, password);
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
                socket.Start();
            else
                Exit();
        }

        public void Start()
        {
            // the initial socket is an AuthSocket - it will initiate its own asynch read
            Running = socket.Connect();

            Task.Run(() =>
                {
                    while (Running)
                    {
                        // main loop here
                        Update();
                        Thread.Sleep(500);
                    }
                });
        }

        public void Update()
        {
            if (World.SelectedCharacter == null)
                return;

            if (scheduledActions.Count == 0)
                return;

            var action = scheduledActions.Dequeue();
            action();
        }

        public void Exit()
        {
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

        public void Enqueue(Action action)
        {
            scheduledActions.Enqueue(action);
        }

        #region Unused Methods
        public void Log(string message, LogLevel level = LogLevel.Info)
        {
        }

        public void LogLine(string message, LogLevel level = LogLevel.Info)
        {
        }

        public void LogException(string message)
        {
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
            while (scheduledActions.Count > 0)
                Thread.Sleep(1000);

            Exit();

            Thread.Sleep(1000);

            if (socket != null)
                socket.Dispose();
        }
    }
}
