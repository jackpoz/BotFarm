using Client.UI.CommandLine;
using Client.UI.CommandLine.Properties;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            var hostname = Settings.Default.Hostname;
            var port = Settings.Default.Port;
            var username = Settings.Default.Username;
            var password = Settings.Default.Password;
            var p = new Game<CommandLineUI>(hostname, port, username, password);

            p.Start();
        }
    }
}
