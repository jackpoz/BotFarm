using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.World.Network
{
    public interface Header
    {
        WorldCommand Command { get; }
        int Size { get; }
    }
}
