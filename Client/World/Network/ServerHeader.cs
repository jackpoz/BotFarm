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

        internal ServerHeader(byte[] data)
        {
            if (data.Length == 4)
            {
                Size = (int)(((uint)data[0]) << 8 | data[1]);
                Command = (WorldCommand)BitConverter.ToUInt16(data, 2);
            }
            else //if (data.Length == 5)
            {
                Size = (int)(((((uint)data[0]) &~ 0x80) << 16) & 0xFF | (((uint)data[1]) << 8) | data[2]);
                Command = (WorldCommand)BitConverter.ToUInt16(data, 3);
            }
//             else
//             {
//                 Console.WriteLine("Header Data Length {0}", data.Length);
//                 return;
            Console.WriteLine("Command {0} Header Size {1} Packet Size {2}", Command, data.Length, Size);            
            // decrement since we already have command's two bytes
            Size -= 2;
        }
    }
}
