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
    public class UnitTest : IDisposable
    {
        AutomatedGame game;

        public UnitTest()
        {
            var hostname = Settings.Default.Hostname;
            var port = Settings.Default.Port;
            var username = Settings.Default.Username;
            var password = Settings.Default.Password;
            game = new AutomatedGame(hostname, port, username, password);

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

        public void Dispose()
        {
            game.Enqueue(() => game.DoSayChat("Disconnecting"));
            if (game != null)
                game.Dispose();
        }
    }
}
