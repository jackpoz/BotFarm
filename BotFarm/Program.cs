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
            BotFactory factory = new BotFactory();
            factory.SetupFactory(1);
            Console.ReadLine();
            GC.KeepAlive(factory);
        }
    }
}
