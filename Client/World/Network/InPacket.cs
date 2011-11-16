using System.IO;

namespace Client.World.Network
{
    public class InPacket : BinaryReader, Packet
    {
        public Header Header { get; private set; }

        internal InPacket(ServerHeader header)
            : this(header, new byte[] { })
        {

        }

        internal InPacket(ServerHeader header, byte[] buffer)
            : base(new MemoryStream(buffer))
        {
            Header = header;
        }
    }
}
