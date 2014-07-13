using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Client.World.Network;

namespace Client
{
    public abstract class GameSocket : IDisposable
    {
        public GameSocket()
        {
            SocketArgs = new SocketAsyncEventArgs();
            SocketArgs.Completed += CallSocketCallback;
        }

        public IGame Game { get; protected set; }

        protected TcpClient connection { get; set; }

        public AuthenticationCrypto authenticationCrypto = new AuthenticationCrypto();

        public bool IsConnected
        {
            get { return connection.Connected; }
        }

        #region Asynchronous Reading

        //ToDo: find a way to avoid creating new buffers every time
        protected byte[] ReceiveData;

        protected SocketAsyncEventArgs SocketArgs;
        protected object SocketAsyncState;
        protected EventHandler<SocketAsyncEventArgs> SocketCallback;
        private void CallSocketCallback(object sender, SocketAsyncEventArgs e)
        {
            if (SocketCallback != null)
                SocketCallback(sender, e);
        }

        public abstract void Start();

        #endregion

        public abstract bool Connect();

        public void Dispose()
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (connection != null)
                connection.Close();
        }

        public abstract void InitHandlers();
    }
}
