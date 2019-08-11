using BotFarm.Properties;
using Client;
using Client.UI;
using Client.World;
using Client.World.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.World.Definitions;
using Client.World.Entities;
using DetourCLI;
using MapCLI;
using DBCStoresCLI;
using BotFarm.AI;
using Client.Chat.Definitions;

namespace BotFarm
{
    class BotGame : AutomatedGame
    {
        public BotBehaviorSettings Behavior
        {
            get;
            private set;
        }

        #region Player members
        DateTime CorpseReclaim;
        public ulong TraderGUID
        {
            get;
            private set;
        }
        public HashSet<ulong> TradedGUIDs
        {
            get;
            private set;
        } = new HashSet<ulong>();
        #endregion

        public BotGame(string hostname, int port, string username, string password, int realmId, int character, BotBehaviorSettings behavior)
            : base(hostname, port, username, password, realmId, character)
        {
            this.Behavior = behavior;

            #region AutoResurrect
            if (Behavior.AutoResurrect)
            {
                // Resurrect if bot reaches 0 hp
                AddTrigger(new Trigger(new[] 
                { 
                    new UpdateFieldTriggerAction((int)UnitField.UNIT_FIELD_HEALTH, 0)
                }, () =>
                   {
                       CancelActionsByFlag(ActionFlag.Movement);
                       Resurrect();
                   }));

                // Resurrect sequence
                AddTrigger(new Trigger(new TriggerAction[] 
                { 
                    new UpdateFieldTriggerAction((int)PlayerField.PLAYER_FLAGS, (uint)PlayerFlags.PLAYER_FLAGS_GHOST, () =>
                        {
                            OutPacket corpseQuery = new OutPacket(WorldCommand.MSG_CORPSE_QUERY);
                            SendPacket(corpseQuery);
                        }),
                    new OpcodeTriggerAction(WorldCommand.MSG_CORPSE_QUERY, packet =>
                    {
                        var inPacket = packet as InPacket;
                        if (inPacket == null)
                            return false;

                        bool found = inPacket.ReadByte() != 0;
                        if (found)
                        {
                            var mapId = inPacket.ReadInt32();

                            var corpsePosition = new Position(inPacket.ReadSingle(),
                                                              inPacket.ReadSingle(),
                                                              inPacket.ReadSingle(),
                                                              0.0f,
                                                              inPacket.ReadInt32());
                            Player.CorpsePosition = corpsePosition.GetPosition();

                            if (mapId == corpsePosition.MapID)
                            {
                                MoveTo(corpsePosition);
                                return true;
                            }
                        }

                        return false;
                    }),
                    new CustomTriggerAction(TriggerActionType.DestinationReached, (inputs) =>
                    {
                        if (Player.IsGhost && (Player.CorpsePosition - Player).Length <= 39f)
                        {
                            if (DateTime.Now > CorpseReclaim)
                                return true;
                            else
                                ScheduleAction(() => HandleTriggerInput(TriggerActionType.DestinationReached, inputs), CorpseReclaim.AddSeconds(1));
                        }

                        return false;
                    },() => 
                      {
                          OutPacket reclaimCorpse = new OutPacket(WorldCommand.CMSG_RECLAIM_CORPSE);
                          reclaimCorpse.Write(Player.GUID);
                          SendPacket(reclaimCorpse);
                      })
                }, null));
            }
            #endregion
        }

        public override void Start()
        {
            base.Start();

            // Anti-kick for being afk
            ScheduleAction(() => DoTextEmote(TextEmote.Yawn), DateTime.Now.AddMinutes(5), new TimeSpan(0, 5, 0));
            ScheduleAction(() =>
            {
                if (LoggedIn)
                    SendPacket(new OutPacket(WorldCommand.CMSG_KEEP_ALIVE));
            }, DateTime.Now.AddSeconds(15), new TimeSpan(0, 0, 30));

            #region Begger
            if (Behavior.Begger)
            {
                PushStrategicAI(new BeggerAI());
            }
            #endregion

            #region FollowGroupLeader
            if (Behavior.FollowGroupLeader)
            {
                PushStrategicAI(new FollowGroupLeaderAI());
            }
            #endregion

            #region Explorer
            if (Behavior.Explorer)
            {
                AchievementExploreLocation targetLocation = null;
                List<AchievementExploreLocation> missingLocations = null;
                Position currentPosition = new Position();

                ScheduleAction(() =>
                {
                    if (!Player.IsAlive)
                        return;

                    if (targetLocation != null)
                    {
                        if (!HasExploreCriteria(targetLocation.CriteriaID) && (currentPosition - Player).Length > MovementEpsilon)
                        {
                            currentPosition = Player.GetPosition();
                            return;
                        }

                        targetLocation = null;
                    }

                    currentPosition = Player.GetPosition();

                    if (missingLocations == null)
                        missingLocations = DBCStores.GetAchievementExploreLocations(Player.X, Player.Y, Player.Z, Player.MapID);

                    missingLocations = missingLocations.Where(loc => !HasExploreCriteria(loc.CriteriaID)).ToList();
                    if (missingLocations.Count == 0)
                    {
                        CancelActionsByFlag(ActionFlag.Movement);
                        return;
                    }

                    float closestDistance = float.MaxValue;
                    var playerPosition = new Point(Player.X, Player.Y, Player.Z);
                    foreach (var missingLoc in missingLocations)
                    {
                        float distance = (missingLoc.Location - playerPosition).Length;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            targetLocation = missingLoc;
                        }
                    }

                    MoveTo(new Position(targetLocation.Location.X, targetLocation.Location.Y, targetLocation.Location.Z, 0f, Player.MapID));
                }, DateTime.Now.AddSeconds(30), new TimeSpan(0, 0, 5));
            }
            #endregion

            // Skip Language.Universal since it's discarded anyway from Worldserver
            var languages = Enum.GetValues(typeof(Language))
                .Cast<Language>()
                .Except(new[] { Language.Universal })
                .ToArray();

            if (false)
            {
                var messages = new List<string>()
                {
                    "1 |_|r3",
                    "2 |r3",
                    "3 |cFF",
                    "4 |cGGGGGGGG",
                    "5 |1",
                    "6 |2",
                    "7 |3",
                    "8 |3-",
                    "9 |3-0",
                    "10 |3--1",
                    "11 |3-2",
                    "12 |3-5555",
                    "13 |3-9999999999999",
                    "14 |4",
                    ("15 |HKIA_LINK:KIA_R" + "" + " | h" + "|cffa335ee|Hitem:29434:0:0:0:0:0:0:0:80|h[Знак справедливости]|h|r" + " | h"),
                    "16 |cffFF4E00|Hlevelup:-1:LEVEL_UP_TYPE_CHARACTER|hHey, guess what I just got? its [Fetish of the Bloodthirsty Gladiator] |h|r",
                    "17 |cffFF4E00|Hlevelup:2:LEVEL_UP_TYPE_CHARACTER|h[Level 2]|h|r",
                    "18 |cffa335ee|Hitem:29434:0:0:0:0:0:0:0:80|h[Знак справедливости]|h|r",
                    "19 :||",
                    "20 ||",
                    "21 |",
                    "22 |cFFDDD000|Hquest:|htest|h|r",
                    "23 |cff808080|Hquest:9832:|h[The Second and Third Fragments]|h|r",
                    "24 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r",
                    "25 |cff808080|Hquest:9832:255|h[The Second and Third Fragments]|h|r",
                    "26 |cff808080|Hquest:9832:256|h[The Second and Third Fragments]|h|r|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r",
                    "27 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r |cff808080|Hquest:55555:70|h[The Second and Third Fragments]|h|r",
                    "28 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r |cff808080|Hquest:9832:|h[The Second and Third Fragments]|h|r",
                    "29 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r|cff808080|Hquest:",
                    "30 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r|cff808080|Hquest:|h[The Second and Third Fragments]|h|r",
                    "31 |cFFDDD000|Hquest:|hhey||r",
                    "32 |cff808080|Hquest:9832:0|h[The Second and Third Fragments]|h|r",
                    "33 |3-1 [Errormode]",
                    "34 |3-4 (keepsm)",
                    "35 \u0124cffffff00\u0124Hquest:11318\u0124h[А теперь гонки на баранах... Или вроде того.]\u0124h\u0124r",
                    "36 |cffffff00|Hquest:11318|h[А теперь гонки на баранах... Или вроде того.]|h|r",
                    "37 \u3071",
                    "38 \uD809\uDC01",
                    "39 |01",
                    "40 \\12401",
                    "41 |cff808080|Hquest:9832:70|h[\\124]|h|r",
                    "42 \u00A6",
                    "43 |cff808080|Hquest:9832:70|h[|01]|h|r",
                    "44 |cff808080|Hquest:9832:70 | h[test] | h | r",
                    "",
                    "Finished"
                };
                int index = 0;

                int actionId = -1;
                actionId = ScheduleAction(() =>
                {
                    if (index >= messages.Count)
                    {
                        CancelAction(actionId);
                        return;
                    }

                    var message = messages[index++].ToCString();

                    //foreach (var language in languages)
                    foreach(var language in new [] { Language.Common})
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Say);
                        var race = World.SelectedCharacter.Race;
                        response.Write((uint)language);
                        //response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 1));
            }

            if (false)
            {
                var random = new Random();
                var message = new byte[255];

                ScheduleAction(() =>
                {
                    random.NextBytes(message);

                    // remove filtered characters
                    for (int index = 0; index < message.Length; index++)
                    {
                        if (message[index] <= 31 && message[index] != 9)
                        {
                            message[index] = (byte)random.Next(1, byte.MaxValue);
                            // check the new character just set
                            index--;
                        }
                    }

                    // with the random result first
                    foreach (var language in languages)
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Whisper);
                        response.Write((uint)language);
                        response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                    // remove all '\0' characters as another try
                    for (int index = 0; index < message.Length; index++)
                        if (message[index] == 0)
                            message[index] = (byte)random.Next(32, byte.MaxValue);

                    foreach (var language in languages)
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Whisper);
                        response.Write((uint)language);
                        response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                    // try with max length 255
                    message[message.Length - 1] = 0;
                    foreach (var language in languages)
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Whisper);
                        response.Write((uint)language);
                        response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 0, 100));
            }

            if (false)
            {
                int counter = 15000;

                ScheduleAction(() =>
                {
                    var message = BitConverter.GetBytes(counter++);

                    foreach (var language in new[] { Language.Addon, Language.Common })
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Whisper);
                        response.Write((uint)language);
                        response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                    if (counter % 100 == 0)
                        Console.WriteLine("Sending character: " +  counter);

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 0, 10));
            }

            if (false)
            {
                var channels = new List<string>()
                {
                    "\u3071",
                    "\uD809\uDC01",
                    "|01fffff1f|cffffff1f|cfffff1ff|cffff1fff|cfff1ffff|cff1fffff|cffffffff|c1fffffff|cf1ffffff|r",
                };


                int index = 0;

                int actionId = -1;
                actionId = ScheduleAction(() =>
                {                    
                    if (index >= channels.Count)
                    {
                        CancelAction(actionId);
                        return;
                    }
                    var channelBytes = channels[index++].ToCString();

                    var response = new OutPacket(WorldCommand.CMSG_JOIN_CHANNEL);

                    response.Write((uint)0);
                    response.Write((byte)0);
                    response.Write((byte)0);
                    response.Write(channelBytes);
                    response.Write("".ToCString());
                    SendPacket(response);

                    response = new OutPacket(WorldCommand.CMSG_CHANNEL_INVITE);
                    response.Write(channelBytes);
                    response.Write("User".ToCString());
                    SendPacket(response);

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 5, 0));
            }

            if (false)
            {
                var channels = new List<string>()
                {
                    "test",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0004\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0004\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0377\u0377\u0377\u0377\u0000\u0000\u0000\u0000<\u0330\u0000\u0000\u0301\u0337\u0000\u0000<\u0330\u0000\u0000\u0377\u0337\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0247HW\u0377\u0177"
                    + "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000"
                    + "\u0361\u0000\u0000\u0005\u0002"
                    + "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000"
                    + "\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0340\u0250HW\u0377\u0177\u0000\u0000ЦHW\u0377\u0177\u0000\u0000P\u0250HW\u0377\u0177\u0000\u0000\u0362\u0000\u0000\u0005\u0002"
                    + "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000"
                    + "\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0020\u0246HW\u0377\u0177\u0000\u0000p\u0246HW\u0377\u0177\u0000\u0000 \u0250HW\u0377\u0177\u0000\u0000",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277",
                    "|_|r3",
                    "|r3",
                    "|cFF",
                    "|cGGGGGGGG",
                    "|1",
                    "|2",
                    "|3",
                    "|3-",
                    "|3-0",
                    "|3--1",
                    "|3-2",
                    "|3-5555",
                    "|3-9999999999999",
                    "|4",
                    "|HKIA_LINK:KIA_R" + "" + " | h" + "|cffa335ee|Hitem:29434:0:0:0:0:0:0:0:80|h[Знак справедливости]|h|r" + " | h",
                    "|cffFF4E00|Hlevelup:-1:LEVEL_UP_TYPE_CHARACTER|hHey, guess what I just got? its [Fetish of the Bloodthirsty Gladiator] |h|r",
                    "|cffFF4E00|Hlevelup:2:LEVEL_UP_TYPE_CHARACTER|h[Level 2]|h|r",
                    "|cffa335ee|Hitem:29434:0:0:0:0:0:0:0:80|h[Знак справедливости]|h|r",
                    ":||",
                    "||",
                    "|",
                    "|cFFDDD000|Hquest:|htest|h|r",
                    "|cff808080|Hquest:9832:|h[The Second and Third Fragments]|h|r",
                    "|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r",
                    "|cff808080|Hquest:9832:255|h[The Second and Third Fragments]|h|r",
                    "|cff808080|Hquest:9832:256|h[The Second and Third Fragments]|h|r|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r",
                    "|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r |cff808080|Hquest:55555:70|h[The Second and Third Fragments]|h|r",
                    "|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r |cff808080|Hquest:9832:|h[The Second and Third Fragments]|h|r",
                    "|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r|cff808080|Hquest:",
                    "|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r|cff808080|Hquest:|h[The Second and Third Fragments]|h|r",
                    "|cFFDDD000|Hquest:|hhey||r",
                    "|cff808080|Hquest:9832:0|h[The Second and Third Fragments]|h|r",
                    "|3-1 [Errormode]",
                    "|3-4 (keepsm)",
                    "\r",
                    "\n",
                    "\t",
                    "\u0037",
                    "\r\n\t\u0037"
                };

                for (int i = 0; i <= 256; i++)
                    channels.Add(Convert.ToChar(i).ToString());

                int index = -1;

                int actionId = -1;
                actionId = ScheduleAction(() =>
                {
                    byte[] channelBytes;
                    if (index == -1)
                    {
                        var channelName = "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277";

                        channelBytes = channelName.ToCString().Take(255).ToArray();
                        channelBytes[channelBytes.Length - 1] = 0;
                        index++;
                    }
                    else
                    {

                        if (index >= channels.Count)
                        {
                            CancelAction(actionId);
                            return;
                        }
                        channelBytes = channels[index++].ToCString();
                    }

                    var response = new OutPacket(WorldCommand.CMSG_JOIN_CHANNEL);

                    response.Write((uint)0);
                    response.Write((byte)0);
                    response.Write((byte)0);
                    response.Write(channelBytes);
                    response.Write("".ToCString());
                    SendPacket(response);

                    response = new OutPacket(WorldCommand.CMSG_CHANNEL_INVITE);
                    response.Write(channelBytes);
                    response.Write("User".ToCString());
                    SendPacket(response);

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 0, 50));
            }

            if (false)
            {
                var messages = new List<string>()
                {
                    "1 |_|r3",
                    "2 |r3",
                    "3 |cFF",
                    "4 |cGGGGGGGG",
                    "5 |1",
                    "6 |2",
                    "7 |3",
                    "8 |3-",
                    "9 |3-0",
                    "10 |3--1",
                    "11 |3-2",
                    "12 |3-5555",
                    "13 |3-9999999999999",
                    "14 |4",
                    "15 |HKIA_LINK:KIA_R" + "" + " | h" + "|cffa335ee|Hitem:29434:0:0:0:0:0:0:0:80|h[Знак справедливости]|h|r" + " | h",
                    "16 |cffFF4E00|Hlevelup:-1:LEVEL_UP_TYPE_CHARACTER|hHey, guess what I just got? its [Fetish of the Bloodthirsty Gladiator] |h|r",
                    "17 |cffFF4E00|Hlevelup:2:LEVEL_UP_TYPE_CHARACTER|h[Level 2]|h|r",
                    "18 |cffa335ee|Hitem:29434:0:0:0:0:0:0:0:80|h[Знак справедливости]|h|r",
                    "19 :||",
                    "20 ||",
                    "21 |",
                    "22 |cFFDDD000|Hquest:|htest|h|r",
                    "23 |cff808080|Hquest:9832:|h[The Second and Third Fragments]|h|r",
                    "24 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r",
                    "25 |cff808080|Hquest:9832:255|h[The Second and Third Fragments]|h|r",
                    "26 |cff808080|Hquest:9832:256|h[The Second and Third Fragments]|h|r|cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r",
                    "27 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r |cff808080|Hquest:55555:70|h[The Second and Third Fragments]|h|r",
                    "28 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r |cff808080|Hquest:9832:|h[The Second and Third Fragments]|h|r",
                    "29 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r|cff808080|Hquest:",
                    "30 |cff808080|Hquest:9832:70|h[The Second and Third Fragments]|h|r|cff808080|Hquest:|h[The Second and Third Fragments]|h|r",
                    "31 |cFFDDD000|Hquest:|hhey||r",
                    "32 |cff808080|Hquest:9832:0|h[The Second and Third Fragments]|h|r",
                    "33 |3-1 [Errormode]",
                    "34 |3-4 (keepsm)",
                    "35 \u0124cffffff00\u0124Hquest:11318\u0124h[А теперь гонки на баранах... Или вроде того.]\u0124h\u0124r",
                    "36 |cffffff00|Hquest:11318|h[А теперь гонки на баранах... Или вроде того.]|h|r",
                    "Finished"
                };
                int index = 0;

                int actionId = -1;
                actionId = ScheduleAction(() =>
                {
                    if (index >= messages.Count)
                    {
                        CancelAction(actionId);
                        return;
                    }

                    var message = messages[index++];

                    var response = new OutPacket(WorldCommand.CMSG_SEND_MAIL);

                    response.Write((ulong)0);
                    response.Write("User".ToCString());
                    response.Write(message.ToCString());
                    response.Write(message.ToCString());
                    response.Write((uint)0);
                    response.Write((uint)0);
                    response.Write((byte)0);
                    response.Write((uint)0);
                    response.Write((uint)0);
                    response.Write((ulong)0);
                    response.Write((byte)0);


                    response.Write("User".ToCString());
                    response.Write(message.ToCString());
                    SendPacket(response);

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 1, 0));
            }

            if (false)
            {
                ScheduleAction(() =>
                {
                    var message = (
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217"
                    ).ToCString();

                    for (int i = 0; i < 1; i++)
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Whisper);
                        var race = World.SelectedCharacter.Race;
                        var language = race.IsHorde() ? Language.Orcish : Language.Common;
                        response.Write((uint)language);
                        response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 1));
            }

            if (false)
            {
                ScheduleAction(() =>
                {
                    var message = (
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    ).ToCString();

                    message[255] = 0;

                    for (int i = 0; i < 10000; i++)
                    {
                        var response = new OutPacket(WorldCommand.CMSG_MESSAGECHAT);

                        response.Write((uint)ChatMessageType.Whisper);
                        var race = World.SelectedCharacter.Race;
                    //var language = race.IsHorde() ? Language.Orcish : Language.Common;
                    var language = Language.Addon;
                        response.Write((uint)language);
                        response.Write("User".ToCString());
                        response.Write(message);
                        SendPacket(response);
                    }

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 1));
            }

            if (false)
            {
                var playerNames = new List<string>()
                {
                    "test",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0004\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0004\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0377\u0377\u0377\u0377\u0000\u0000\u0000\u0000<\u0330\u0000\u0000\u0301\u0337\u0000\u0000<\u0330\u0000\u0000\u0377\u0337\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0247HW\u0377\u0177"
                    + "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000"
                    + "\u0361\u0000\u0000\u0005\u0002"
                    + "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000"
                    + "\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0340\u0250HW\u0377\u0177\u0000\u0000ЦHW\u0377\u0177\u0000\u0000P\u0250HW\u0377\u0177\u0000\u0000\u0362\u0000\u0000\u0005\u0002"
                    + "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000"
                    + "\u0001\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0020\u0246HW\u0377\u0177\u0000\u0000p\u0246HW\u0377\u0177\u0000\u0000 \u0250HW\u0377\u0177\u0000\u0000",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    +"\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                    + "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217",
                    "\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277\u0360\u0237\u0217\u0201\u0360\u0237\u0217\u0277"
                };

                int index = 0;

                int actionId = -1;
                actionId = ScheduleAction(() =>
                {
                    if (index >= playerNames.Count)
                    {
                        CancelAction(actionId);
                        Log("Finished");
                        return;
                    }

                    var message = playerNames[index++];

                    var packet = new OutPacket(WorldCommand.CMSG_ADD_IGNORE);
                    packet.Write(message.ToCString());
                    SendPacket(packet);

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 1, 0));
            }

            if (true)
            {
                int counter = 0;

                ScheduleAction(() =>
                {
                    var message = BitConverter.GetBytes(counter++);

                    var packet = new OutPacket(WorldCommand.CMSG_ADD_IGNORE);
                    packet.Write(message);
                    SendPacket(packet);

                    if (counter % 100 == 0)
                        Console.WriteLine("Sending character: " + counter);

                }, DateTime.Now.AddSeconds(10), new TimeSpan(0, 0, 0, 0, 10));
            }
        }

        public override void NoCharactersFound()
        {
            CreateCharacter(Race.Human, Class.Priest);
        }

        public override void CharacterCreationFailed(CommandDetail result)
        {
#warning ToDo: create a character with a different name
            Log($"Bot {Username} failed creating a character with error {result.ToString()}", LogLevel.Error);
        }

        public override void InvalidCredentials()
        {
            BotFactory.Instance.RemoveBot(this);
        }

        public WorldObject FindClosestNonBotPlayer(Func<WorldObject, bool> additionalCheck = null)
        {
            return FindClosestObject(HighGuid.Player, obj =>
            {
                if (BotFactory.Instance.IsBot(obj))
                    return false;
                if (additionalCheck != null && !additionalCheck(obj))
                    return false;
                return true;
            });
        }

        #region Handlers
        [PacketHandler(WorldCommand.SMSG_GROUP_INVITE)]
        protected void HandlePartyInvite(InPacket packet)
        {
            if(Behavior.AutoAcceptGroupInvites)
                SendPacket(new OutPacket(WorldCommand.CMSG_GROUP_ACCEPT, 4));
        }

        [PacketHandler(WorldCommand.SMSG_RESURRECT_REQUEST)]
        protected void HandleResurrectRequest(InPacket packet)
        {
            var resurrectorGuid = packet.ReadUInt64();
            OutPacket response = new OutPacket(WorldCommand.CMSG_RESURRECT_RESPONSE);
            response.Write(resurrectorGuid);
            if (Behavior.AutoAcceptResurrectRequests)
            {
                response.Write((byte)1);
                SendPacket(response);

                OutPacket result = new OutPacket(WorldCommand.MSG_MOVE_TELEPORT_ACK);
                result.WritePacketGuid(Player.GUID);
                result.Write((UInt32)0);
                result.Write(DateTime.Now.Millisecond);
                SendPacket(result);
            }
            else
            {
                response.Write((byte)0);
                SendPacket(response);
            }
        }

        [PacketHandler(WorldCommand.SMSG_CORPSE_RECLAIM_DELAY)]
        protected void HandleCorpseReclaimDelay(InPacket packet)
        {
            CorpseReclaim = DateTime.Now.AddMilliseconds(packet.ReadUInt32());
        }

        [PacketHandler(WorldCommand.SMSG_TRADE_STATUS)]
        protected void HandleTradeStatus(InPacket packet)
        {
            if (Behavior.Begger)
            {
                TradeStatus status = (TradeStatus)packet.ReadUInt32();
                switch (status)
                {
                    case TradeStatus.BeginTrade:
                        TraderGUID = packet.ReadUInt64();
                        // Stop moving
                        CancelActionsByFlag(ActionFlag.Movement);
                        // Accept trade
                        OutPacket beginTrade = new OutPacket(WorldCommand.CMSG_BEGIN_TRADE);
                        SendPacket(beginTrade);
                        break;
                    case TradeStatus.Canceled:
                        EnableActionsByFlag(ActionFlag.Movement);
                        TraderGUID = 0;
                        break;
                    case TradeStatus.Accept:
                        OutPacket acceptTrade = new OutPacket(WorldCommand.CMSG_ACCEPT_TRADE);
                        SendPacket(acceptTrade);
                        break;
                    case TradeStatus.Tradecomplete:
                        DoSayChat("Thank you!");
                        EnableActionsByFlag(ActionFlag.Movement);
                        HandleTriggerInput(TriggerActionType.TradeCompleted, TraderGUID);
                        TraderGUID = 0;
                        break;
                }
            }
        }
        #endregion

        #region Actions
        public void MoveTo(Position destination)
        {
            CancelActionsByFlag(ActionFlag.Movement, false);

            if (destination.MapID != Player.MapID)
            {
                Log("Trying to move to another map", Client.UI.LogLevel.Warning);
                HandleTriggerInput(TriggerActionType.DestinationReached, false);
                return;
            }

            Path path = null;
            using(var detour = new DetourCLI.Detour())
            {
                List<MapCLI.Point> resultPath;
                var pathResult = detour.FindPath(Player.X, Player.Y, Player.Z,
                                        destination.X, destination.Y, destination.Z,
                                        Player.MapID, out resultPath);
                if (pathResult != PathType.Complete)
                {
                    Log($"Cannot reach destination, FindPath() returned {pathResult} : {destination.ToString()}", Client.UI.LogLevel.Warning);
                    HandleTriggerInput(TriggerActionType.DestinationReached, false);
                    return;
                }

                path = new Path(resultPath, Player.Speed, Player.MapID);
                var destinationPoint = path.Destination;
                destination.SetPosition(destinationPoint.X, destinationPoint.Y, destinationPoint.Z);
            }

            var remaining = destination - Player.GetPosition();
            // check if we even need to move
            if (remaining.Length < MovementEpsilon)
            {
                HandleTriggerInput(TriggerActionType.DestinationReached, true);
                return;
            }

            var direction = remaining.Direction;

            var facing = new MovementPacket(WorldCommand.MSG_MOVE_SET_FACING)
            {
                GUID = Player.GUID,
                flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                X = Player.X,
                Y = Player.Y,
                Z = Player.Z,
                O = path.CurrentOrientation
            };

            SendPacket(facing);
            Player.SetPosition(facing.GetPosition());

            var startMoving = new MovementPacket(WorldCommand.MSG_MOVE_START_FORWARD)
            {
                GUID = Player.GUID,
                flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                X = Player.X,
                Y = Player.Y,
                Z = Player.Z,
                O = path.CurrentOrientation
            };
            SendPacket(startMoving);

            var previousMovingTime = DateTime.Now;

            var oldRemaining = remaining;
            ScheduleAction(() =>
            {
                Point progressPosition = path.MoveAlongPath((float)(DateTime.Now - previousMovingTime).TotalSeconds);
                Player.SetPosition(progressPosition.X, progressPosition.Y, progressPosition.Z);
                previousMovingTime = DateTime.Now;

                remaining = destination - Player.GetPosition();
                if (remaining.Length > MovementEpsilon)
                {
                    oldRemaining = remaining;

                    var heartbeat = new MovementPacket(WorldCommand.MSG_MOVE_HEARTBEAT)
                    {
                        GUID = Player.GUID,
                        flags = MovementFlags.MOVEMENTFLAG_FORWARD,
                        X = Player.X,
                        Y = Player.Y,
                        Z = Player.Z,
                        O = path.CurrentOrientation
                    };
                    SendPacket(heartbeat);
                }
                else
                {
                    var stopMoving = new MovementPacket(WorldCommand.MSG_MOVE_STOP)
                    {
                        GUID = Player.GUID,
                        X = Player.X,
                        Y = Player.Y,
                        Z = Player.Z,
                        O = path.CurrentOrientation
                    };
                    SendPacket(stopMoving);
                    Player.SetPosition(stopMoving.GetPosition());

                    CancelActionsByFlag(ActionFlag.Movement, false);

                    HandleTriggerInput(TriggerActionType.DestinationReached, true);
                }
            }, new TimeSpan(0, 0, 0, 0, 100), ActionFlag.Movement,
            () =>
            {
                var stopMoving = new MovementPacket(WorldCommand.MSG_MOVE_STOP)
                {
                    GUID = Player.GUID,
                    X = Player.X,
                    Y = Player.Y,
                    Z = Player.Z,
                    O = path.CurrentOrientation
                };
                SendPacket(stopMoving);
            });
        }

        public void Resurrect()
        {
            OutPacket repop = new OutPacket(WorldCommand.CMSG_REPOP_REQUEST);
            repop.Write((byte)0);
            SendPacket(repop);
        }
        #endregion

        #region Logging
        public override void Log(string message, LogLevel level = LogLevel.Info)
        {
            BotFactory.Instance.Log(Username + " - " + message, level);
        }

        public override void LogLine(string message, LogLevel level = LogLevel.Info)
        {
            BotFactory.Instance.Log(Username + " - " + message, level);
        }

        public override void LogException(string message)
        {
            BotFactory.Instance.Log(Username + " - " + message, LogLevel.Error);
        }

        public override void LogException(Exception ex)
        {
            BotFactory.Instance.Log(string.Format(Username + " - {0} {1}", ex.Message, ex.StackTrace), LogLevel.Error);
        }
        #endregion
    }
}
