using Client.World.Definitions;
using Client.World.Entities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Client.World.Network
{
    public class OutPacket : BinaryWriter, Packet
    {
        public Header Header { get; private set; }

        protected readonly MemoryStream Buffer;
        protected byte[] FinalizedPacket;

        public OutPacket(WorldCommand command, int emptyOffset = 0)
            : base()
        {
            this.Header = new ClientHeader(command, this);

            Buffer = new MemoryStream();
            base.OutStream = Buffer;

            if (emptyOffset > 0)
                Write(new byte[emptyOffset]);
        }

        public virtual byte[] Finalize(AuthenticationCrypto authenticationCrypto)
        {
            if (FinalizedPacket == null)
            {
                byte[] data = new byte[6 + Buffer.Length];
                byte[] size = ((ClientHeader)Header).EncryptedSize(authenticationCrypto);
                byte[] command = ((ClientHeader)Header).EncryptedCommand(authenticationCrypto);

                Array.Copy(size, 0, data, 0, 2);
                Array.Copy(command, 0, data, 2, 4);
                Array.Copy(Buffer.ToArray(), 0, data, 6, Buffer.Length);

                FinalizedPacket = data;
            }

            return FinalizedPacket;
        }

        public void WritePacketGuid(UInt64 guid)
        {
            byte[] packGUID = new byte[8+1];
            packGUID[0] = 0;
            var size = 1;
            for (byte i = 0;guid != 0;++i)
            {
                if ((guid & 0xFF) != 0)
                {
                    packGUID[0] |= (byte)(1 << i);
                    packGUID[size] =  (byte)(guid & 0xFF);
                    ++size;
                }

                guid >>= 8;
            }
            Write(packGUID.Take(size).ToArray());
        }

        public override string ToString()
        {
            return Header.Command.ToString();
        }
    }

    public class MovementPacket : OutPacket
    {
        public ulong GUID
        {
            get;
            set;
        }

        public MovementFlags flags
        {
            get;
            set;
        }

        public MovementFlags2 flags2
        {
            get;
            set;
        }

        public uint time
        {
            get;
            set;
        }

        public float X
        {
            get;
            set;
        }

        public float Y
        {
            get;
            set;
        }

        public float Z
        {
            get;
            set;
        }

        public float O
        {
            get;
            set;
        }

        public uint fallTime
        {
            get;
            set;
        }

        public MovementPacket(WorldCommand command)
            : base(command)
        {
            time = (uint)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
        }

        public override byte[] Finalize(AuthenticationCrypto authenticationCrypto)
        {
            if (Buffer.Length == 0)
            {
                WritePacketGuid(GUID);
                Write((uint)flags);
                Write((ushort)flags2);
                Write(time);
                Write(X);
                Write(Y);
                Write(Z);
                Write(O);
                Write(fallTime);
            }

            return base.Finalize(authenticationCrypto);
        }

        public Position GetPosition()
        {
            return new Position(X, Y, Z, O, Position.INVALID_MAP_ID);
        }
    }
}
