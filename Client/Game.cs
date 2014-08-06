using System.Numerics;
using System.Threading;
using Client.Authentication;
using Client.Authentication.Network;
using Client.UI;
using Client.World.Network;
using Client.Chat;
using System.Collections.Generic;

namespace Client
{
    public interface IGame
    {
        BigInteger Key { get; }
        string Username { get; }

        IGameUI UI { get; }

        GameWorld World { get; }

        void ConnectTo(WorldServerInfo server);

        void Start();

        void Reconnect();

        void NoCharactersFound();

        void InvalidCredentials();

        void Exit();

        void SendPacket(OutPacket packet);
    }

    public class Game<T> : IGame
        where T : IGameUI, new()
    {
        bool Running;

        GameSocket socket;

        public BigInteger Key { get; private set; }
        public string Username { get; private set; }

        public IGameUI UI { get; protected set; }

        public GameWorld World
        {
            get { return _world; }
            private set { _world = value; }
        }

        private GameWorld _world;

        public Game(string hostname, int port, string username, string password, LogLevel logLevel)
        {
            UI = new T();
            UI.Game = this;
            UI.LogLevel = logLevel;

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

            while (Running)
            {
                // main loop here
                UI.Update();
                Thread.Sleep(100);
            }

            UI.Exit();
        }

        public void Reconnect()
        {
            Exit();
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

        public void NoCharactersFound()
        { }

        public void InvalidCredentials()
        { }
    }
}