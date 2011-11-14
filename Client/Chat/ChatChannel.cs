using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Chat.Definitions;

namespace Client.Chat
{
    /// <summary>
    /// A channel trough which a message is sent. This can also be whisper.
    /// </summary>
    public class ChatChannel
    {
        public ChatMessageType Type;
        public string Sender;
        public string ChannelName = String.Empty; // Optional
    }
}
