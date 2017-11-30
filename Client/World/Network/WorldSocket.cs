using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Client.Authentication;
using Client.UI;
using System.IO;

namespace Client.World.Network
{
    public partial class WorldSocket : GameSocket
    {
        static HashSet<WorldCommand> IgnoredOpcodes = new HashSet<WorldCommand>()
        {
            WorldCommand.SMSG_ADDON_INFO,
            WorldCommand.SMSG_CLIENTCACHE_VERSION,
            WorldCommand.SMSG_TUTORIAL_FLAGS,
            WorldCommand.SMSG_WARDEN_DATA,
            WorldCommand.MSG_SET_DUNGEON_DIFFICULTY,
            WorldCommand.SMSG_ACCOUNT_DATA_TIMES,
            WorldCommand.SMSG_FEATURE_SYSTEM_STATUS,
            WorldCommand.SMSG_MOTD,
            WorldCommand.SMSG_GUILD_EVENT,
            WorldCommand.SMSG_GUILD_BANK_LIST,
            WorldCommand.SMSG_GUILD_ROSTER,
            WorldCommand.SMSG_LEARNED_DANCE_MOVES,
            WorldCommand.SMSG_SET_PCT_SPELL_MODIFIER,
            WorldCommand.SMSG_CONTACT_LIST,
            WorldCommand.SMSG_BINDPOINTUPDATE,
            WorldCommand.SMSG_INSTANCE_DIFFICULTY,
            WorldCommand.SMSG_SEND_UNLEARN_SPELLS,
            WorldCommand.SMSG_ACTION_BUTTONS,
            WorldCommand.SMSG_EQUIPMENT_SET_LIST,
            WorldCommand.SMSG_LOGIN_SETTIMESPEED,
            WorldCommand.SMSG_INIT_WORLD_STATES,
            WorldCommand.SMSG_UPDATE_WORLD_STATE,
            WorldCommand.SMSG_WEATHER,
            WorldCommand.SMSG_TIME_SYNC_REQ,
            WorldCommand.SMSG_NOTIFICATION,
            WorldCommand.SMSG_SPLINE_MOVE_STOP_SWIM,
            WorldCommand.SMSG_SPLINE_MOVE_SET_WALK_MODE,
            WorldCommand.SMSG_SPLINE_MOVE_SET_RUN_MODE,
            WorldCommand.SMSG_SPLINE_MOVE_START_SWIM,
            WorldCommand.MSG_MOVE_SET_FACING,
            WorldCommand.SMSG_TRIGGER_CINEMATIC,
            WorldCommand.SMSG_UPDATE_INSTANCE_OWNERSHIP,
            WorldCommand.SMSG_EMOTE,
            WorldCommand.SMSG_LFG_OTHER_TIMEDOUT,
            WorldCommand.SMSG_FORCE_SWIM_SPEED_CHANGE,
            WorldCommand.SMSG_FORCE_SWIM_BACK_SPEED_CHANGE,
            WorldCommand.SMSG_FORCE_RUN_SPEED_CHANGE,
            WorldCommand.SMSG_FORCE_RUN_BACK_SPEED_CHANGE,
            WorldCommand.SMSG_FORCE_FLIGHT_SPEED_CHANGE,
            WorldCommand.SMSG_FORCE_FLIGHT_SPEED_CHANGE,
            WorldCommand.SMSG_FORCE_FLIGHT_BACK_SPEED_CHANGE,
            WorldCommand.CMSG_UNKNOWN_1303,
            WorldCommand.SMSG_ITEM_TIME_UPDATE,
            WorldCommand.SMSG_SPLINE_MOVE_UNROOT,
            WorldCommand.SMSG_SPELLENERGIZELOG,
            WorldCommand.SMSG_PET_SPELLS,
            WorldCommand.SMSG_MOVE_SET_CAN_FLY,
            WorldCommand.SMSG_RECEIVED_MAIL,
            WorldCommand.MSG_CHANNEL_START,
            WorldCommand.MSG_CHANNEL_UPDATE,
            WorldCommand.SMSG_FRIEND_STATUS,
            WorldCommand.SMSG_UNKNOWN_1236,
            WorldCommand.SMSG_UNKNOWN_1235,
            WorldCommand.SMSG_SPLINE_MOVE_UNSET_FLYING,
            WorldCommand.SMSG_SPLINE_MOVE_ROOT,
            WorldCommand.SMSG_GAMEOBJECT_DESPAWN_ANIM,
            WorldCommand.SMSG_DISMOUNT,
            WorldCommand.CMSG_MOVE_FALL_RESET,
        };

        static HashSet<WorldCommand> NotYetImplementedOpcodes = new HashSet<WorldCommand>()
        {
            WorldCommand.SMSG_SET_PROFICIENCY,
            WorldCommand.SMSG_POWER_UPDATE,
            WorldCommand.SMSG_CANCEL_COMBAT,
            WorldCommand.SMSG_TALENTS_INFO,
            WorldCommand.SMSG_INITIAL_SPELLS,
            WorldCommand.SMSG_INITIALIZE_FACTIONS,
            WorldCommand.SMSG_SET_FORCED_REACTIONS,
            WorldCommand.SMSG_COMPRESSED_UPDATE_OBJECT,
            WorldCommand.SMSG_AURA_UPDATE,
            WorldCommand.SMSG_DESTROY_OBJECT,
            WorldCommand.SMSG_MONSTER_MOVE,
            WorldCommand.SMSG_SPELL_GO,
            WorldCommand.SMSG_AURA_UPDATE_ALL,
            WorldCommand.SMSG_AI_REACTION,
            WorldCommand.SMSG_HIGHEST_THREAT_UPDATE,
            WorldCommand.SMSG_THREAT_UPDATE,
            WorldCommand.MSG_MOVE_START_FORWARD,
            WorldCommand.MSG_MOVE_JUMP,
            WorldCommand.MSG_MOVE_START_BACKWARD,
            WorldCommand.MSG_MOVE_START_STRAFE_RIGHT,
            WorldCommand.MSG_MOVE_START_TURN_RIGHT,
            WorldCommand.MSG_MOVE_START_TURN_LEFT,
            WorldCommand.MSG_MOVE_STOP,
            WorldCommand.MSG_MOVE_STOP_TURN,
            WorldCommand.MSG_MOVE_HEARTBEAT,
            WorldCommand.MSG_MOVE_FALL_LAND,
            WorldCommand.SMSG_SPELL_START,
            WorldCommand.SMSG_SPELLHEALLOG,
            WorldCommand.SMSG_ATTACKSTART,
            WorldCommand.SMSG_ATTACKERSTATEUPDATE,
            WorldCommand.SMSG_ATTACKSTOP,
            WorldCommand.SMSG_THREAT_REMOVE,
            WorldCommand.SMSG_PERIODICAURALOG,
            WorldCommand.MSG_MOVE_START_STRAFE_LEFT,
            WorldCommand.MSG_MOVE_STOP_STRAFE,
            WorldCommand.SMSG_SPELLNONMELEEDAMAGELOG,
            WorldCommand.SMSG_LOOT_LIST,
            WorldCommand.SMSG_THREAT_CLEAR,
            WorldCommand.SMSG_GM_MESSAGECHAT,
            WorldCommand.SMSG_SET_FLAT_SPELL_MODIFIER,
            WorldCommand.SMSG_SPELL_FAILURE,
            WorldCommand.SMSG_SPELL_FAILED_OTHER,
            WorldCommand.SMSG_MONSTER_MOVE_TRANSPORT,
            WorldCommand.SMSG_MOVE_WATER_WALK,
            WorldCommand.SMSG_BREAK_TARGET,
            WorldCommand.SMSG_DEATH_RELEASE_LOC,
            WorldCommand.SMSG_SET_PHASE_SHIFT,
            WorldCommand.SMSG_PARTY_MEMBER_STATS
        };

        WorldServerInfo ServerInfo;

        private long transferred;
        public long Transferred { get { return transferred; } }

        private long sent;
        public long Sent { get { return sent; } }

        private long received;
        public long Received { get { return received; } }

        public override string LastOutOpcodeName
        {
            get
            {
                return LastOutOpcode?.ToString();
            }
        }
        public WorldCommand? LastOutOpcode
        {
            get;
            protected set;
        }
        public override DateTime LastOutOpcodeTime => _lastOutOpcodeTime;
        protected DateTime _lastOutOpcodeTime;
        public override string LastInOpcodeName
        {
            get
            {
                return LastInOpcode?.ToString();
            }
        }
        public WorldCommand? LastInOpcode
        {
            get;
            protected set;
        }
        public override DateTime LastInOpcodeTime => _lastInOpcodeTime;
        protected DateTime _lastInOpcodeTime;

        BatchQueue<InPacket> packetsQueue = new BatchQueue<InPacket>();

        public WorldSocket(IGame program, WorldServerInfo serverInfo)
        {
            Game = program;
            ServerInfo = serverInfo;
        }

        #region Handler registration

        Dictionary<WorldCommand, PacketHandler> PacketHandlers;

        public override void InitHandlers()
        {
            PacketHandlers = new Dictionary<WorldCommand, PacketHandler>();

            RegisterHandlersFrom(this);
            RegisterHandlersFrom(Game);
        }

        void RegisterHandlersFrom(object obj)
        {
            // create binding flags to discover all non-static methods
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            IEnumerable<PacketHandlerAttribute> attributes;
            foreach (var method in obj.GetType().GetMethods(flags))
            {
                if (!method.TryGetAttributes(false, out attributes))
                    continue;

                PacketHandler handler = (PacketHandler)PacketHandler.CreateDelegate(typeof(PacketHandler), obj, method);

                foreach (var attribute in attributes)
                {
                    Game.UI.LogDebug(string.Format("Registered '{0}.{1}' to '{2}'", obj.GetType().Name, method.Name, attribute.Command));
                    PacketHandlers[attribute.Command] = handler;
                }
            }
        }

        #endregion

        #region Asynchronous Reading

        int Index;
        int Remaining;
        
        private void ReadAsync(EventHandler<SocketAsyncEventArgs> callback, object state = null)
        {
            if (Disposing)
                return;

            SocketAsyncState = state;
            SocketArgs.SetBuffer(ReceiveData, Index, Remaining);
            SocketCallback = callback;
            connection.Client.ReceiveAsync(SocketArgs);
        }

        /// <summary>
        /// Determines how large the incoming header will be by
        /// inspecting the first byte, then initiates reading the header.
        /// </summary>
        private void ReadSizeCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    // TODO: world server disconnect
                    Game.UI.LogLine("Server has closed the connection");
                    Game.Reconnect();
                    return;
                }

                Interlocked.Increment(ref transferred);
                Interlocked.Increment(ref received);

                authenticationCrypto.Decrypt(ReceiveData, 0, 1);
                if ((ReceiveData[0] & 0x80) != 0)
                {
                    // need to resize the buffer
                    byte temp = ReceiveData[0];
                    ReserveData(5);
                    ReceiveData[0] = (byte)((0x7f & temp));

                    Remaining = 4;
                }
                else
                    Remaining = 3;

                Index = 1;
                ReadAsync(ReadHeaderCallback);
            }
            // these exceptions can happen as race condition on shutdown
            catch(ObjectDisposedException ex)
            {
                Game.UI.LogException(ex);
            }
            catch(NullReferenceException ex)
            {
                Game.UI.LogException(ex);
            }
            catch(InvalidOperationException ex)
            {
                Game.UI.LogException(ex);
            }
            catch(SocketException ex)
            {
                Game.UI.LogException(ex);
                Game.UI.LogLine("Last InPacket: " + LastInOpcodeName, LogLevel.Warning);
                Game.UI.LogLine("Last OutPacket: " + LastOutOpcodeName, LogLevel.Warning);
                Game.Reconnect();
            }
        }

        /// <summary>
        /// Reads the rest of the incoming header.
        /// </summary>
        private void ReadHeaderCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    // TODO: world server disconnect
                    Game.UI.LogLine("Server has closed the connection");
                    Game.Reconnect();
                    return;
                }

                Interlocked.Add(ref transferred, bytesRead);
                Interlocked.Add(ref received, bytesRead);

                if (bytesRead == Remaining)
                {
                    // finished reading header
                    // the first byte was decrypted already, so skip it
                    authenticationCrypto.Decrypt(ReceiveData, 1, ReceiveDataLength - 1);
                    ServerHeader header = new ServerHeader(ReceiveData, ReceiveDataLength);

                    Game.UI.LogDebug(header.ToString());
                    if (header.InputDataLength > 5 || header.InputDataLength < 4)
                        Game.UI.LogException(String.Format("Header.InputDataLength invalid: {0}", header.InputDataLength));

                    if (header.Size > 0)
                    {
                        // read the packet payload
                        Index = 0;
                        Remaining = header.Size;
                        ReserveData(header.Size);
                        ReadAsync(ReadPayloadCallback, header);
                    }
                    else
                    {
                        // the packet is just a header, start next packet
                        QueuePacket(new InPacket(header));
                        Start();
                    }
                }
                else
                {
                    // more header to read
                    Index += bytesRead;
                    Remaining -= bytesRead;
                    ReadAsync(ReadHeaderCallback);
                }
            }
            // these exceptions can happen as race condition on shutdown
            catch (ObjectDisposedException ex)
            {
                Game.UI.LogException(ex);
            }
            catch (NullReferenceException ex)
            {
                Game.UI.LogException(ex);
            }
            catch (SocketException ex)
            {
                Game.UI.LogException(ex);
            }
        }

        /// <summary>
        /// Reads the payload data.
        /// </summary>
        private void ReadPayloadCallback(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                int bytesRead = e.BytesTransferred;
                if (bytesRead == 0)
                {
                    // TODO: world server disconnect
                    Game.UI.LogLine("Server has closed the connection");
                    Game.Reconnect();
                    return;
                }

                Interlocked.Add(ref transferred, bytesRead);
                Interlocked.Add(ref received, bytesRead);

                if (bytesRead == Remaining)
                {
                    // get header and packet, handle it
                    ServerHeader header = (ServerHeader)SocketAsyncState;
                    QueuePacket(new InPacket(header, ReceiveData, ReceiveDataLength));

                    // start new asynchronous read
                    Start();
                }
                else
                {
                    // more payload to read
                    Index += bytesRead;
                    Remaining -= bytesRead;
                    ReadAsync(ReadPayloadCallback, SocketAsyncState);
                }
            }
            catch(NullReferenceException ex)
            {
                Game.UI.LogException(ex);
            }
            catch(SocketException ex)
            {
                Game.UI.LogException(ex);
                Game.Reconnect();
                return;
            }
        }

        #endregion

        public void HandlePackets()
        {
            foreach (var packet in packetsQueue.BatchDequeue())
                HandlePacket(packet);
        }

        private void HandlePacket(InPacket packet)
        {
            try
            {
                LastInOpcode = packet.Header.Command;
                _lastInOpcodeTime = DateTime.Now;
                PacketHandler handler;
                if (PacketHandlers.TryGetValue(packet.Header.Command, out handler))
                {
                    Game.UI.LogDebug(string.Format("Received {0}", packet.Header.Command));
                    handler(packet);
                }
                else
                {
                    if (!IgnoredOpcodes.Contains(packet.Header.Command) && !NotYetImplementedOpcodes.Contains(packet.Header.Command))
                        Game.UI.LogDebug(string.Format("Unknown or unhandled command '{0}'", packet.Header.Command));
                }
                Game.HandleTriggerInput(TriggerActionType.Opcode, packet);
            }
            catch(Exception ex)
            {
                Game.UI.LogException(ex);
            }
            finally
            {
                packet.Dispose();
            }
        }

        private void QueuePacket(InPacket packet)
        {
            packetsQueue.Enqueue(packet);
        }

        #region GameSocket Members

        public override void Start()
        {
            ReserveData(4, true);
            Index = 0;
            Remaining = 1;
            ReadAsync(ReadSizeCallback);
        }

        public override bool Connect()
        {
            try
            {
                Game.UI.LogDebug(string.Format("Connecting to realm {0}... ", ServerInfo.Name));

                if (connection != null)
                    connection.Close();
                connection = new TcpClient(ServerInfo.Address, ServerInfo.Port);

                Game.UI.LogDebug("done!");
            }
            catch (SocketException ex)
            {
                Game.UI.LogLine(string.Format("World connect failed. ({0})", (SocketError)ex.ErrorCode), LogLevel.Error);
                return false;
            }

            return true;
        }

        #endregion

        public void Send(OutPacket packet)
        {
            LastOutOpcode = packet.Header.Command;
            _lastOutOpcodeTime = DateTime.Now;
            byte[] data = packet.Finalize(authenticationCrypto);

            try
            {
                connection.Client.Send(data, 0, data.Length, SocketFlags.None);
            }
            catch(ObjectDisposedException ex)
            {
                Game.UI.LogException(ex);
            }
            catch(EndOfStreamException ex)
            {
                Game.UI.LogException(ex);
            }

            Interlocked.Add(ref transferred, data.Length);
            Interlocked.Add(ref sent, data.Length);
        }
    }
}
