using System;

namespace Client.World.Network
{
	class ClientHeader : Header
	{
		public WorldCommand Command { get; private set; }
		public byte[] EncryptedCommand
		{
			get
			{
				byte[] data = BitConverter.GetBytes((uint)this.Command);
				AuthenticationCrypto.Encrypt(data, 0, data.Length);
				return data;
			}
		}

		public int Size { get { return (int)Packet.BaseStream.Length + 4; } }
		public byte[] EncryptedSize
		{
			get
			{
				byte[] data = BitConverter.GetBytes(this.Size).SubArray(0, 2);
				AuthenticationCrypto.Encrypt(data, 0, 2);
				return data;
			}
		}

		private OutPacket Packet;

		public ClientHeader(WorldCommand command, OutPacket packet)
		{
			this.Command = command;
			Packet = packet;
		}
	}
}
