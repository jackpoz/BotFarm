using System;
using Client.World.Network;

namespace Client.World
{
    public delegate void PacketHandler(InPacket packet);

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class PacketHandlerAttribute : Attribute
    {
        public WorldCommand Command { get; private set; }

        public PacketHandlerAttribute(WorldCommand command)
        {
            Command = command;
        }
    }
}
