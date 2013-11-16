using BotFarm.Properties;
using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotFarm
{
    class Program
    {
        static void Main(string[] args)
        {
            using (BotFactory factory = new BotFactory())
            {
                factory.SetupFactory(Settings.Default.BotsCount);
                Console.ReadLine();
                GC.KeepAlive(factory);
            }
        }
    }
}
