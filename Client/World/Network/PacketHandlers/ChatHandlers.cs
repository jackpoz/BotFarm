using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Chat;
using Client.Chat.Definitions;

namespace Client.World.Network
{
    public partial class WorldSocket
    {
        [PacketHandler(WorldCommand.SMSG_MESSAGECHAT)]
        protected void HandleMessageChat(InPacket packet)
        {
            var type = (ChatMessageType)packet.ReadByte();
            var lang = (Language)packet.ReadInt32();
            var guid = packet.ReadUInt64();
            var unkInt = packet.ReadInt32();

            switch (type)
            {
                case ChatMessageType.Say:
                case ChatMessageType.Yell:
                case ChatMessageType.Party:
                case ChatMessageType.PartyLeader:
                case ChatMessageType.Raid:
                case ChatMessageType.RaidLeader:
                case ChatMessageType.RaidWarning:
                case ChatMessageType.Guild:
                case ChatMessageType.Officer:
                case ChatMessageType.Emote:
                case ChatMessageType.TextEmote:
                case ChatMessageType.Whisper:
                case ChatMessageType.WhisperInform:
                case ChatMessageType.System:
                case ChatMessageType.Channel:
                case ChatMessageType.Battleground:
                case ChatMessageType.BattlegroundNeutral:
                case ChatMessageType.BattlegroundAlliance:
                case ChatMessageType.BattlegroundHorde:
                case ChatMessageType.BattlegroundLeader:
                case ChatMessageType.Achievement:
                case ChatMessageType.GuildAchievement:
                    {
                        ChatChannel channel = new ChatChannel();
                        channel.Type = type;

                        if (type == ChatMessageType.Channel)
                            channel.ChannelName = packet.ReadCString();

                        var sender = packet.ReadUInt64();

                        ChatMessage message = new ChatMessage();
                        var textLen = packet.ReadInt32();
                        message.Message = packet.ReadCString();
                        message.Language = lang;
                        message.ChatTag = (ChatTag)packet.ReadByte();
                        message.Sender = channel;

                        //! If we know the name of the sender GUID, use it
                        //! For system messages sender GUID is 0, don't need to do anything fancy
                        string senderName = null;
                        if (type == ChatMessageType.System ||
                            Game.World.PlayerNameLookup.TryGetValue(sender, out senderName))
                        {
                            message.Sender.Sender = senderName;
                            Game.UI.PresentChatMessage(message);
                            return;
                        }

                        //! If not we place the message in the queue,
                        //! .. either existent
                        Queue<ChatMessage> messageQueue = null;
                        if (Game.World.QueuedChatMessages.TryGetValue(sender, out messageQueue))
                            messageQueue.Enqueue(message);
                        //! or non existent
                        else
                        {
                            messageQueue = new Queue<ChatMessage>();
                            messageQueue.Enqueue(message);
                            Game.World.QueuedChatMessages.Add(sender, messageQueue);
                        }

                        //! Furthermore we send CMSG_NAME_QUERY to the server to retrieve the name of the sender
                        OutPacket response = new OutPacket(WorldCommand.CMSG_NAME_QUERY);
                        response.Write(sender);
                        Game.SendPacket(response);

                        //! Enqueued chat will be printed when we receive SMSG_NAME_QUERY_RESPONSE

                        break;
                    }
                default:
                    return;
            }
        }

        [PacketHandler(WorldCommand.SMSG_CHAT_PLAYER_NOT_FOUND)]
        protected void HandleChatPlayerNotFound(InPacket packet)
        {
            Game.UI.LogLine(String.Format("Player {0} not found!", packet.ReadCString()));
        }
    }
}
