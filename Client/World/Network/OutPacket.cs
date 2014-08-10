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

        public ushort flags2
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
                Write(flags2);
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

    [Flags]
    public enum MovementFlags
    {
        MOVEMENTFLAG_NONE = 0x00000000,
        MOVEMENTFLAG_FORWARD = 0x00000001,
        MOVEMENTFLAG_BACKWARD = 0x00000002,
        MOVEMENTFLAG_STRAFE_LEFT = 0x00000004,
        MOVEMENTFLAG_STRAFE_RIGHT = 0x00000008,
        MOVEMENTFLAG_LEFT = 0x00000010,
        MOVEMENTFLAG_RIGHT = 0x00000020,
        MOVEMENTFLAG_PITCH_UP = 0x00000040,
        MOVEMENTFLAG_PITCH_DOWN = 0x00000080,
        MOVEMENTFLAG_WALKING = 0x00000100,
        MOVEMENTFLAG_ONTRANSPORT = 0x00000200,
        MOVEMENTFLAG_DISABLE_GRAVITY = 0x00000400,
        MOVEMENTFLAG_ROOT = 0x00000800,
        MOVEMENTFLAG_FALLING = 0x00001000,
        MOVEMENTFLAG_FALLING_FAR = 0x00002000,
        MOVEMENTFLAG_PENDING_STOP = 0x00004000,
        MOVEMENTFLAG_PENDING_STRAFE_STOP = 0x00008000,
        MOVEMENTFLAG_PENDING_FORWARD = 0x00010000,
        MOVEMENTFLAG_PENDING_BACKWARD = 0x00020000,
        MOVEMENTFLAG_PENDING_STRAFE_LEFT = 0x00040000,
        MOVEMENTFLAG_PENDING_STRAFE_RIGHT = 0x00080000,
        MOVEMENTFLAG_PENDING_ROOT = 0x00100000,
        MOVEMENTFLAG_SWIMMING = 0x00200000,
        MOVEMENTFLAG_ASCENDING = 0x00400000,
        MOVEMENTFLAG_DESCENDING = 0x00800000,
        MOVEMENTFLAG_CAN_FLY = 0x01000000,
        MOVEMENTFLAG_FLYING = 0x02000000,
        MOVEMENTFLAG_SPLINE_ELEVATION = 0x04000000,
        MOVEMENTFLAG_SPLINE_ENABLED = 0x08000000,
        MOVEMENTFLAG_WATERWALKING = 0x10000000,
        MOVEMENTFLAG_FALLING_SLOW = 0x20000000,
        MOVEMENTFLAG_HOVER = 0x40000000,
    };
}
