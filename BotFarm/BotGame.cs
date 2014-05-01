using BotFarm.Properties;
using Client;
using Client.UI;
using Client.World;
using Client.World.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm
{
    class BotGame : AutomatedGame
    {
        public bool SettingUp
        {
            get;
            set;
        }

        #region Player members
        public UInt64 GroupLeaderGuid { get; private set; }
        public List<UInt64> GroupMembersGuids = new List<UInt64>();
        #endregion

        public BotGame(string hostname, int port, string username, string password, int realmId, int character)
            : base(hostname, port, username, password, realmId, character)
        { }

        public override void NoCharactersFound()
        {
            if (!SettingUp)
            {
                Log("Removing current bot because there are no characters");
                BotFactory.Instance.RemoveBot(this);
            }
        }

        [PacketHandler(WorldCommand.SMSG_GROUP_INVITE)]
        void HandlePartyInvite(InPacket packet)
        {
            if(Settings.Default.Behavior.AutoAcceptGroupInvites)
                SendPacket(new OutPacket(WorldCommand.CMSG_GROUP_ACCEPT, 4));
        }

        [PacketHandler(WorldCommand.SMSG_GROUP_LIST)]
        void HandlePartyList(InPacket packet)
        {
            GroupType groupType = (GroupType)packet.ReadByte();
            packet.ReadByte();
            packet.ReadByte();
            packet.ReadByte();
            if (groupType.HasFlag(GroupType.GROUPTYPE_LFG))
            {
                packet.ReadByte();
                packet.ReadUInt32();
            }
            packet.ReadUInt64();
            packet.ReadUInt32();
            uint membersCount = packet.ReadUInt32();
            GroupMembersGuids.Clear();
            for(uint index = 0; index < membersCount; index++)
            {
                packet.ReadCString();
                UInt64 memberGuid = packet.ReadUInt64();
                GroupMembersGuids.Add(memberGuid);
                packet.ReadByte();
                packet.ReadByte();
                packet.ReadByte();
                packet.ReadByte();
            }
            GroupLeaderGuid = packet.ReadUInt64();
        }

        [PacketHandler(WorldCommand.SMSG_GROUP_DESTROYED)]
        void HandlePartyDisband(InPacket packet)
        {
            GroupLeaderGuid = 0;
            GroupMembersGuids.Clear();
        }

        #region Logging
        public override void Log(string message, LogLevel level = LogLevel.Info)
        {
            BotFactory.Instance.Log(Username + " - " + message, level);
        }

        public override void LogLine(string message, LogLevel level = LogLevel.Info)
        {
            BotFactory.Instance.Log(Username + " - " + message, level);
        }

        public override void LogException(string message)
        {
            BotFactory.Instance.Log(Username + " - " + message, LogLevel.Error);
        }

        public override void LogException(Exception ex)
        {
            BotFactory.Instance.Log(string.Format(Username + " - {0} {1}", ex.Message, ex.StackTrace), LogLevel.Error);
        }
        #endregion
    }
}
