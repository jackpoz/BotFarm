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
        const int DefaultBufferSize = 128;
        public GameSocket()
        {
            SocketArgs = new SocketAsyncEventArgs();
            SocketArgs.Completed += CallSocketCallback;
            _receiveData = new byte[DefaultBufferSize];
            ReceiveDataLength = DefaultBufferSize;
        }

        public IGame Game { get; protected set; }

        protected TcpClient connection { get; set; }

        public AuthenticationCrypto authenticationCrypto = new AuthenticationCrypto();

        public bool IsConnected
        {
            get { return connection.Connected; }
        }

        public bool Disposed
        {
            get;
            private set;
        }

        protected bool Disposing
        {
            get;
            private set;
        }

        #region Asynchronous Reading

        protected byte[] ReceiveData
        {
            get
            {
                return _receiveData;
            }
        }
        private byte[] _receiveData;
        protected int ReceiveDataLength;
        protected void ReserveData(int size, bool reset = false)
        {
            if (reset)
                _receiveData = new byte[DefaultBufferSize];
            if (_receiveData.Length < size)
                Array.Resize(ref _receiveData, size);
            ReceiveDataLength = size;
        }


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
            Disposing = true;
            if (!Disposed)
                Disconnect();
        }

        public void Disconnect()
        {
            if (connection != null)
            {
                if (connection.Connected)
                {
                    try
                    {
                        connection.Client.Shutdown(SocketShutdown.Send);
                        connection.Client.BeginReceive(_receiveData, 0, _receiveData.Length, SocketFlags.None, SocketShutdownCallback, null);
                    }
                    catch(SocketException)
                    {
                        Disposed = true;
                        connection.Close();
                    }
                }
                else
                {
                    Disposed = true;
                    connection.Close();
                }
            }
        }

        void SocketShutdownCallback(IAsyncResult result)
        {
            int size = connection.Client.EndReceive(result);
            if (size > 0)
                connection.Client.BeginReceive(_receiveData, 0, _receiveData.Length, SocketFlags.None, SocketShutdownCallback, null);
            else
            {
                Disposed = true;
                connection.Close();
            }
        }

        public abstract void InitHandlers();

        public abstract string LastInOpcodeName
        {
            get;
        }

        public abstract DateTime LastInOpcodeTime
        {
            get;
        }

        public abstract string LastOutOpcodeName
        {
            get;
        }

        public abstract DateTime LastOutOpcodeTime
        {
            get;
        }
    }
}
