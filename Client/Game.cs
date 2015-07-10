using System.Numerics;
using System.Threading;
using Client.Authentication;
using Client.Authentication.Network;
using Client.UI;
using Client.World.Network;
using Client.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client
{
    public interface IGame
    {
        BigInteger Key { get; }
        string Username { get; }

        IGameUI UI { get; }

        GameWorld World { get; }

        void ConnectTo(WorldServerInfo server);

        void Start();

        void Reconnect();

        void NoCharactersFound();

        void InvalidCredentials();

        Task Exit();

        void SendPacket(OutPacket packet);

        void HandleTriggerInput(TriggerActionType type, params object[] inputs);
    }
}
