using System.Collections.Generic;
using Client.Chat;
using Client.Chat.Definitions;

namespace Client.World.Network
{
    public partial class WorldSocket
    {
        [PacketHandler(WorldCommand.SMSG_NAME_QUERY_RESPONSE)]
        protected void HandleNameQueryResponse(InPacket packet)
        {
            var pguid = packet.ReadPackedGuid();
            var end = packet.ReadBoolean();
            if (end)    //! True if not found, false if found
                return;

            var name = packet.ReadCString();

            if (!Game.World.PlayerNameLookup.ContainsKey(pguid))
            {
                //! Add name definition per GUID
                Game.World.PlayerNameLookup.Add(pguid, name);
                //! See if any queued messages for this GUID are stored
                Queue<ChatMessage> messageQueue = null;
                if (Game.World.QueuedChatMessages.TryGetValue(pguid, out messageQueue))
                {
                    ChatMessage m;
                    while (messageQueue.GetEnumerator().MoveNext())
                    {
                        //! Print with proper name and remove from queue
                        m = messageQueue.Dequeue();
                        m.Sender.Sender = name;
                        Game.UI.PresentChatMessage(m);
                    }
                }
            }

            /*
            var realmName = packet.ReadCString();
            var race = (Race)packet.ReadByte();
            var gender = (Gender)packet.ReadByte();
            var cClass = (Class)packet.ReadByte();
            var decline = packet.ReadBoolean();

            if (!decline)
                return;

            for (var i = 0; i < 5; i++)
                var declinedName = packet.ReadCString();
            */
        }
    }
}