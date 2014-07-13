using System.IO;

namespace Client.World.Network
{
    public class InPacket : BinaryReader, Packet
    {
        public Header Header { get; private set; }

        internal InPacket(ServerHeader header)
            : this(header, new byte[] { }, 0)
        {

        }

        internal InPacket(ServerHeader header, byte[] buffer, int bufferLength)
            : base(new MemoryStream(buffer, 0, bufferLength, false, false))
        {
            Header = header;
        }

        public ulong ReadPackedGuid()
        {
            var mask = ReadByte();

            if (mask == 0)
                return (ulong)0;

            ulong res = 0;

            var i = 0;
            while (i < 8)
            {
                if ((mask & 1 << i) != 0)
                    res += (ulong)ReadByte() << (i * 8);

                i++;
            }

            return res;
        }

        public override string ToString()
        {
            return Header.Command.ToString();
        }
    }
}
