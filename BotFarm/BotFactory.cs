using BotFarm.Properties;
using Client;
using Client.World;
using Client.World.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BotFarm
{
    class BotFactory : IDisposable
    {
        List<AutomatedGame> bots = new List<AutomatedGame>();
        AutomatedGame factoryGame;
        List<BotInfo> botInfos;
        const string botsInfosPath = "botsinfos.xml";

        public BotFactory()
        {
            if (!File.Exists(botsInfosPath))
                botInfos = new List<BotInfo>();
            else using (StreamReader sr = new StreamReader(botsInfosPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<BotInfo>));
                botInfos = (List<BotInfo>)serializer.Deserialize(sr);
            }

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
            Log("Creating new bot");
            Random random = new Random();
            AutomatedGame game = null;

            do
            {
                string username = "BOT" + random.Next();
                string password = random.Next().ToString();
                factoryGame.DoSayChat(".account create " + username + " " + password);
                Thread.Sleep(1000);

                for (int loginTries = 0; loginTries < 5; loginTries++)
                {
                    game = new AutomatedGame(Settings.Default.Hostname,
                                                       Settings.Default.Port,
                                                       username,
                                                       password,
                                                       Settings.Default.RealmID,
                                                       0);
                    game.Start();
                    for (int tries = 0; !game.Connected && tries < 10; tries++)
                        Thread.Sleep(1000);
                    if (!game.Connected)
                    {
                        game.Dispose();
                        game = null;
                    }
                    else
                    {
                        botInfos.Add(new BotInfo(username, password));
                        break;
                    }
                }
            } while (game == null);


            game.CreateCharacter();
            Thread.Sleep(1000);
            game.SendPacket(new OutPacket(WorldCommand.ClientEnumerateCharacters));
            Thread.Sleep(1000);
            return game;
        }

        public AutomatedGame LoadBot(BotInfo info)
        {
            AutomatedGame game = new AutomatedGame(Settings.Default.Hostname,
                                                   Settings.Default.Port,
                                                   info.Username,
                                                   info.Password,
                                                   Settings.Default.RealmID,
                                                   0);
            game.Start();
            return game;
        }

        public void SetupFactory(int botCount)
        {
            Log("Setting up bot factory with " + botCount + " bots");
            int createdBots = 0;
            foreach (var info in botInfos)
            {
                bots.Add(LoadBot(info));
                createdBots++;
            }

            for (; createdBots < botCount; createdBots++)
                bots.Add(CreateBot());
        }

        public void Dispose()
        {
            foreach (var bot in bots)
                bot.Dispose();
            factoryGame.Dispose();

            using (StreamWriter sw = new StreamWriter(botsInfosPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<BotInfo>));
                serializer.Serialize(sw, botInfos);
            }
        }

        [Conditional("DEBUG")]
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
