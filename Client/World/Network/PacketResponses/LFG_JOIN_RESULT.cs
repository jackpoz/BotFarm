using Client.World.Definitions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.World.Network.PacketResponses
{
    public class LFG_JOIN_RESULT
    {
        readonly InPacket packet;

        public LfgJoinResult Result { get; private set; }
        public LfgRoleCheckStatus Reason { get; private set; }
        public Dictionary<UInt64, List<LFGLockStatus>> Locks { get; private set; }

        public LFG_JOIN_RESULT(InPacket packet)
        {
            this.packet = packet;
            Read();
        }

        public void Read()
        {

            Result = (LfgJoinResult)packet.ReadUInt32();
            Reason = (LfgRoleCheckStatus)packet.ReadUInt32();

            Locks = new Dictionary<ulong, List<LFGLockStatus>>();

            if (Result != LfgJoinResult.PartyNotMeetReqs)
                return;

            var playerCount = packet.ReadByte();
            for(var playerCounter = 0; playerCounter < playerCount; playerCounter++)
            {
                UInt64 guid = packet.ReadUInt64();
                var dungCount = packet.ReadUInt32();

                var playerLocks = new List<LFGLockStatus>();

                for (var dungCounter = 0; dungCounter < dungCount; dungCounter++)
                    playerLocks.Add(new LFGLockStatus(packet.ReadUInt32(), (LfgEntryCheckResult)packet.ReadUInt32()));

                Locks.Add(guid, playerLocks);
            }
        }

        public class LFGLockStatus
        {
            public UInt32 DungeonEntry { get; private set; }
            public LfgEntryCheckResult LockStatus { get; private set; }

            public LFGLockStatus(UInt32 entry, LfgEntryCheckResult status)
            {
                this.DungeonEntry = entry;
                this.LockStatus = status;
            }
        }
    }
}
