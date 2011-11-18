using System;
using Client.Chat.Definitions;
using Client.World;
using Client.World.Definitions;
using Client.World.Network;

namespace Client.UI.CommandLine
{
    public partial class CommandLineUI
    {
        [KeyBind(ConsoleKey.G)]
        public void DoGuildChat()
        {
            Log("Guild Message: ");
            var message = Game.UI.ReadLine();

            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Guild);
            var race = Game.World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(message.ToCString());
            Game.SendPacket(response);
        }

        [KeyBind(ConsoleKey.O)]
        public void DoOfficerChat()
        {
            Log("Officer Message: ");
            var message = Game.UI.ReadLine();

            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Officer);
            var race = Game.World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(message.ToCString());
            Game.SendPacket(response);
        }
    }
}
