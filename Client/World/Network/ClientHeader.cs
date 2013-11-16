using System;

namespace Client.World.Network
{
    class ClientHeader : Header
    {
        public WorldCommand Command { get; private set; }
        private byte[] encryptedCommand;
        public byte[] EncryptedCommand(AuthenticationCrypto authenticationCrypto)
        {
            if (encryptedCommand == null)
            {
                encryptedCommand = BitConverter.GetBytes((uint)this.Command);
                authenticationCrypto.Encrypt(encryptedCommand, 0, encryptedCommand.Length);
            }

            return encryptedCommand;
        }

        public int Size { get { return (int)Packet.BaseStream.Length + 4; } }

        private byte[] encryptedSize;
        public byte[] EncryptedSize (AuthenticationCrypto authenticationCrypto)
        {
            if (encryptedSize == null)
            {
                encryptedSize = BitConverter.GetBytes(this.Size).SubArray(0, 2);
                Array.Reverse(encryptedSize);
                authenticationCrypto.Encrypt(encryptedSize, 0, 2);
            }

            return encryptedSize;
        }

        private OutPacket Packet;

        public ClientHeader(WorldCommand command, OutPacket packet)
        {
            this.Command = command;
            Packet = packet;
        }
    }
}
