using System;
using System.Numerics;
using System.Text;
using Client.Crypto;

namespace Client.World.Network
{
    public partial class WorldSocket
    {
        [PacketHandler(WorldCommand.ServerAuthChallenge)]
        protected void HandleServerAuthChallenge(InPacket packet)
        {
            uint one = packet.ReadUInt32();
            uint seed = packet.ReadUInt32();

            BigInteger seed1 = packet.ReadBytes(16).ToBigInteger();
            BigInteger seed2 = packet.ReadBytes(16).ToBigInteger();

            var rand = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rand.GetBytes(bytes);
            BigInteger ourSeed = bytes.ToBigInteger();

            uint zero = 0;

            byte[] authResponse = HashAlgorithm.SHA1.Hash
            (
                Encoding.ASCII.GetBytes(Game.Username.ToUpper()),
                BitConverter.GetBytes(zero),
                BitConverter.GetBytes((uint)ourSeed),
                BitConverter.GetBytes(seed),
                Game.Key.ToCleanByteArray()
            );

            OutPacket response = new OutPacket(WorldCommand.ClientAuthSession);
            response.Write((uint)12340);        // client build
            response.Write(zero);
            response.Write(Game.Username.ToUpper().ToCString());
            response.Write(zero);
            response.Write((uint)ourSeed);
            response.Write(zero);
            response.Write(zero);
            response.Write(ServerInfo.ID);
            response.Write((ulong)zero);
            response.Write(authResponse);
            response.Write(zero);            // length of addon data

            Send(response);

            // TODO: don't fully initialize here, auth may fail
            // instead, initialize in HandleServerAuthResponse when auth succeeds
            // will require special logic in network code to correctly decrypt/parse packet header
            authenticationCrypto.Initialize(Game.Key.ToCleanByteArray());
        }

        [PacketHandler(WorldCommand.ServerAuthResponse)]
        protected void HandleServerAuthResponse(InPacket packet)
        {
            CommandDetail detail = (CommandDetail)packet.ReadByte();

            uint billingTimeRemaining = packet.ReadUInt32();
            byte billingFlags = packet.ReadByte();
            uint billingTimeRested = packet.ReadUInt32();
            byte expansion = packet.ReadByte();

            if (detail == CommandDetail.AUTH_OK)
            {
                OutPacket request = new OutPacket(WorldCommand.CMSG_CHAR_ENUM);
                Send(request);
            }
            else
            {
                Game.UI.LogLine(string.Format("Authentication succeeded, but received response {0}", detail));
                Game.UI.Exit();
            }
        }

        [PacketHandler(WorldCommand.SMSG_CHAR_ENUM)]
        protected void HandleCharEnum(InPacket packet)
        {
            byte count = packet.ReadByte();

            if (count == 0)
            {
                Game.UI.LogLine("No characters found!");
                Game.NoCharactersFound();
            }
            else
            {
                Character[] characters = new Character[count];
                for (byte i = 0; i < count; ++i)
                    characters[i] = new Character(packet);

                Game.UI.PresentCharacterList(characters);
            }
        }
    }
}