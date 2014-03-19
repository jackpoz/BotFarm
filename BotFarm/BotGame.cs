using Client;
using Client.World;
using Client.World.Network;
using System;
using System.Collections.Generic;
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
            SendPacket(new OutPacket(WorldCommand.CMSG_GROUP_ACCEPT, 4));
        }
    }
}
