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

        private int _inputDataLen = 0;

        internal ServerHeader(byte[] data)
        {
            _inputDataLen = data.Length;
            if (_inputDataLen == 4)
            {
                Size = (int)(((uint)data[0]) << 8 | data[1]);
                Command = (WorldCommand)BitConverter.ToUInt16(data, 2);
            }
            else if (_inputDataLen == 5)
            {
                Size = (int)(((((uint)data[0]) &~ 0x80) << 16) & 0xFF | (((uint)data[1]) << 8) | data[2]);
                Command = (WorldCommand)BitConverter.ToUInt16(data, 3);
            }
            else
                throw new Exception(String.Format("Unsupported header size {0}", _inputDataLen));
            
            // decrement since we already have command's two bytes
            Size -= 2;
        }

        public override string ToString()
        {
            return String.Format("Command {0} Header Size {1} Packet Size {2}", Command, _inputDataLen, Size);
        }
    }
}
