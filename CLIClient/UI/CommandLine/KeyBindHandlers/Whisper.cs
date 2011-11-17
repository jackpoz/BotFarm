using System;
using Client.Chat;
using Client.Chat.Definitions;
using Client.World;
using Client.World.Definitions;
using Client.World.Network;

namespace Client.UI.CommandLine
{
    public partial class CommandLineUI
    {
        [KeyBindAttribute(ConsoleKey.W)]
        public void HandleWhisper()
        {
            LogLine("Enter name of player to whisper, or enter 'Q' to go back.");
            Log("To: ");
            var target = Console.ReadLine();
            if (target.Equals('Q'))
                return;

            Log("Message: ");
            var message = Console.ReadLine();

            var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

            response.Write((uint)ChatMessageType.Whisper);
            var race = Game.World.SelectedCharacter.Race;
            var language = race.IsHorde() ? Language.Orcish : Language.Common;
            response.Write((uint)language);
            response.Write(target.ToCString());
            response.Write(message.ToCString());
            Game.SendPacket(response);

            //! Print on WhisperInform message
        }
    }
}