using System;
using Client.World.Network;

namespace Client.World
{
	public delegate void PacketHandler(InPacket packet);

	public class PacketHandlerAttribute : Attribute
	{
		public WorldCommand Command { get; private set; }

		public PacketHandlerAttribute(WorldCommand command)
		{
			Command = command;
		}
	}
}
