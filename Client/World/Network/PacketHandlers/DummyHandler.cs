namespace Client.World.Network
{
    public partial class WorldSocket
    {
        //! Ignore the packet.
        [PacketHandler(WorldCommand.SMSG_POWER_UPDATE)]
        [PacketHandler(WorldCommand.SMSG_SET_PROFICIENCY)]
        [PacketHandler(WorldCommand.MSG_SET_DUNGEON_DIFFICULTY)]
        [PacketHandler(WorldCommand.SMSG_LOGIN_VERIFY_WORLD)]
        [PacketHandler(WorldCommand.SMSG_ACCOUNT_DATA_TIMES)]
        [PacketHandler(WorldCommand.SMSG_FEATURE_SYSTEM_STATUS)]
        [PacketHandler(WorldCommand.SMSG_LEARNED_DANCE_MOVES)]
        [PacketHandler(WorldCommand.SMSG_BINDPOINTUPDATE)]
        [PacketHandler(WorldCommand.SMSG_TALENTS_INFO)]
        [PacketHandler(WorldCommand.SMSG_INSTANCE_DIFFICULTY)]
        [PacketHandler(WorldCommand.SMSG_INITIAL_SPELLS)]
        [PacketHandler(WorldCommand.SMSG_SEND_UNLEARN_SPELLS)]
        [PacketHandler(WorldCommand.SMSG_ACTION_BUTTONS)]
        [PacketHandler(WorldCommand.SMSG_INITIALIZE_FACTIONS)]
        [PacketHandler(WorldCommand.SMSG_ALL_ACHIEVEMENT_DATA)]
        [PacketHandler(WorldCommand.SMSG_EQUIPMENT_SET_LIST)]
        [PacketHandler(WorldCommand.SMSG_LOGIN_SETTIMESPEED)]
        [PacketHandler(WorldCommand.SMSG_SET_FORCED_REACTIONS)]
        [PacketHandler(WorldCommand.SMSG_COMPRESSED_UPDATE_OBJECT)]
        [PacketHandler(WorldCommand.SMSG_MONSTER_MOVE)]
        void HandleDummyPacket(InPacket packet)
        {

        }
    }
}