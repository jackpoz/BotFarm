using Client;
using Client.World;
using Client.World.Definitions;
using Client.World.Network;
using Client.World.Network.PacketResponses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrinityCore_UnitTests.Helpers;
using TrinityCore_UnitTests.Properties;

namespace TrinityCore_UnitTests.LFG
{
    [TestClass]
    public class JoinLFGTest
    {
        static AutomatedGame game;
        const int WaitTimeAfterEachTestInms = 5000;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
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
            await game.ScheduleActionAndWait(() => game.DoSayChat("Connected"));
        }

        [TestMethod]
        public async Task Test01_JoinLFGWithEmptyRole()
        {
            game.ScheduleAction(() =>
            {
                game.JoinLFG(LfgRoleFlag.None, new []{ (uint)0x06000105 });
            });

            var response = await game.WaitForPacket(WorldCommand.SMSG_LFG_JOIN_RESULT, 5000);
            if (response != null)
            {
                var joinResult = new LFG_JOIN_RESULT(response);
                Assert.AreNotEqual<LfgJoinResult?>(LfgJoinResult.Ok, joinResult.Result);
            }
        }

        [TestMethod]
        public async Task Test02_LeaveLFG()
        {
            await game.ScheduleActionAndWait(() =>
            {
                game.LeaveLFG();
            }, WaitTimeAfterEachTestInms);
        }

        [TestMethod]
        public async Task Test03_JoinLFGWithInvalidRole()
        {
            game.ScheduleAction(() =>
            {
                game.JoinLFG((LfgRoleFlag)0x80, new[] { (uint)0x06000105 });
            });

            var response = await game.WaitForPacket(WorldCommand.SMSG_LFG_JOIN_RESULT, 5000);
            if (response != null)
            {
                var joinResult = new LFG_JOIN_RESULT(response);
                Assert.AreNotEqual<LfgJoinResult?>(LfgJoinResult.Ok, joinResult.Result);
            }
        }

        [TestMethod]
        public async Task Test04_LeaveLFG()
        {
            await game.ScheduleActionAndWait(() =>
            {
                game.LeaveLFG();
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
