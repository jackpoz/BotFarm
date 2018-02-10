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
        [PacketHandler(WorldCommand.SMSG_GM_MESSAGECHAT)]
        protected void HandleMessageChat(InPacket packet)
        {
            var chatType = (ChatMessageType)packet.ReadByte();
            var language = (Language)packet.ReadInt32();
            UInt64 senderGuid = packet.ReadUInt64();
            var unkInt = packet.ReadUInt32();

            UInt32 senderNameLen = 0;
            string senderName = null;
            UInt64 receiverGuid = 0;
            UInt32 receiverNameLen = 0;
            string receiverName = null;
            string channelName = null;

            switch (chatType)
            {
                case ChatMessageType.MonsterSay:
                case ChatMessageType.MonsterParty:
                case ChatMessageType.MonsterYell:
                case ChatMessageType.MonsterWhisper:
                case ChatMessageType.MonsterEmote:
                case ChatMessageType.RaidBossEmote:
                case ChatMessageType.RaidBossWhisper:
                case ChatMessageType.BattleNet:
                    senderNameLen = packet.ReadUInt32();
                    senderName = packet.ReadCString();
                    receiverGuid = packet.ReadUInt64();
                    if (receiverGuid != 0 && !receiverGuid.IsPlayer() && !receiverGuid.IsPet())
                    {
                        receiverNameLen = packet.ReadUInt32();
                        receiverName = packet.ReadCString();
                    }
                    break;
                case ChatMessageType.WhisperForeign:
                    senderNameLen = packet.ReadUInt32();
                    senderName = packet.ReadCString();
                    receiverGuid = packet.ReadUInt64();
                    break;
                case ChatMessageType.BattlegroundNeutral:
                case ChatMessageType.BattlegroundAlliance:
                case ChatMessageType.BattlegroundHorde:
                    receiverGuid = packet.ReadUInt64();
                    if (receiverGuid != 0 && !receiverGuid.IsPlayer())
                    {
                        receiverNameLen = packet.ReadUInt32();
                        receiverName = packet.ReadCString();
                    }
                    break;
                case ChatMessageType.Achievement:
                case ChatMessageType.GuildAchievement:
                    receiverGuid = packet.ReadUInt64();
                    break;
                default:
                    if (packet.Header.Command == WorldCommand.SMSG_GM_MESSAGECHAT)
                    {
                        senderNameLen = packet.ReadUInt32();
                        senderName = packet.ReadCString();
                    }

                    if (chatType == ChatMessageType.Channel)
                    {
                        channelName = packet.ReadCString();
                    }

                    receiverGuid = packet.ReadUInt64();
                    break;
            }

            UInt32 messageLen = packet.ReadUInt32();
            string message = packet.ReadCString();
            var chatTag = (ChatTag)packet.ReadByte();

            if (chatType == ChatMessageType.Achievement || chatType == ChatMessageType.GuildAchievement)
            {
                var achievementId = packet.ReadUInt32();
            }
            
            ChatChannel channel = new ChatChannel();
            channel.Type = chatType;

            if (chatType == ChatMessageType.Channel)
                channel.ChannelName = channelName;

            ChatMessage chatMessage = new ChatMessage();
            chatMessage.Message = message;
            chatMessage.Language = language;
            chatMessage.ChatTag = chatTag;
            chatMessage.Sender = channel;

            //! If we know the name of the sender GUID, use it
            //! For system messages sender GUID is 0, don't need to do anything fancy
            if (senderGuid == 0 || !string.IsNullOrEmpty(senderName)
                || Game.World.PlayerNameLookup.TryGetValue(senderGuid, out senderName))
            {
                chatMessage.Sender.Sender = senderName;
                Game.UI.PresentChatMessage(chatMessage);
                return;
            }

            //! If not we place the message in the queue,
            //! .. either existent
            Queue<ChatMessage> messageQueue = null;
            if (Game.World.QueuedChatMessages.TryGetValue(senderGuid, out messageQueue))
                messageQueue.Enqueue(chatMessage);
            //! or non existent
            else
            {
                messageQueue = new Queue<ChatMessage>();
                messageQueue.Enqueue(chatMessage);
                Game.World.QueuedChatMessages.Add(senderGuid, messageQueue);
            }

            //! Furthermore we send CMSG_NAME_QUERY to the server to retrieve the name of the sender
            OutPacket response = new OutPacket(WorldCommand.CMSG_NAME_QUERY);
            response.Write(senderGuid);
            Game.SendPacket(response);

            //! Enqueued chat will be printed when we receive SMSG_NAME_QUERY_RESPONSE
        }

        [PacketHandler(WorldCommand.SMSG_CHAT_PLAYER_NOT_FOUND)]
        protected void HandleChatPlayerNotFound(InPacket packet)
        {
            Game.UI.LogLine(String.Format("Player {0} not found!", packet.ReadCString()));
        }
    }
}
