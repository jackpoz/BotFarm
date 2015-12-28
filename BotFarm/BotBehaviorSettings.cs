using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm
{
    [Serializable]
    public struct BotBehaviorSettings
    {
        public string Name;
        public uint Probability;
        public bool AutoAcceptGroupInvites;
        public bool AutoAcceptResurrectRequests;
        public bool AutoResurrect;
        public bool Begger;
        public bool FollowGroupLeader;
        public bool Explorer;
    }
}
