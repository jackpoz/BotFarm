using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (Sender.Type == ChatMessageType.Channel)
            {
                sb.Append(" [");
                sb.Append(Sender.ChannelName);
                sb.Append("] ");
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
