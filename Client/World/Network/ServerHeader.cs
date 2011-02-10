using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
			else
			{
				Size = (int)((((uint)data[0]) << 16) | (((uint)data[1]) << 8) | data[2]);
				Command = (WorldCommand)BitConverter.ToUInt16(data, 3);
			}

			// decrement since we already have command's two bytes
			Size -= 2;
		}
	}
}
