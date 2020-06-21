using Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrinityCore_UnitTests.Helpers
{
    static class Helper
    {
        public static async Task ScheduleActionAndWait(this AutomatedGame game, Action action, int waitMilliseconds = 0)
        {
            await game.ScheduleActionAndWait(action, DateTime.Now, waitMilliseconds);
        }

        public static async Task ScheduleActionAndWait(this AutomatedGame game, Action action, DateTime time, int waitMilliseconds = 0)
        {
            var semaphore = new SemaphoreSlim(0);
            game.ScheduleAction(() =>
            {
                action();
                semaphore.Release();
            }, time);

            await semaphore.WaitAsync();

            if (waitMilliseconds > 0)
                await Task.Delay(waitMilliseconds);
        }
    }
}
