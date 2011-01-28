using System;
using Client.Authentication;

namespace Client.UI.CommandLine
{
	class CommandLineUI : IGameUI
	{
		LogLevel LogLevel = LogLevel.Info;

		#region IGameInterface Members

		public IGame Game { get; set; }

		public void Update()
		{

		}

		public void Exit()
		{
			Console.Write("Press any key to continue...");
			Console.ReadKey(true);
		}

		public void PresentRealmList(WorldServerList worldServerList)
		{
			WorldServerInfo selectedServer = null;

			if (worldServerList.Count == 1)
				selectedServer = worldServerList[0];
			else
			{
				Log("\n\tName\tType\tPopulation");

				int index = 0;
				foreach (WorldServerInfo server in worldServerList)
					Log(string.Format("{3}\t{0}\t{1}\t{2}", server.Name, server.Type, server.Population, server.Flags, index++));

				// select a realm - default to the first realm if there is only one
				index = worldServerList.Count == 1 ? 0 : -1;
				while (index > worldServerList.Count || index < 0)
				{
					Log("Choose a realm:  ");
					index = int.Parse(Console.ReadLine());
				}
				selectedServer = worldServerList[index];
			}

			Game.ConnectTo(selectedServer);
		}

		public void Log(string message, LogLevel level = LogLevel.Info)
		{
			if (level >= LogLevel)
				Console.Write(message);
		}

		public void LogLine(string message, LogLevel level = LogLevel.Info)
		{
			if (level >= LogLevel)
				Console.WriteLine(message);
		}

		#endregion
	}
}
