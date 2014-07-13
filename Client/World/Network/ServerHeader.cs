using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.World.Network
{
    public class ServerHeader : Header
    {
        public WorldCommand Command { get; private set; }
        public int Size { get; private set; }
        public int InputDataLength { get; private set; }

        internal ServerHeader(byte[] data, int dataLength)
        {
            InputDataLength = dataLength;
            if (InputDataLength == 4)
            {
                Size = (int)(((uint)data[0]) << 8 | data[1]);
                Command = (WorldCommand)BitConverter.ToUInt16(data, 2);
            }
            else if (InputDataLength == 5)
            {
                Size = (int)(((((uint)data[0]) & 0x7F) << 16) | (((uint)data[1]) << 8) | data[2]);
                Command = (WorldCommand)BitConverter.ToUInt16(data, 3);
            }
            else
                return;
            
            // decrement since we already have command's two bytes
            Size -= 2;
        }

        public override string ToString()
        {
            return String.Format("Command {0} Header Size {1} Packet Size {2}", Command, InputDataLength, Size);
        }
    }
}
