using Client.Chat.Definitions;

namespace Client.Chat
{
    public class ChatMessage
    {
        public ChatChannel Sender;
        public Language Language;
        public ChatTag ChatTag;
        public string Message;
    }
}
