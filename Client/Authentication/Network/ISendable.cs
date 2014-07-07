using System.IO;
using System.Net.Sockets;

namespace Client.Authentication.Network
{
    interface ISendable
    {
        void Send(NetworkStream writer);
    }
}
