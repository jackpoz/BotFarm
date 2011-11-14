using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Client.Authentication.Network;

namespace Client.Authentication
{
    struct ClientAuthChallenge : ISendable
    {
        public string username;
        public uint IP;

        static readonly byte[] version = new byte[] { 3, 3, 5 };
        const ushort build = 12340;

        #region ISendable Members

        public void Send(BinaryWriter writer)
        {
            writer.Write((byte)AuthCommand.LOGON_CHALLENGE);
            writer.Write((byte)6);
            writer.Write((byte)(username.Length + 30));
            writer.Write((byte)0);
            writer.Write("WoW".ToCString());
            writer.Write(version);
            writer.Write(build);
            writer.Write("68x".ToCString());
            writer.Write("niW".ToCString());
            writer.Write(Encoding.ASCII.GetBytes("SUne"));
            writer.Write((uint)0x3c);
            writer.Write(IP);
            writer.Write((byte)username.Length);
            writer.Write(Encoding.ASCII.GetBytes(username));
        }

        #endregion
    }
}
