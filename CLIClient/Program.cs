using Client.UI.CommandLine;

namespace Client
{
	public class Program
	{
		static void Main(string[] args)
		{
			var p = new Game<CommandLineUI>("username", "password");

			p.Start();
		}
	}
}
