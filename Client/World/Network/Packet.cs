using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.World.Network
{
	interface Packet
	{
		Header Header { get; }
	}
}
