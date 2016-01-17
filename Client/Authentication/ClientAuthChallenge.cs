using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Client.Authentication.Network;
using System.Net.Sockets;

namespace Client.Authentication
{
    struct ClientAuthChallenge : ISendable
    {
        public string username;
        public uint IP;

        static readonly byte[] version = new byte[] { 3, 3, 5 };
        const ushort build = 12340;

        #region ISendable Members
        public AuthCommand Command
        {
            get
            {
                return AuthCommand.LOGON_CHALLENGE;
            }
        }

        public void Send(NetworkStream writer)
        {
            using (var stream = new MemoryStream(128))
            {
                var binaryStream = new BinaryWriter(stream);
                binaryStream.Write((byte)Command);
                binaryStream.Write((byte)6);
                binaryStream.Write((UInt16)(username.Length + 30));
                binaryStream.Write("WoW".ToCString());
                binaryStream.Write(version);
                binaryStream.Write(build);
                binaryStream.Write("68x".ToCString());
                binaryStream.Write("niW".ToCString());
                binaryStream.Write(Encoding.ASCII.GetBytes("SUne"));
                binaryStream.Write((uint)0x3c);
                binaryStream.Write(IP);
                binaryStream.Write((byte)username.Length);
                binaryStream.Write(Encoding.ASCII.GetBytes(username));
                stream.Seek(0, SeekOrigin.Begin);
                var buffer = stream.ToArray();
                writer.Write(buffer, 0, buffer.Length);
            }
        }

        #endregion
    }
}
