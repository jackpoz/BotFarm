using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BotFarm.UnitTests.Properties;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BotFarm.UnitTests
{
    [TestClass]
    public class Maps
    {
        [TestMethod]
        public void MMapsSimplePath()
        {
            DetourCLI.Detour.Initialize(Settings.Default.MMAPsFolderPath);
            VMapCLI.VMap.Initialize(Settings.Default.VMAPsFolderPath);
            MapCLI.Map.Initialize(Settings.Default.MAPsFolderPath);

            using (var detour = new DetourCLI.Detour())
            {
                List<MapCLI.Point> resultPath;
                var result = detour.FindPath(-8896.072266f, -82.352325f, 86.421661f,
                                        -8915.272461f, -111.634041f, 82.275642f,
                                        0, out resultPath);
                Assert.IsTrue(result == DetourCLI.PathType.Complete);
            }
        }

        [TestMethod]
        public void MMapsRaceConditions()
        {
            DetourCLI.Detour.Initialize(Settings.Default.MMAPsFolderPath);
            VMapCLI.VMap.Initialize(Settings.Default.VMAPsFolderPath);
            MapCLI.Map.Initialize(Settings.Default.MAPsFolderPath);

            var queuedTask = new List<Task>();
            for (int i = 0; i < 4; i++)
                queuedTask.Add(new Task(() =>
                {
                    using (var detour = new DetourCLI.Detour())
                    {
                        List<MapCLI.Point> resultPath;
                        var result = detour.FindPath(-8896.072266f, -82.352325f, 86.421661f,
                                                -8915.272461f, -111.634041f, 82.275642f,
                                                0, out resultPath);
                        Assert.IsTrue(result == DetourCLI.PathType.Complete);
                    }
                }));

            queuedTask.ForEach(task => task.Start());
            Task.WaitAll(queuedTask.ToArray());
        }
    }
}
