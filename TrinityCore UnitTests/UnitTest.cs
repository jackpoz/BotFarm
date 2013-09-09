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

            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            game.Start();
            while (!game.LoggedIn)
                Thread.Sleep(1000);
            Thread.Sleep(5000);
            game.Enqueue(() => game.DoSayChat("Connected"));
        }

        [TestMethod]
        public void TestMethod()
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
