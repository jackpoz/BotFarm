using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Client;
using Client.Authentication;
using Client.Chat;
using Client.Chat.Definitions;
using Client.World;
using Client.World.Network;

namespace Client.UI.CommandLine
{
    public partial class CommandLineUI : IGameUI
    {
        #region Private Members
        private LogLevel _logLevel;
        private StreamWriter _logFile;

        #endregion

        public CommandLineUI()
        {
            _logFile = new StreamWriter(String.Format("{0}.log", DateTime.Now).Replace(':', '_').Replace('/', '-'));
            _logFile.AutoFlush = true;

            InitializeKeybinds();
        }

        #region IGameUI Members

        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }

        public IGame Game { get; set; }

        public void Update()
        {
            if (Game.World.SelectedCharacter == null)
                return;

            ConsoleKeyInfo keyPress = Console.ReadKey();
            KeyBind handler;
            if (_keyPressHandlers.TryGetValue(keyPress.Key, out handler))
                handler();
        }

        public void Exit()
        {
            Console.Write("Press any key to continue...");
            Console.ReadKey(true);

            _logFile.Close();
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
                        string.Format("{0}\t{1}\t{2}\t{3}",
                        index++,
                        server.Name,
                        server.Type,
                        server.Population
                        )
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
                Game.World.SelectedCharacter = characterList[index];
                // TODO: enter world

                LogLine(string.Format("Entering pseudo-world with character {0}", Game.World.SelectedCharacter.Name));

                OutPacket packet = new OutPacket(WorldCommand.CMSG_PLAYER_LOGIN);
                packet.Write(Game.World.SelectedCharacter.GUID);
                Game.SendPacket(packet);
            }
            else
            {
                // TODO: character creation
            }
        }

        public void PresentChatMessage(ChatMessage message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message.Sender.Type == ChatMessageType.WhisperInform ? "To: " : message.Sender.Type.ToString());
            //! Color codes taken from default chat_cache in WTF folder
            //! TODO: RTF form?
            switch (message.Sender.Type)
            {
            case ChatMessageType.Channel:
                {
                    //sb.ForeColor(Color.FromArgb(255, 192, 192));
                    Console.ForegroundColor = ConsoleColor.DarkYellow;//Color.DarkSalmon;
                    sb.Append(" [");
                    sb.Append(message.Sender.ChannelName);
                    sb.Append("] ");
                    break;
                }
            case ChatMessageType.Whisper:
                    Game.World.LastWhisperers.Enqueue(message.Sender.Sender);
                    goto case ChatMessageType.WhisperInform;
            case ChatMessageType.WhisperInform:
            case ChatMessageType.WhisperForeign:
                    //sb.ForeColor(Color.FromArgb(255, 128, 255));
                Console.ForegroundColor = ConsoleColor.Magenta;//Color.DeepPink;
                break;
            case ChatMessageType.System:
            case ChatMessageType.Money:
            case ChatMessageType.TargetIcons:
            case ChatMessageType.Achievement:
                    //sb.ForeColor(Color.FromArgb(255, 255, 0));
                Console.ForegroundColor = ConsoleColor.Yellow;//Color.Gold;
                break;
            case ChatMessageType.Emote:
            case ChatMessageType.TextEmote:
            case ChatMessageType.MonsterEmote:
                    //sb.ForeColor(Color.FromArgb(255, 128, 64));
                Console.ForegroundColor = ConsoleColor.DarkRed;//Color.Chocolate;
                break;
            case ChatMessageType.Party:
                    //sb.ForeColor(Color.FromArgb(170, 170, 255));
                Console.ForegroundColor = ConsoleColor.DarkCyan;//Color.CornflowerBlue;
                break;
            case ChatMessageType.PartyLeader:
                    //sb.ForeColor(Color.FromArgb(118, 200, 255));
                Console.ForegroundColor = ConsoleColor.Cyan;//Color.DodgerBlue;
                break;
            case ChatMessageType.Raid:
                    //sb.ForeColor(Color.FromArgb(255, 172, 0));
                Console.ForegroundColor = ConsoleColor.Red;//Color.DarkOrange;
                break;
            case ChatMessageType.RaidLeader:
                    //sb.ForeColor(Color.FromArgb(255, 72, 9));
                Console.ForegroundColor = ConsoleColor.Red;//Color.DarkOrange;
                break;
            case ChatMessageType.RaidWarning:
                    //sb.ForeColor(Color.FromArgb(255, 72, 0));
                Console.ForegroundColor = ConsoleColor.Red;//Color.DarkOrange;
                break;
            case ChatMessageType.Guild:
            case ChatMessageType.GuildAchievement:
                    //sb.ForeColor(Color.FromArgb(64, 255, 64));
                Console.ForegroundColor = ConsoleColor.Green;//Color.LimeGreen;
                break;
            case ChatMessageType.Officer:
                    //sb.ForeColor(Color.FromArgb(64, 192, 64));
                Console.ForegroundColor = ConsoleColor.DarkGreen;//Color.DarkSeaGreen;
                break;
            case ChatMessageType.Yell:
                Console.ForegroundColor = ConsoleColor.DarkRed;
                break;
            case ChatMessageType.Say:
            default:
                    //sb.ForeColor(Color.FromArgb(255, 255, 255));
                Console.ForegroundColor = ConsoleColor.White;
                break;
            }

            sb.Append("[");
            if (message.ChatTag.HasFlag(ChatTag.Gm))
                sb.Append("<GM>");
            if (message.ChatTag.HasFlag(ChatTag.Afk))
                sb.Append("<AFK>");
            if (message.ChatTag.HasFlag(ChatTag.Dnd))
                sb.Append("<DND>");
            sb.Append(message.Sender.Sender);
            sb.Append("]: ");
            sb.Append(message.Message);

            LogLine(sb.ToString(), message.Language == Language.Addon ? LogLevel.Debug : LogLevel.Info);
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            lock (Console.Out)
            {
                if (level >= LogLevel)
                {
                    Console.Write(message);
                    _logFile.Write(String.Format("{0} : {1}", DateTime.Now, message));
                    Console.ResetColor();
                }
            }
        }

        public void LogLine(LogLevel level = LogLevel.Info)
        {
            lock (Console.Out)
            {
                if (level >= LogLevel)
                {
                    Console.WriteLine();
                    _logFile.WriteLine();
                    Console.ResetColor();
                }
            }
        }

        public void LogLine(string message, LogLevel level = LogLevel.Info)
        {
            lock (Console.Out)
            {
                if (level >= LogLevel)
                {
                    Console.WriteLine(message);
                    _logFile.WriteLine(String.Format("{0} : {1}", DateTime.Now, message));
                    Console.ResetColor();
                }
            }
        }

        public void LogException(string message)
        {
            _logFile.WriteLine(String.Format("{0} : Exception: {1}", DateTime.Now, message));
            _logFile.WriteLine((new StackTrace(1, true)).ToString());
            throw new Exception(message);
        }

        public void LogException(Exception ex)
        {
            _logFile.WriteLine(String.Format("{0} : Exception: {1} : Stacktrace : {2}", DateTime.Now, ex.Message, ex.StackTrace));
            throw ex;
        }

        public string ReadLine()
        {
            //! We don't want to clutter the console, so wait for input before printing output
            string ret;
            lock (Console.Out)
                ret = Console.ReadLine();

            return ret;
        }

        public int Read()
        {
            //! We don't want to clutter the console, so wait for input before printing output
            int ret;
            lock (Console.Out)
                ret = Console.Read();

            return ret;
        }

        public ConsoleKeyInfo ReadKey()
        {
            //! We don't want to clutter the console, so wait for input before printing output
            ConsoleKeyInfo ret;
            lock (Console.Out)
                ret = Console.ReadKey();

            return ret;
        }

        #endregion
    }
}
