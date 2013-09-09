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

namespace TrinityCore_UnitTests
{
    class AutomatedGame : IGame, IGameUI
    {
        bool Running;

        GameSocket socket;

        public BigInteger Key { get; private set; }
        public string Username { get; private set; }

        Queue<Action> scheduledActions;

        public GameWorld World
        {
            get { return _world; }
            private set { _world = value; }
        }

        private GameWorld _world;

        public AutomatedGame(string hostname, int port, string username, string password)
        {
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
                        Thread.Sleep(100);
                    }
                });
        }

        public void Update()
        {
            if (Game.World.SelectedCharacter == null)
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
            Game.ConnectTo(realmList.First());
        }

        public void PresentCharacterList(Character[] characterList)
        {
            Game.World.SelectedCharacter = characterList.First();
            OutPacket packet = new OutPacket(WorldCommand.CMSG_PLAYER_LOGIN);
            packet.Write(Game.World.SelectedCharacter.GUID);
            Game.SendPacket(packet);
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
    }
}
