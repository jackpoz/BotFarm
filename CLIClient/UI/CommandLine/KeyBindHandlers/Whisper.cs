using System;
using Client.Chat.Definitions;
using Client.World;
using Client.World.Definitions;
using Client.World.Network;

namespace Client.UI.CommandLine
{
    public partial class CommandLineUI
    {
        [KeyBind(ConsoleKey.W)]
        public void HandleWhisper()
        {
            LogLine("Enter name of player to whisper, or enter 'Q' to go back.", LogLevel.Detail);
            Log("To: ");
            var target = Game.UI.ReadLine();
            if (target == "Q")
                return;

            Log("Message: ");
            var message = Game.UI.ReadLine();

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

        [KeyBind(ConsoleKey.R)]
        public void HandleWhisperReply()
        {
            if (Game.World.LastWhisperers.Count == 0)
                return;

            var target = Game.World.LastWhisperers.Peek();
            LogLine("Hit <TAB> to cycle trough last whisperers, hit <ENTER> to select current recipient, hit <ESCAPE> to return.", LogLevel.Detail);
            LogLine(String.Format("To: {0}", target));
            while (true)
            {
                ConsoleKeyInfo key = Game.UI.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        goto cont;
                    case ConsoleKey.Tab:
                        //! To do: maybe a more efficient way for this:
                        var previous = Game.World.LastWhisperers.Dequeue();
                        Game.World.LastWhisperers.Enqueue(previous);
                        target = Game.World.LastWhisperers.Peek();
                        if (target != previous)
                            LogLine(String.Format("To: {0}", target));
                        continue;
                    case ConsoleKey.Escape:
                        return;
                    default:
                        continue;
                }
            }

            cont:
            Log("Message: ");
            var message = Game.UI.ReadLine();

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