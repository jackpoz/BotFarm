using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.World.Network
{
    public interface Packet
    {
        Header Header { get; }
    }
}
