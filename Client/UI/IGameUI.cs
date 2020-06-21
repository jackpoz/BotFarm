using System;
using Client.Authentication;
using Client.World;
using Client.Chat;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Client.UI
{
    public abstract class IGameUI
    {
        public abstract IGame Game { get; }

        public abstract LogLevel LogLevel { get; set; }

        public abstract void Update();
        public abstract Task Exit();

        #region Packet handler presenters

        public abstract void PresentRealmList(WorldServerList realmList);
        public abstract void PresentCharacterList(Character[] characterList);

        public abstract void PresentChatMessage(ChatMessage message);

        #endregion

        #region UI Output

        public abstract void Log(string message, LogLevel level = LogLevel.Info);
        public abstract void LogLine(string message, LogLevel level = LogLevel.Info);
        [Conditional("DEBUG")]
        public abstract void LogDebug(string message);
        public abstract void LogException(string message);

        public abstract void LogException(Exception ex);

        #endregion

        #region UI Input

        public abstract string ReadLine();
        public abstract int Read();
        public abstract ConsoleKeyInfo ReadKey();

        #endregion
    }
}
