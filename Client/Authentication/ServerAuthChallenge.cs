using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Client.Authentication
{
    struct ServerAuthChallenge
    {
        public readonly AuthCommand command;
        public readonly byte unk2;
        public readonly AuthResult error;
        public readonly byte[] B;
        public readonly byte gLen;
        public readonly byte[] g;
        public readonly byte nLen;
        public readonly byte[] N;
        public readonly byte[] salt;
        public readonly byte[] unk3;
        public readonly byte securityFlags;

        public ServerAuthChallenge(BinaryReader reader)
            : this()
        {
            command = AuthCommand.LOGON_CHALLENGE;
            unk2 = reader.ReadByte();
            error = (AuthResult)reader.ReadByte();
            if (error != AuthResult.SUCCESS)
                return;

            B = reader.ReadBytes(32);
            gLen = reader.ReadByte();
            g = reader.ReadBytes(1);
            nLen = reader.ReadByte();
            N = reader.ReadBytes(32);
            salt = reader.ReadBytes(32);
            unk3 = reader.ReadBytes(16);
            securityFlags = reader.ReadByte();
        }
    }
}
