using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Client
{
    abstract class GameSocket : IDisposable
    {
        public IGame Game { get; protected set; }

        protected TcpClient connection { get; set; }

        public bool IsConnected
        {
            get { return connection.Connected; }
        }

        #region Asynchronous Reading

        protected byte[] ReceiveData;

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
