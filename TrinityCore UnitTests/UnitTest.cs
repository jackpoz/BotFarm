using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client;
using Client.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrinityCore_UnitTests.Properties;

namespace TrinityCore_UnitTests
{
    [TestClass]
    public class UnitTest
    {
        static AutomatedGame game;
        static SemaphoreSlim semaphore;

        [ClassInitialize]
        public static void UnitTestInitialize(TestContext context)
        {
            var hostname = Settings.Default.Hostname;
            var port = Settings.Default.Port;
            var username = Settings.Default.Username;
            var password = Settings.Default.Password;
            var realmId = Settings.Default.RealmID;
            var character = Settings.Default.Character;
            game = new AutomatedGame(hostname, port, username, password, realmId, character);
            semaphore = new SemaphoreSlim(0);

            game.Start();
            int tries = 0;
            while (!game.LoggedIn)
            {
                Thread.Sleep(1000);
                tries++;
                if (tries > 15)
                    throw new TimeoutException("Could not login after 15 tries");
            }
            Thread.Sleep(5000);
            game.ScheduleAction(() => game.DoSayChat("Connected"));
        }

        [TestMethod]
        public async Task Test01_Teleport()
        {
            game.ScheduleAction(() =>
                {
                    game.DoSayChat("teleing to start position");
                    game.Tele("goldshire");

                    game.ScheduleAction(() => semaphore.Release(), DateTime.Now.AddSeconds(5));
                });

            await semaphore.WaitAsync();
        }

        [TestMethod]
        public async Task Test02_CastSpells()
        {
            game.ScheduleAction(() =>
                {
                    game.DoSayChat("testing spells");
                    game.DoSayChat(".go xyz -8790.59 349.3 101.02 0 4.57");
                    game.CastSpell(139);
                    game.DoSayChat("finished testing spells");

                    game.ScheduleAction(() => semaphore.Release(), DateTime.Now.AddSeconds(5));
                });

            await semaphore.WaitAsync();
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            game.ScheduleAction(() => game.DoSayChat("Disconnecting"));
            await game?.Dispose();
        }
    }
}
