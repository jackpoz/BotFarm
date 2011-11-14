using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Client.Authentication
{
    struct ServerAuthProof
    {
        public AuthCommand command;
        public AuthResult error;
        public readonly byte[] M2;
        public uint unk1;
        public uint unk2;
        public ushort unk3;

        public ServerAuthProof(BinaryReader reader)
            : this()
        {
            command = AuthCommand.LOGON_PROOF;
            error = (AuthResult)reader.ReadByte();
            if (error != AuthResult.SUCCESS)
            {
                reader.ReadUInt16();
                return;
            }

            M2 = reader.ReadBytes(20);
            unk1 = reader.ReadUInt32();
            unk2 = reader.ReadUInt32();
            unk3 = reader.ReadUInt16();
        }
    }
}
