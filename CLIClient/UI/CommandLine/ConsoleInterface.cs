using System;
using System.Diagnostics;
using System.IO;
using Client.Authentication;
using Client.World;
using Client.World.Network;
using Client;

namespace Client.UI.CommandLine
{
    class CommandLineUI : IGameUI
    {
        #region Private Members
        private LogLevel _logLevel;

        private StreamWriter _logFile;

        #endregion

        public CommandLineUI()
        {
            _logFile = new StreamWriter(String.Format("{0}.log", DateTime.Now).Replace(':', '_'));
            _logFile.AutoFlush = true;
        }

        #region IGameInterface Members

        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }

        public IGame Game { get; set; }

        public void Update()
        {

        }

        public void Exit()
        {
            _logFile.Close();

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
                LogLine("\n\tName\tType\tPopulation");

                int index = 0;
                foreach (WorldServerInfo server in worldServerList)
                    LogLine
                    (
                        string.Format("{3}\t{0}\t{1}\t{2}",
                        server.Name,
                        server.Type,
                        server.Population,
                        server.Flags,
                        index++)
                    );

                // select a realm - default to the first realm if there is only one
                index = worldServerList.Count == 1 ? 0 : -1;
                while (index > worldServerList.Count || index < 0)
                {
                    Log("Choose a realm:  ");
                    if (!int.TryParse(Console.ReadLine(), out index))
                        LogLine();
                }
                selectedServer = worldServerList[index];
            }

            Game.ConnectTo(selectedServer);
        }

        public void PresentCharacterList(Character[] characterList)
        {
            LogLine("\n\tName\tLevel Class Race");

            Character selectedCharacter;
            int index = 0;
            foreach (Character character in characterList)
                LogLine
                (
                    string.Format("{4}\t{0}\t{1} {2} {3}",
                    character.Name,
                    character.Level,
                    character.Race,
                    character.Class,
                    index++)
                );

            if (characterList.Length < 10)
                LogLine(string.Format("{0}\tCreate a new character. (NOT YET IMPLEMENTED)", index));

            int length = characterList.Length == 10 ? 10 : (characterList.Length + 1);
            index = -1;
            while (index > length || index < 0)
            {
                Log("Choose a character:  ");
                if (!int.TryParse(Console.ReadLine(), out index))
                    LogLine();
            }

            if (index < characterList.Length)
            {
                selectedCharacter = characterList[index];
                // TODO: enter world

                LogLine(string.Format("Entering pseudo-world with character {0}", selectedCharacter.Name));
                
                OutPacket packet = new OutPacket(WorldCommand.CMSG_PLAYER_LOGIN);
                packet.Write(selectedCharacter.GUID);
                Game.SendPacket(packet);
            }
            else
            {
                // TODO: character creation
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (level >= LogLevel)
            {
                Console.Write(message);
                _logFile.Write(String.Format("{0} : {1}", DateTime.Now, message));
            }

            Console.ResetColor();
        }

        public void LogLine(LogLevel level = LogLevel.Info)
        {
            if (level >= LogLevel)
            {
                Console.WriteLine();
                _logFile.WriteLine();
            }

            Console.ResetColor();
        }

        public void LogLine(string message, LogLevel level = LogLevel.Info)
        {
            if (level >= LogLevel)
            {
                Console.WriteLine(message);
                _logFile.WriteLine(String.Format("{0} : {1}", DateTime.Now, message));
            }

            Console.ResetColor();
        }

        public void LogException(string message)
        {
            _logFile.WriteLine(String.Format("{0} : Exception: {1}", DateTime.Now, message));
            _logFile.WriteLine((new StackTrace(1, true)).ToString());
            throw new Exception(message);
        }

        #endregion
    }
}
