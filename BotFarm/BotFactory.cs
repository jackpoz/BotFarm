using BotFarm.Properties;
using Client;
using Client.World;
using Client.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BotFarm
{
    class BotFactory : IDisposable
    {
        List<AutomatedGame> bots = new List<AutomatedGame>();
        AutomatedGame factoryGame;

        public BotFactory()
        {
            factoryGame = new AutomatedGame(Settings.Default.Hostname,
                                            Settings.Default.Port,
                                            Settings.Default.Username,
                                            Settings.Default.Password,
                                            Settings.Default.RealmID,
                                            0);
            factoryGame.Start();
            int tries = 0;
            while (!factoryGame.LoggedIn)
            {
                Thread.Sleep(1000);
                tries++;
                if (tries > 15)
                    throw new TimeoutException("Could not login after 15 tries");
            }
        }

        public AutomatedGame CreateBot()
        {
            Random random = new Random();
            string username = "BOT" + random.Next();
            string password = random.Next().ToString();
            factoryGame.DoSayChat(".account create " + username + " " + password);
            Thread.Sleep(1000);

            AutomatedGame game = new AutomatedGame(Settings.Default.Hostname,
                                                   Settings.Default.Port,
                                                   username,
                                                   password,
                                                   Settings.Default.RealmID,
                                                   0);
            game.Start();
            while(!game.Connected)
                Thread.Sleep(1000);
            game.CreateCharacter();
            Thread.Sleep(1000);
            game.SendPacket(new OutPacket(WorldCommand.ClientEnumerateCharacters));
            Thread.Sleep(1000);
            return game;
        }

        public void SetupFactory(int botCount)
        {
            for (int i = 0; i < botCount; i++)
                bots.Add(CreateBot());
        }

        public void Dispose()
        {
            foreach (var bot in bots)
                bot.Dispose();
            factoryGame.Dispose();
        }
    }
}
