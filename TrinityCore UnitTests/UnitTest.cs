using System;
using System.Text;
using System.Threading;
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
            game.Enqueue(() => game.DoSayChat("Connected"));
        }

        [TestMethod]
        public void Teleport()
        {
            game.Enqueue(() =>
                {
                    game.DoSayChat("teleing to start position");
                    game.DoSayChat(".tele goldshire");
                });
        }

        [TestMethod]
        public void CastSpells()
        {
            game.Enqueue(() =>
                {
                    game.DoSayChat("testing spells");
                });
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            game.Enqueue(() => game.DoSayChat("Disconnecting"));
            if (game != null)
                game.Dispose();
        }
    }
}
