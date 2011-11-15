using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Client.Chat.Definitions;

namespace Client.Chat
{
    public class ChatMessage
    {
        public ChatChannel Sender;
        public Language Language;
        public ChatTag ChatTag;
        public string Message;
        public DateTime Timestamp;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Timestamp);
            sb.Append(": ");
            sb.Append(Sender.Type.ToString());
            //! Color codes taken from default chat_cache in WTF folder
            //! TODO: RTF form?
            switch (Sender.Type)
            {
                case ChatMessageType.Channel:
                {
                    //sb.ForeColor(Color.FromArgb(255, 192, 192));
                    Console.ForegroundColor = ConsoleColor.DarkYellow;//Color.DarkSalmon;
                    sb.Append(" [");
                    sb.Append(Sender.ChannelName);
                    sb.Append("] ");
                    break;
                }
                case ChatMessageType.Whisper:
                case ChatMessageType.WhisperInform:
                case ChatMessageType.WhisperForeign:
                    //sb.ForeColor(Color.FromArgb(255, 128, 255));
                    Console.ForegroundColor = ConsoleColor.Magenta;//Color.DeepPink;
                    break;
                case ChatMessageType.System:
                case ChatMessageType.Money:
                case ChatMessageType.TargetIcons:
                case ChatMessageType.Achievement:
                    //sb.ForeColor(Color.FromArgb(255, 255, 0));
                    Console.ForegroundColor = ConsoleColor.Yellow;//Color.Gold;
                    break;
                case ChatMessageType.Emote:
                case ChatMessageType.TextEmote:
                case ChatMessageType.MonsterEmote:
                    //sb.ForeColor(Color.FromArgb(255, 128, 64));
                    Console.ForegroundColor = ConsoleColor.DarkRed;//Color.Chocolate;
                    break;
                case ChatMessageType.Party:
                    //sb.ForeColor(Color.FromArgb(170, 170, 255));
                    Console.ForegroundColor = ConsoleColor.DarkCyan;//Color.CornflowerBlue;
                    break;
                case ChatMessageType.PartyLeader:
                    //sb.ForeColor(Color.FromArgb(118, 200, 255));
                    Console.ForegroundColor = ConsoleColor.Cyan;//Color.DodgerBlue;
                    break;
                case ChatMessageType.Raid:
                    //sb.ForeColor(Color.FromArgb(255, 172, 0));
                    Console.ForegroundColor = ConsoleColor.Red;//Color.DarkOrange;
                    break;
                case ChatMessageType.RaidLeader:
                    //sb.ForeColor(Color.FromArgb(255, 72, 9));
                    Console.ForegroundColor = ConsoleColor.Red;//Color.DarkOrange;
                    break;
                case ChatMessageType.RaidWarning:
                    //sb.ForeColor(Color.FromArgb(255, 72, 0));
                    Console.ForegroundColor = ConsoleColor.Red;//Color.DarkOrange;
                    break;
                case ChatMessageType.Guild:
                case ChatMessageType.GuildAchievement:
                    //sb.ForeColor(Color.FromArgb(64, 255, 64));
                    Console.ForegroundColor = ConsoleColor.Green;//Color.LimeGreen;
                    break;
                case ChatMessageType.Officer:
                    //sb.ForeColor(Color.FromArgb(64, 192, 64));
                    Console.ForegroundColor = ConsoleColor.DarkGreen;//Color.DarkSeaGreen;
                    break;
                case ChatMessageType.Say:
                default:
                    //sb.ForeColor(Color.FromArgb(255, 255, 255));
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            
            sb.Append("[");
            if (ChatTag.HasFlag(ChatTag.Gm))
                sb.Append("<GM>");
            if (ChatTag.HasFlag(ChatTag.Afk))
                sb.Append("<AFK>");
            if (ChatTag.HasFlag(ChatTag.Dnd))
                sb.Append("<DND>");
            sb.Append(Sender.Sender);
            sb.Append("]: ");
            sb.Append(Message);
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
