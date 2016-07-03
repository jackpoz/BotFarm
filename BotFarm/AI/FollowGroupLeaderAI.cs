using Client;
using Client.AI;
using Client.World.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm.AI
{
    class FollowGroupLeaderAI : IStrategicAI
    {
        int scheduledAction;
        AutomatedGame game;

        public bool Activate(AutomatedGame game)
        {
            this.game = game;
            ScheduleFollowLeader();
            return true;
        }

        void ScheduleFollowLeader()
        {
            scheduledAction = game.ScheduleAction(() =>
            {
                if (!game.Player.IsAlive)
                    return;

                // Check if we are in a party and follow the party leader
                if (game.GroupLeaderGuid == 0)
                    return;

                WorldObject groupLeader;
                if (game.Objects.TryGetValue(game.GroupLeaderGuid, out groupLeader))
                {
                    game.CancelActionsByFlag(ActionFlag.Movement);
                    game.Follow(groupLeader);
                }
            }, DateTime.Now.AddSeconds(30), new TimeSpan(0, 0, 30));
        }

        public bool AllowPause()
        {
            return true;
        }

        public void Deactivate()
        {
            game.CancelAction(scheduledAction);
        }

        public void Pause()
        {
            game.CancelAction(scheduledAction);
        }

        public void Resume()
        {
            ScheduleFollowLeader();
        }

        public void Update()
        {
        }
    }
}
