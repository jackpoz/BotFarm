using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.World.Network
{
    enum CommandDetail : byte
    {
        AuthSuccess = 0x0C,
        AuthQueue,
        AuthFailure
    }
}
