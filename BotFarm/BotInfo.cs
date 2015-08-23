using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm
{
    public class BotInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string BehaviorName { get; set; }
        public BotInfo() { }

        public BotInfo(string username, string password, string behaviorName)
        {
            this.Username = username;
            this.Password = password;
            this.BehaviorName = behaviorName;
        }
    }
}
