using BotFarm.Properties;
using Client;
using Client.UI;
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
        public static BotFactory Instance
        {
            get;
            private set;
        }

        List<BotGame> bots = new List<BotGame>();
        AutomatedGame factoryGame;
        List<BotInfo> botInfos;
        const string botsInfosPath = "botsinfos.xml";
        const string logPath = "botfactory.log";
        StreamWriter logger;

        public BotFactory()
        {
            Instance = this;

            logger = new StreamWriter(logPath);
            logger.WriteLine("Starting BotFactory");

            if (!File.Exists(botsInfosPath))
                botInfos = new List<BotInfo>();
            else using (StreamReader sr = new StreamReader(botsInfosPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<BotInfo>));
                    botInfos = (List<BotInfo>)serializer.Deserialize(sr);
                }
                catch(InvalidOperationException)
                {
                    botInfos = new List<BotInfo>();
                }
            }

            factoryGame = new AutomatedGame(Settings.Default.Hostname,
                                            Settings.Default.Port,
                                            Settings.Default.Username,
                                            Settings.Default.Password,
                                            Settings.Default.RealmID,
                                            0);
            factoryGame.Start();
        }

        public BotGame CreateBot()
        {
            Log("Creating new bot");
            Random random = new Random();
            BotGame game = null;

            do
            {
                string username = "BOT" + random.Next();
                string password = random.Next().ToString();
                factoryGame.DoSayChat(".account create " + username + " " + password);
                Thread.Sleep(1000);

                for (int loginTries = 0; loginTries < 5; loginTries++)
                {
                    game = new BotGame(Settings.Default.Hostname,
                                                       Settings.Default.Port,
                                                       username,
                                                       password,
                                                       Settings.Default.RealmID,
                                                       0);
                    game.SettingUp = true;
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
            game.SettingUp = false;
            return game;
        }

        public BotGame LoadBot(BotInfo info)
        {
            BotGame game = new BotGame(Settings.Default.Hostname,
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
            var infos = botInfos.Take(botCount).ToList();
            Parallel.ForEach<BotInfo>(infos, info =>
            {
                bots.Add(LoadBot(info));
                createdBots++;
            });

            for (; createdBots < botCount; createdBots++)
                bots.Add(CreateBot());

            Log("Finished setting up bot factory with " + botCount + " bots");

            for (; ; )
            {
                string line = Console.ReadLine();
                switch(line)
                {
                    case "quit":
                    case "exit":
                    case "close":
                    case "shutdown":
                        return;
                    case "info":
                    case "infos":
                    case "stats":
                    case "statistics":
                        Console.WriteLine(bots.Where(bot => bot.Running).Count() + " bots are active");
                        Console.WriteLine(bots.Where(bot => bot.Connected).Count() + " bots are connected");
                        Console.WriteLine(bots.Where(bot => bot.LoggedIn).Count() + " bots are ingame");
                        break;
                }
            }
        }

        public void Dispose()
        {
            Parallel.ForEach<BotGame>(bots, 
                bot => bot.Dispose());

            factoryGame.Dispose();

            using (StreamWriter sw = new StreamWriter(botsInfosPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<BotInfo>));
                serializer.Serialize(sw, botInfos);
            }

            logger.WriteLine("Shutting down BotFactory");
            logger.WriteLine("");
            logger.Dispose();
            logger = null;
        }

        public void Log(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
#if !DEBUG_LOG
            if (level > LogLevel.Debug)
#endif
            {
                Console.WriteLine(message);
                logger.WriteLine(message);
            }
        }

        public void RemoveBot(BotGame bot)
        {
            botInfos.Remove(botInfos.Single(info => info.Username == bot.Username && info.Password == bot.Password));
            bots.Remove(bot);
            bot.Dispose();
        }
    }
}
