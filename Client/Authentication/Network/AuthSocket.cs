using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Client.Crypto;
using Client.UI;
using System.Threading;

namespace Client.Authentication.Network
{
    public class AuthSocket : GameSocket
    {
        public BigInteger Key { get; private set; }
        byte[] m2;

        BinaryWriter output;
        BinaryReader input;

        private string Username;
        private byte[] PasswordHash;

        private string Hostname;
        private int Port;

        Dictionary<AuthCommand, CommandHandler> Handlers;

        public AuthSocket(IGame program, string hostname, int port, string username, string password)
        {
            this.Game = program;

            this.Username = username;
            this.Hostname = hostname;
            this.Port = port;
            
            string authstring = string.Format("{0}:{1}", this.Username, password);

            PasswordHash = HashAlgorithm.SHA1.Hash(Encoding.ASCII.GetBytes(authstring.ToUpper()));

            ReceiveData = new byte[1];
        }

        ~AuthSocket()
        {
            Dispose();
        }

        void SendLogonChallenge()
        {
            Game.UI.LogLine("Sending logon challenge", LogLevel.Debug);

            ClientAuthChallenge challenge = new ClientAuthChallenge()
            {
                username = Username,
                IP = BitConverter.ToUInt32((connection.Client.LocalEndPoint as IPEndPoint).Address.GetAddressBytes(), 0)
            };

            challenge.Send(output);
            ReadCommand();
        }

        #region Handlers

        public override void InitHandlers()
        {
            Handlers = new Dictionary<AuthCommand, CommandHandler>();

            Handlers[AuthCommand.LOGON_CHALLENGE] = HandleRealmLogonChallenge;
            Handlers[AuthCommand.LOGON_PROOF] = HandleRealmLogonProof;
            Handlers[AuthCommand.REALM_LIST] = HandleRealmList;
        }

        void HandleRealmLogonChallenge()
        {
            ServerAuthChallenge challenge = new ServerAuthChallenge(new BinaryReader(connection.GetStream()));

            switch (challenge.error)
            {
                case AuthResult.SUCCESS:
                {
                    Game.UI.LogLine("Received logon challenge", LogLevel.Debug);

                    BigInteger N, A, B, a, u, x, S, salt, unk1, g, k;
                    k = new BigInteger(3);

                    #region Receive and initialize

                    B = challenge.B.ToBigInteger();            // server public key
                    g = challenge.g.ToBigInteger();
                    N = challenge.N.ToBigInteger();            // modulus
                    salt = challenge.salt.ToBigInteger();
                    unk1 = challenge.unk3.ToBigInteger();

                    Game.UI.LogLine("---====== Received from server: ======---", LogLevel.Debug);
                    Game.UI.LogLine(string.Format("B={0}", B.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("N={0}", N.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("salt={0}", challenge.salt.ToHexString()), LogLevel.Debug);

                    #endregion

                    #region Hash password

                    x = HashAlgorithm.SHA1.Hash(challenge.salt, PasswordHash).ToBigInteger();

                    Game.UI.LogLine("---====== shared password hash ======---", LogLevel.Debug);
                    Game.UI.LogLine(string.Format("g={0}", g.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("x={0}", x.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("N={0}", N.ToCleanByteArray().ToHexString()), LogLevel.Debug);

                    #endregion

                    #region Create random key pair

                    var rand = System.Security.Cryptography.RandomNumberGenerator.Create();

                    do
                    {
                        byte[] randBytes = new byte[19];
                        rand.GetBytes(randBytes);
                        a = randBytes.ToBigInteger();

                        A = g.ModPow(a, N);
                    } while (A.ModPow(1, N) == 0);

                    Game.UI.LogLine("---====== Send data to server: ======---", LogLevel.Debug);
                    Game.UI.LogLine(string.Format("A={0}", A.ToCleanByteArray().ToHexString()), LogLevel.Debug);

                    #endregion

                    #region Compute session key

                    u = HashAlgorithm.SHA1.Hash(A.ToCleanByteArray(), B.ToCleanByteArray()).ToBigInteger();

                    // compute session key
                    // TODO: session key computation fails for some reason
                    //     all variables match exactly to the server side, but
                    //     S is different
                    S = (B - k * g.ModPow(x, N)).ModPow(a + u * x, N);

                    byte[] keyHash;
                    byte[] sData = S.ToCleanByteArray();
                    if (sData.Length < 32)
                    {
                        var tmpBuffer = new byte[32];
                        Buffer.BlockCopy(sData, 0, tmpBuffer, 32 - sData.Length, sData.Length);
                        sData = tmpBuffer;
                    }
                    byte[] keyData = new byte[40];
                    byte[] temp = new byte[16];

                    // take every even indices byte, hash, store in even indices
                    for (int i = 0; i < 16; ++i)
                        temp[i] = sData[i * 2];
                    keyHash = HashAlgorithm.SHA1.Hash(temp);
                    for (int i = 0; i < 20; ++i)
                        keyData[i * 2] = keyHash[i];

                    // do the same for odd indices
                    for (int i = 0; i < 16; ++i)
                        temp[i] = sData[i * 2 + 1];
                    keyHash = HashAlgorithm.SHA1.Hash(temp);
                    for (int i = 0; i < 20; ++i)
                        keyData[i * 2 + 1] = keyHash[i];

                    Key = keyData.ToBigInteger();

                    Game.UI.LogLine("---====== Compute session key ======---", LogLevel.Debug);
                    Game.UI.LogLine(string.Format("u={0}", u.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("S={0}", S.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("K={0}", Key.ToCleanByteArray().ToHexString()), LogLevel.Debug);

                    #endregion

                    #region Generate crypto proof

                    // XOR the hashes of N and g together
                    byte[] gNHash = new byte[20];

                    byte[] nHash = HashAlgorithm.SHA1.Hash(N.ToCleanByteArray());
                    for (int i = 0; i < 20; ++i)
                        gNHash[i] = nHash[i];
                    Game.UI.LogLine(string.Format("nHash={0}", nHash.ToHexString()), LogLevel.Debug);

                    byte[] gHash = HashAlgorithm.SHA1.Hash(g.ToCleanByteArray());
                    for (int i = 0; i < 20; ++i)
                        gNHash[i] ^= gHash[i];
                    Game.UI.LogLine(string.Format("gHash={0}", gHash.ToHexString()), LogLevel.Debug);

                    // hash username
                    byte[] userHash = HashAlgorithm.SHA1.Hash(Encoding.ASCII.GetBytes(Username));

                    // our proof
                    byte[] m1Hash = HashAlgorithm.SHA1.Hash
                    (
                        gNHash,
                        userHash,
                        challenge.salt,
                        A.ToCleanByteArray(),
                        B.ToCleanByteArray(),
                        Key.ToCleanByteArray()
                    );

                    Game.UI.LogLine("---====== Client proof: ======---", LogLevel.Debug);
                    Game.UI.LogLine(string.Format("gNHash={0}", gNHash.ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("userHash={0}", userHash.ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("salt={0}", challenge.salt.ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("A={0}", A.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("B={0}", B.ToCleanByteArray().ToHexString()), LogLevel.Debug);
                    Game.UI.LogLine(string.Format("key={0}", Key.ToCleanByteArray().ToHexString()), LogLevel.Debug);

                    Game.UI.LogLine("---====== Send proof to server: ======---", LogLevel.Debug);
                    Game.UI.LogLine(string.Format("M={0}", m1Hash.ToHexString()), LogLevel.Debug);

                    // expected proof for server
                    m2 = HashAlgorithm.SHA1.Hash(A.ToCleanByteArray(), m1Hash, keyData);

                    #endregion

                    #region Send proof

                    ClientAuthProof proof = new ClientAuthProof()
                    {
                        A = A.ToCleanByteArray(),
                        M1 = m1Hash,
                        crc = new byte[20],
                    };

                    Game.UI.LogLine("Sending logon proof", LogLevel.Debug);
                    proof.Send(output);

                    #endregion

                    break;
                }
                case AuthResult.NO_MATCH:
                    Game.UI.LogLine("Unknown account name", LogLevel.Error);
                    break;
                case AuthResult.ACCOUNT_IN_USE:
                    Game.UI.LogLine("Account already logged in", LogLevel.Error);
                    break;
                case AuthResult.WRONG_BUILD_NUMBER:
                    Game.UI.LogLine("Wrong build number", LogLevel.Error);
                    break;
            }

            // get next command
            ReadCommand();
        }

        void HandleRealmLogonProof()
        {
            ServerAuthProof proof = new ServerAuthProof(new BinaryReader(connection.GetStream()));

            switch (proof.error)
            {
                case AuthResult.UPDATE_CLIENT:
                    Game.UI.LogLine("Client update requested");
                    break;
                case AuthResult.NO_MATCH:
                case AuthResult.UNKNOWN2:
                    Game.UI.LogLine("Wrong password or invalid account or authentication error", LogLevel.Error);
                    Thread.Sleep(1000);
                    break;
                case AuthResult.WRONG_BUILD_NUMBER:
                    Game.UI.LogLine("Wrong build number", LogLevel.Error);
                    break;
                default:
                    if (proof.error != AuthResult.SUCCESS)
                        Game.UI.LogLine(string.Format("Unkown error {0}", proof.error), LogLevel.Error);
                    break;
            }

            if (proof.error != AuthResult.SUCCESS)
            {
                SendLogonChallenge();
                return;
            }

            Game.UI.LogLine("Received logon proof", LogLevel.Debug);

            bool equal = true;
            equal = m2 != null && m2.Length == 20;
            for (int i = 0; i < m2.Length && equal; ++i)
                if (!(equal = m2[i] == proof.M2[i]))
                    break;

            if (!equal)
                Game.UI.LogLine("Server auth failed!", LogLevel.Error);
            else
            {
                Game.UI.LogLine("Authentication succeeded!");
                Game.UI.LogLine("Requesting realm list", LogLevel.Detail);
                output.Write((byte)AuthCommand.REALM_LIST);
                output.Write((uint)0);
            }

            // get next command
            ReadCommand();
        }

        void HandleRealmList()
        {
            BinaryReader reader = new BinaryReader(connection.GetStream());

            uint size = reader.ReadUInt16();
            WorldServerList realmList = new WorldServerList(reader);
            Game.UI.LogLine("Received realm list", LogLevel.Debug);

            Game.UI.PresentRealmList(realmList);
        }

        #endregion

        #region GameSocket Members

        public override void Start()
        {
            ReadCommand();
        }

        private void ReadCommand()
        {
            try
            {
                this.connection.Client.BeginReceive
                (
                    ReceiveData, 0, 1,    // buffer and buffer bounds
                    SocketFlags.None,    // flags for the read
                    this.ReadCallback,    // callback to handle completion
                    null                // state object
                );
            }
            catch
            {
            }
        }

        protected void ReadCallback(IAsyncResult result)
        {
            try
            {
                int size = this.connection.Client.EndReceive(result);

                if (size == 0)
                {
                    Game.UI.LogLine("Server has disconnected.", LogLevel.Info);
                    Game.Exit();
                }

                AuthCommand command = (AuthCommand)ReceiveData[0];

                CommandHandler handler;
                if (Handlers.TryGetValue(command, out handler))
                    handler();
                else
                    Game.UI.LogLine(string.Format("Unkown or unhandled command '{0}'", command), LogLevel.Debug);
            }
            // these exceptions can happen as race condition on shutdown
            catch (ObjectDisposedException)
            { }
            catch (NullReferenceException)
            { }
            catch (SocketException)
            { }
        }

        public override bool Connect()
        {
            try
            {
                Game.UI.Log("Connecting to realmlist... ");

                connection = new TcpClient(this.Hostname, this.Port);
                output = new BinaryWriter(connection.GetStream());
                input = new BinaryReader(connection.GetStream());

                Game.UI.LogLine("done!", LogLevel.Debug);

                SendLogonChallenge();
            }
            catch (SocketException ex)
            {
                Game.UI.LogLine(string.Format("Auth socket failed. ({0})", (SocketError)ex.ErrorCode), LogLevel.Error);
                return false;
            }

            return true;
        }

        #endregion
    }
}
