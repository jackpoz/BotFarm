using Client.Authentication;
using Client.World;

namespace Client.UI
{
    public interface IGameUI
    {
        IGame Game { get; set; }

        void Update();
        void Exit();

        void PresentRealmList(WorldServerList realmList);
        void PresentCharacterList(Character[] characterList);

        void Log(string message, LogLevel level = LogLevel.Info);
        void LogLine(string message, LogLevel level = LogLevel.Info);
    }
}
