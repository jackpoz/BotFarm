using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Client.Authentication.Network;
using System.Net.Sockets;

namespace Client.Authentication
{
    struct ClientAuthProof : ISendable
    {
        public byte[] A;
        public byte[] M1;
        public byte[] crc;

        #region ISendable Members

        public void Send(NetworkStream writer)
        {
            using(var stream = new MemoryStream(1 + A.Length + M1.Length + crc.Length + 2))
            {
                var binaryStream = new BinaryWriter(stream);
                binaryStream.Write((byte)AuthCommand.LOGON_PROOF);
                binaryStream.Write(A);
                binaryStream.Write(M1);
                binaryStream.Write(crc);
                binaryStream.Write((byte)0);
                binaryStream.Write((byte)0);
                stream.Seek(0, SeekOrigin.Begin);
                var buffer = stream.ToArray();
                writer.Write(buffer, 0, buffer.Length);
            }
        }

        #endregion
    }
}
