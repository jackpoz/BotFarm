using System;
using System.Text;
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
        }

        [TestMethod]
        public void TestMethod()
        {
            System.Threading.Thread.Sleep(10000);
        }

        public void Dispose()
        {
            if (game != null)
                game.Exit();
        }
    }
}
