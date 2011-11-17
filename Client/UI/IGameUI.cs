using System;
using Client.Authentication;
using Client.World;
using Client.Chat;

namespace Client.UI
{
    public interface IGameUI
    {
        IGame Game { get; set; }

        LogLevel LogLevel { get; set; }

        void Update();
        void Exit();

        #region Packet handler presenters

        void PresentRealmList(WorldServerList realmList);
        void PresentCharacterList(Character[] characterList);

        void PresentChatMessage(ChatMessage message);

        #endregion

        #region UI Output

        void Log(string message, LogLevel level = LogLevel.Info);
        void LogLine(string message, LogLevel level = LogLevel.Info);
        void LogException(string message);

        #endregion

        #region UI Input

        string ReadLine();
        int Read();
        ConsoleKeyInfo ReadKey();

        #endregion
    }
}
