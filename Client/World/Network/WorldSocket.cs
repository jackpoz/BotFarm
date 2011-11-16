using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Client.Authentication;
using Client.UI;
using Client.Chat;
using Client.Chat.Definitions;

namespace Client.World.Network
{
    public partial class WorldSocket : GameSocket
    {
        WorldServerInfo ServerInfo;

        private long transferred;
        public long Transferred { get { return transferred; } }

        private long sent;
        public long Sent { get { return sent; } }

        private long received;
        public long Received { get { return received; } }

        public WorldSocket(IGame program, WorldServerInfo serverInfo)
        {
            Game = program;
            ServerInfo = serverInfo;

            SendLock = new object();
        }

        #region Handler registration

        Dictionary<WorldCommand, PacketHandler> PacketHandlers;

        public override void InitHandlers()
        {
            PacketHandlers = new Dictionary<WorldCommand, PacketHandler>();

            RegisterHandlersFrom(this);
            RegisterHandlersFrom(Game);
        }

        void RegisterHandlersFrom(object obj)
        {
            // create binding flags to discover all non-static methods
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (MethodInfo method in obj.GetType().GetMethods(flags))
            {
                PacketHandlerAttribute[] attributes = (PacketHandlerAttribute[])method.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
                if (attributes.Length == 0)
                    continue;

                PacketHandler handler = (PacketHandler)PacketHandler.CreateDelegate(typeof(PacketHandler), obj, method);

                foreach (PacketHandlerAttribute attribute in attributes)
                {
                    Game.UI.LogLine(string.Format("Registered '{0}.{1}' to '{2}'", obj.GetType().Name, method.Name, attribute.Command), LogLevel.Debug);
                    PacketHandlers[attribute.Command] = handler;
                }
            }
        }

        #endregion

        #region Asynchronous Reading

        int Index;
        int Remaining;

        private void BeginRead(AsyncCallback callback, object state = null)
        {
            this.connection.Client.BeginReceive
            (
                ReceiveData, Index, Remaining,
                SocketFlags.None,
                callback,
                state
            );
        }

        /// <summary>
        /// Determines how large the incoming header will be by
        /// inspecting the first byte, then initiates reading the header.
        /// </summary>
        private void ReadSizeCallback(IAsyncResult result)
        {
            int bytesRead = this.connection.Client.EndReceive(result);
            if (bytesRead == 0 && result.IsCompleted)
            {
                // TODO: world server disconnect
                Game.Exit();
            }

            Interlocked.Increment(ref transferred);
            Interlocked.Increment(ref received);

            AuthenticationCrypto.Decrypt(ReceiveData, 0, 1);
            if ((ReceiveData[0] & 0x80) != 0)
            {
                // need to resize the buffer
                byte temp = ReceiveData[0];
                ReceiveData = new byte[5];
                ReceiveData[0] = (byte)((0x7f & temp));

                Remaining = 4;
            }
            else
                Remaining = 3;

            Index = 1;
            BeginRead(new AsyncCallback(ReadHeaderCallback));
        }

        /// <summary>
        /// Reads the rest of the incoming header.
        /// </summary>
        private void ReadHeaderCallback(IAsyncResult result)
        {
            //if (ReceiveData.Length != 4 && ReceiveData.Length != 5)
              //  throw new Exception("ReceiveData.Length not in order");

            int bytesRead = this.connection.Client.EndReceive(result);
            if (bytesRead == 0 && result.IsCompleted)
            {
                // TODO: world server disconnect
                Game.Exit();
            }

            Interlocked.Add(ref transferred, bytesRead);
            Interlocked.Add(ref received, bytesRead);

            if (bytesRead == Remaining)
            {
                // finished reading header
                // the first byte was decrypted already, so skip it
                AuthenticationCrypto.Decrypt(ReceiveData, 1, ReceiveData.Length - 1);
                ServerHeader header = new ServerHeader(ReceiveData);

                Game.UI.LogLine(header.ToString(), LogLevel.Debug);

                Index = 0;
                Remaining = header.Size;
                ReceiveData = new byte[header.Size];
                BeginRead(new AsyncCallback(ReadPayloadCallback), header);
                return;
            }
            else
            {
                // more header to read
                Index += bytesRead;
                Remaining -= bytesRead;
                BeginRead(new AsyncCallback(ReadHeaderCallback));
            }
        }

        /// <summary>
        /// Reads the payload data.
        /// </summary>
        private void ReadPayloadCallback(IAsyncResult result)
        {
            int bytesRead = this.connection.Client.EndReceive(result);
            if (bytesRead == 0 && result.IsCompleted)
            {
                // TODO: world server disconnect
                Game.Exit();
            }

            Interlocked.Add(ref transferred, bytesRead);
            Interlocked.Add(ref received, bytesRead);

            if (bytesRead == Remaining)
            {
                // get header and packet
                ServerHeader header = (ServerHeader)result.AsyncState;
                InPacket packet = new InPacket(header, ReceiveData);

                PacketHandler handler;
                if (PacketHandlers.TryGetValue((WorldCommand)header.Command, out handler))
                {
                    Game.UI.LogLine(string.Format("Received {0}", header.Command), LogLevel.Debug);

                    if (AuthenticationCrypto.Status == AuthStatus.Ready)
                        // AuthenticationCrypto is ready, handle the packet asynchronously
                        handler.BeginInvoke(packet, null, null);
                    else
                        handler(packet);
                }
                else
                    Game.UI.LogLine(string.Format("Unknown or unhandled command '{0}'", header.Command), LogLevel.Debug);

                // start new asynchronous read
                Start();
            }
            else
            {
                // more payload to read
                Index += bytesRead;
                Remaining -= bytesRead;
                BeginRead(new AsyncCallback(ReadHeaderCallback), result.AsyncState);
            }
        }

        #endregion

        #region GameSocket Members

        public override void Start()
        {
            ReceiveData = new byte[4];
            Index = 0;
            Remaining = 1;
            BeginRead(new AsyncCallback(ReadSizeCallback));
        }

        public override bool Connect()
        {
            try
            {
                Game.UI.Log(string.Format("Connecting to realm {0}... ", ServerInfo.Name));

                connection = new TcpClient(ServerInfo.Address, ServerInfo.Port);

                Game.UI.LogLine("done!");
            }
            catch (SocketException ex)
            {
                Game.UI.LogLine(string.Format("failed. ({0})", (SocketError)ex.ErrorCode), LogLevel.Error);
                return false;
            }

            return true;
        }

        #endregion

        object SendLock;

        public void Send(OutPacket packet)
        {
            byte[] data = packet.Finalize();

            // TODO: switch to asynchronous send
            lock (SendLock)
                connection.Client.Send(data);

            Interlocked.Add(ref transferred, data.Length);
            Interlocked.Add(ref sent, data.Length);
        }
    }
}
