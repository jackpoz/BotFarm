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
        public string ChannelName = string.Empty; // Optional
    }
}
