using System.IO;
using System.Net.Sockets;

namespace Client.Authentication.Network
{
    interface ISendable
    {
        AuthCommand Command { get; }

        void Send(NetworkStream writer);
    }
}
