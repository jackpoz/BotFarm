using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Client.Authentication.Network;

namespace Client.Authentication
{
    struct ClientAuthProof : ISendable
    {
        public byte[] A;
        public byte[] M1;
        public byte[] crc;

        #region ISendable Members

        public void Send(BinaryWriter writer)
        {
            writer.Write((byte)AuthCommand.LOGON_PROOF);
            writer.Write(A);
            writer.Write(M1);
            writer.Write(crc);
            writer.Write((byte)0);
            writer.Write((byte)0);
        }

        #endregion
    }
}
