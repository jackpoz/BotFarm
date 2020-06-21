using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client;
using Client.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrinityCore_UnitTests.Helpers;
using TrinityCore_UnitTests.Properties;

namespace TrinityCore_UnitTests
{
    [TestClass]
    public class UnitTest
    {
        static AutomatedGame game;
        const int WaitTimeAfterEachTestInms = 5000;

        [ClassInitialize]
        public static async Task UnitTestInitialize(TestContext context)
        {
            var hostname = Settings.Default.Hostname;
            var port = Settings.Default.Port;
            var username = Settings.Default.Username;
            var password = Settings.Default.Password;
            var realmId = Settings.Default.RealmID;
            var character = Settings.Default.Character;
            game = new AutomatedGame(hostname, port, username, password, realmId, character);

            game.Start();
            int tries = 0;
            while (!game.LoggedIn)
            {
                await Task.Delay(1000);
                tries++;
                if (tries > 15)
                    throw new TimeoutException("Could not login after 15 tries");
            }
            await Task.Delay(5000);
            await game.ScheduleActionAndWait(() => game.DoSayChat("Connected"), WaitTimeAfterEachTestInms);
        }

        [TestMethod]
        public async Task Test01_Teleport()
        {
            await game.ScheduleActionAndWait(() =>
                {
                    game.DoSayChat("teleing to start position");
                    game.Tele("goldshire");
                }, WaitTimeAfterEachTestInms);
        }

        [TestMethod]
        public async Task Test02_CastSpells()
        {
            await game.ScheduleActionAndWait(() =>
                {
                    game.DoSayChat("testing spells");
                    game.DoSayChat(".go xyz -8790.59 349.3 101.02 0 4.57");
                }, 1000);

            await game.ScheduleActionAndWait(() =>
            {
                game.CastSpell(139);
                game.DoSayChat("finished testing spells");
            }, WaitTimeAfterEachTestInms);
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            await game.ScheduleActionAndWait(() => game.DoSayChat("Disconnecting"), WaitTimeAfterEachTestInms);
            await game.DisposeAsync();
        }
    }
}
