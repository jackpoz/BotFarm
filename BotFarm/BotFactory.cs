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
        TextWriter logger;
        Random randomGenerator = new Random();

        public BotFactory()
        {
            Instance = this;

            logger = TextWriter.Synchronized(new StreamWriter(logPath));
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

            DetourCLI.Detour.Initialize(Settings.Default.MMAPsFolderPath);
            VMapCLI.VMap.Initialize(Settings.Default.VMAPsFolderPath);
            MapCLI.Map.Initialize(Settings.Default.MAPsFolderPath);

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

            string username = "BOT" + randomGenerator.Next();
            string password = randomGenerator.Next().ToString();
            lock(factoryGame)
                factoryGame.DoSayChat(".account create " + username + " " + password);

            BotGame game = new BotGame(Settings.Default.Hostname,
                                                Settings.Default.Port,
                                                username,
                                                password,
                                                Settings.Default.RealmID,
                                                0);
            game.SettingUp = true;
            game.Start();
            botInfos.Add(new BotInfo(username, password));

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
            while(!factoryGame.LoggedIn)
            {
                 Log("Waiting for BotFactory account to login");
                 Thread.Sleep(1000);
            }

            Log("Setting up bot factory with " + botCount + " bots");
            int createdBots = 0;
            List<BotInfo> infos;
            if (Settings.Default.RandomBots)
                infos = botInfos.TakeRandom(botCount).ToList();
            else
                infos = botInfos.Take(botCount).ToList();
            Parallel.ForEach<BotInfo>(infos, info =>
            {
                var bot = LoadBot(info);
                lock(bots)
                    bots.Add(bot);
                Interlocked.Increment(ref createdBots);
            });


            Parallel.For(createdBots, botCount, index =>
            {
                try
                {
                    var bot = CreateBot();
                    lock (bots)
                    {
                        bots.Add(bot);
                        if (bots.Count % 100 == 0)
                            SaveBotInfos();
                    }
                }
                catch(Exception ex)
                {
                    Log("Error creating new bot: " + ex.Message + "\n" + ex.StackTrace, LogLevel.Error);
                }
            });

            Log("Finished setting up bot factory with " + botCount + " bots");

            SaveBotInfos();

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
            Log("Shutting down BotFactory");
            Log("This might at least 20 seconds to allow all bots to properly logout");

            Parallel.ForEach<BotGame>(bots, 
                bot => bot.Dispose());

            factoryGame.Dispose();

            SaveBotInfos();

            logger.Dispose();
            logger = null;
        }

        private void SaveBotInfos()
        {
            using (StreamWriter sw = new StreamWriter(botsInfosPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<BotInfo>));
                serializer.Serialize(sw, botInfos);
            }
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
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
            lock (bots)
            {
                botInfos.Remove(botInfos.Single(info => info.Username == bot.Username && info.Password == bot.Password));
                bots.Remove(bot);
            }

            bot.Dispose();
        }
    }
}
