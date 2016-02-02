using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm.AI
{
    class BeggerAI : IGameAI
    {
        int scheduledAction;
        int trigger;
        BotGame game;

        public void Activate(AutomatedGame game)
        {
            this.game = (BotGame)game;
            ScheduledBegging();
        }

        void ScheduledBegging()
        {
            // Beg a player only once
            trigger = game.AddTrigger(new Trigger(new[]
            {
                new AlwaysTrueTriggerAction(TriggerActionType.TradeCompleted)
            }, () => game.TradedGUIDs.Add(game.TraderGUID)));

            // Follow player trigger
            //  - find closest player and follow him begging for money with chat messages (unless its a bot)
            scheduledAction = game.ScheduleAction(() =>
            {
                if (game.TraderGUID != 0)
                    return;

                game.CancelActionsByFlag(ActionFlag.Movement);
                var target = game.FindClosestNonBotPlayer(obj => !game.TradedGUIDs.Contains(obj.GUID));
                if (target != null)
                {
                    game.DoSayChat("Please " + game.GetPlayerName(target) + ", give me some money");
                    game.Follow(target);
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
            game.RemoveTrigger(trigger);
        }

        public void Pause()
        {
            game.CancelAction(scheduledAction);
        }

        public void Resume()
        {
            ScheduledBegging();
        }

        public void Update()
        {
        }
    }
}
