using System.IO;

namespace Client.Authentication.Network
{
	interface ISendable
	{
		void Send(BinaryWriter writer);
	}
}
