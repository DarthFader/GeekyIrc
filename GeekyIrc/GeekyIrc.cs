namespace GeekyIrc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Bots.Gatherbuddy;
    using Meebey.SmartIrc4net;
    using Styx;
    using Styx.Common;
    using Styx.CommonBot;
    using Styx.CommonBot.POI;
    using Styx.CommonBot.Profiles;
    using Styx.Plugins;
    using Styx.WoWInternals;
    using Styx.WoWInternals.WoWObjects;

    public class GeekyIrc : HBPlugin
    {
        private IrcClient _irc;
        private Thread _listener;

        private bool Init
        {
            get;
            set;
        }

        public bool Blocking
        {
            get;
            set;
        }

        internal int TotalFriendsOnline
        {
            get { return Lua.GetReturnVal<int>(string.Format("return BNGetNumFriends()"), 1); }
        }

        private int BnetReply
        {
            get;
            set;
        }

        private readonly Styx.Common.Helpers.WaitTimer _waitTimer = Styx.Common.Helpers.WaitTimer.ThirtySeconds;

        private void Listen()
        {
            try
            {
                while (_irc.IsConnected)
                {
                    _irc.Listen();
                }
            }
            catch(Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        private void Write(string text)
        {
            if (ConfigValues.Instance.LogInHbsLog)
                Logging.Write(text);
            
            SendIrc(text);
        }

        public static List<string> ChatList = new List<string>();

        #region Overrides

        /// <summary>
        /// The name of this plugin.
        /// </summary>
        public override string Name
        {
            get { return "GeekyIrc"; }
        }

        public override bool WantButton
        {
            get { return true; }
        }

        public override void OnButtonPress()
        {
            if (!ConfigForm.IsValid)
                new ConfigForm().Show();
            else
                ConfigForm.Instance.Activate();
        }

        /// <summary>
        /// The author of this plugin.
        /// </summary>
        public override string Author
        {
            get { return "geeekzor"; }
        }

        /// <summary>
        /// The version of the plugin.
        /// </summary>
        public override Version Version
        {
            get { return new Version(3, 0, 2, 2); }
        }

        public override void Initialize()
        {
            if (Init) return;
            
            try
            {
                _irc = new IrcClient();
                _irc.OnRawMessage += OnRawMessage;
                //
                Chat.Battleground += ChatSpec;
                Chat.BattlegroundLeader += ChatSpec;
                Chat.Guild += Chat_Guild;
                Chat.Officer += ChatSpec;
                Chat.Raid += ChatSpec;
                Chat.Channel += ChatSpec;
                Chat.Party += ChatSpec;
                Chat.PartyLeader += ChatSpec;
                Chat.Say += ChatSpec;
                Chat.Yell += ChatSpec;
                Chat.Whisper += Chat_Whisper;
                Chat.WhisperTo += ChatSpec;
                //
                Logging.OnLogMessage += Logging_OnLogMessage;
                //
                Lua.Events.AttachEvent("CHAT_MSG_LOOT", LootChatMonitor);
                Lua.Events.AttachEvent("ACHIEVEMENT_EARNED", AchievementMonitor);
                Lua.Events.AttachEvent("CHAT_MSG_BN_WHISPER", BnChatMonitor);
                Lua.Events.AttachEvent("CHAT_MSG_BN_CONVERSATION", BnChatMonitor);
                Lua.Events.AttachEvent("BN_FRIEND_ACCOUNT_ONLINE", BnFriendsOn);
                Lua.Events.AttachEvent("BN_FRIEND_ACCOUNT_OFFLINE", BnFriendsOff);
                Lua.Events.AttachEvent("CHAT_MSG_BN_WHISPER_INFORM", BnInform);
                //
                BotEvents.OnBotStopped += BotEvents_OnBotStopped;
                BotEvents.OnBotStarted += BotEvents_OnBotStarted;
                BotEvents.OnBotChanged += BotEvents_OnBotChanged;

                BotEvents.Player.OnLevelUp += Player_OnLevelUp;
                BotEvents.Player.OnPlayerDied += Player_OnPlayerDied;
                BotEvents.Player.OnMobKilled += Player_OnMobKilled;
                //
                _irc.Connect(ConfigValues.Instance.IrcAddress, ConfigValues.Instance.IrcPort);
                _irc.Login(ConfigValues.Instance.IrcUsername, ConfigValues.Instance.IrcUsername[0]);
                _irc.RfcJoin(ConfigValues.Instance.IrcChannel);
                
                _listener = new Thread(Listen) {IsBackground = true};

                _listener.Start();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);

                Init = false;
                return;
            }
            finally
            {
                

                Logging.Write("GeekyIrc version: {0} started.",Version);
            }

            Init = true;
        }

        public override void Dispose()
        {
            _irc.RfcDie();
            _irc.Disconnect();
            base.Dispose();
        }

        public override void Pulse()
        {
            if (!_waitTimer.IsFinished) return;

            try
            {
                if (!_irc.IsConnected)
                    _irc.Connect(ConfigValues.Instance.IrcAddress, ConfigValues.Instance.IrcPort);

                _irc.Login(ConfigValues.Instance.IrcUsername, ConfigValues.Instance.IrcUsername[0]);
                _irc.RfcJoin(ConfigValues.Instance.IrcChannel);
                
                _listener = new Thread(Listen) {IsBackground = true};

                _listener.Start();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }

            _waitTimer.Reset();
        }

        #endregion

        #region Events

        void Logging_OnLogMessage(System.Collections.ObjectModel.ReadOnlyCollection<Logging.LogMessage> messages)
        {
            try
            {
                if (ConfigValues.Instance.LogItAll)
                {
                    messages.ToList().ForEach(t =>
                    {
                        if (t.Level == Styx.Helpers.GlobalSettings.Instance.LogLevel)
                        {
                            SendIrc(t.Message);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        private void Player_OnMobKilled(BotEvents.Player.MobKilledEventArgs args) 
        { 
            if (!ConfigValues.Instance.NotifyMobKilled)return;
            var msg = string.Format("I killed {0}",args.KilledMob);
            Write(msg);
        }

        private void Player_OnLevelUp(BotEvents.Player.LevelUpEventArgs args)
        {
            if (!ConfigValues.Instance.NotifyLevelUp)return;
            var msg = string.Format("I'm level {0} now",args.NewLevel);
            Write(msg);
        }

        private void Player_OnPlayerDied() 
        { 
            if (!ConfigValues.Instance.NotifyDeath)return;
            Write("I died!"); 
        }

        private void BotEvents_OnBotChanged(BotEvents.BotChangedEventArgs args)
        {
            var msg = string.Format("Changed bot, From {0} to {1}",args.OldBot, args.NewBot);
            Write(msg);
        }

        void BotEvents_OnBotStarted(EventArgs args)
        {
            Write("Bot Started!");
        }

        private void BotEvents_OnBotStopped(EventArgs args)
        {
            SendIrc("Bot Stopped!");
        }

        void Chat_Whisper(Chat.ChatWhisperEventArgs e)
        {
            if (!ConfigValues.Instance.LogWhispers) return;
            var msg = string.Format("[{0}] {1} : {2}", e.EventName.Replace("CHAT_MSG_", ""), e.Author, e.Message);
            Write(msg);
        }

        private void ChatSpec(Chat.ChatLanguageSpecificEventArgs e)
        {
            var msg = string.Format("[{0}] {1} : {2}", e.EventName.Replace("CHAT_MSG_", ""), e.Author, e.Message);
            var channel = e.EventName.Replace("CHAT_MSG_", "").ToLower();

            if (e.Author == StyxWoW.Me.Name && !ConfigValues.Instance.LogOwn) return;

            if (channel == "say" && ConfigValues.Instance.LogSay)
            {
                if (ConfigValues.Instance.NotifySay) OurNameNotification(e.Args);
                Write(msg);
            }

            if (channel == "guild" && ConfigValues.Instance.LogGuild)
            {
                if (ConfigValues.Instance.NotifyGuild) OurNameNotification(e.Args);
                Write(msg);
            }

            if (channel == "officer" && ConfigValues.Instance.LogOfficer)
            {
                if (ConfigValues.Instance.NotifyOfficer) OurNameNotification(e.Args);
                Write(msg);
            }

            if (channel == "raid" && ConfigValues.Instance.LogRaid)
            {
                if (ConfigValues.Instance.NotifyRaid) OurNameNotification(e.Args);
                Write(msg);
            }

            if (channel == "battleground" && ConfigValues.Instance.LogBattleground)
            {
                if (ConfigValues.Instance.NotifyBg) OurNameNotification(e.Args);
                Write(msg);
            }

            if (channel == "party" && ConfigValues.Instance.LogParty)
            {
                if (ConfigValues.Instance.NotifyParty) OurNameNotification(e.Args);
                Write(msg);
            }
           
            if (ConfigValues.Instance.DebugLogging)
                Write(string.Format("{0}", e.Args));
        }

        private void Chat_Guild(Chat.ChatGuildEventArgs e)
        {
            if (!ConfigValues.Instance.LogGuild) return;
            var msg = string.Format("[{0}] {1} : {2}", e.EventName.Replace("CHAT_MSG_", ""), e.Author, e.Message);
            Write(msg);
        }

        /// <summary>
        ///   Send a Irc Message or Notice if the current players name is mentioned in a chat.
        /// </summary>
        /// <param name="args"> Chat Event Args </param>
        public void OurNameNotification(params object[] args)
        {
            if (!args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                return;

            try
            {
                string outMsg = String.Format
                    ("[{2}] [Notification] Our name was mentioned in {0} by {1}",
                     UppercaseFirst(args[0]),
                     UppercaseFirst(args[1]),
                     DateTime.Now.ToShortTimeString());

                if (ConfigValues.Instance.IrcNotice)
                    _irc.RfcNotice(ConfigValues.Instance.IrcChannel, IrcColor.Yellow + outMsg);
                else
                    _irc.SendMessage(SendType.Message, ConfigValues.Instance.IrcChannel, IrcColor.Yellow + outMsg);
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        private void LootChatMonitor(object sender, LuaEventArgs args)
        {
            if (!ConfigValues.Instance.LogLoot)
                return;

            try
            {
                var lootname =
                    Lua.GetReturnVal<string>("return GetItemInfo('" + args.Args[0].ToString().Split(':')[1] + "')", 0);
                var lootquality =
                    Lua.GetReturnVal<int>("return GetItemInfo('" + args.Args[0].ToString().Split(':')[1] + "')", 2);
                if (lootquality >= ConfigValues.Instance.LootFilter &&
                    args.Args[0].ToString().Contains("You receive loot"))
                {
                    SendIrc(string.Format("[Looted] {0}", ItemColor(lootname)));
                }
            }
            catch
            {
                // Catch some wierd lua exceptions
            }
        }

        private void AchievementMonitor(object sender, LuaEventArgs args)
        {
            var list = new List<string>
                       {
                           string.Format
                               ("Earned Achievement \"{0}\"",
                                Lua.GetReturnVal<string>(
                                                         string.Format("return GetAchievementInfo('{0}')", args.Args[0]), 1)),
                           string.Format
                               ("Description: {0}",
                                Lua.GetReturnVal<string>(
                                                         string.Format("return GetAchievementInfo('{0}')", args.Args[0]), 7))
                       };

            if (args.Args[10].ToString().Length > 0)
            {
                list.Add(
                         string.Format
                             ("Reward: {0}",
                              Lua.GetReturnVal<string>(string.Format("return GetAchievementInfo('{0}')", args.Args[0]), 10)));
            }

            list.ForEach(SendIrc);
        }

        #endregion

        #region Irccommands

        public void OnRawMessage
            (object sender,
             IrcEventArgs e)
        {
            bool b = Blocking;
            try
            {
                switch (e.Data.MessageArray[0].StartsWith(ConfigValues.Instance.CommandPrefix))
                {
                    case true:
                        switch (
                            e.Data.MessageArray[0].TrimStart(Convert.ToChar(ConfigValues.Instance.CommandPrefix)).
                                ToLower())
                        {
                            case "afk":
                                Run
                                    (e.Data,
                                     () => Lua.DoString(string.Format("RunMacroText(\"/AFK\")")));
                                break;
                            case "dnd":
                                Run
                                    (e.Data,
                                     () => Lua.DoString(string.Format("RunMacroText(\"/DND\")")));
                                break;
                            case "testunstuck":
                                Run
                                    (e.Data,
                                     () =>
                                     new List<string> {"CloseTaxiMap()", "CloseMerchant()", "CloseGossip()"}.ForEach
                                         (t =>
                                         {
                                             SendIrc
                                                 (string
                                                      .
                                                      Format
                                                      ("Testing: Lua.DoString({0})",
                                                       t));
                                             Lua
                                                 .
                                                 DoString
                                                 (t);
                                         }));
                                break;
                            case "bnetdump":
                                DumpValues(e.Data);
                                break;
                            case "bnetmsg":
                                MessageBNetFriend(e.Data);
                                break;
                            case "bnetreply":
                                ReplyBnFriend(e.Data);
                                break;
                            case "blocking":
                                Request
                                    (e.Data,
                                     b);
                                break;
                            case "chatlog":
                                RequestList
                                    (e.Data,
                                     ChatList,
                                     "chatlog");
                                break;
                            case "send":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         int i;
                                         for (i = 0; i < 2; i++)
                                             _irc.SendMessage
                                                 (SendType.Notice,
                                                  ConfigValues.Instance.IrcChannel,
                                                  string.Format
                                                      ("{0}",
                                                       i));
                                     });
                                break;
                            case "restart":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         BotManager.Instance.SetCurrent(BotManager.Current);
                                         TreeRoot.Start();
                                     });
                                break;
                            //case "testcolor":
                            //    var testcolor = new List<string>
                            //                    {
                            //                        IrcColor.White + "White",
                            //                        IrcColor.Black + "Black",
                            //                        IrcColor.BlueNavy + "BlueNavy",
                            //                        IrcColor.Blue + "Blue",
                            //                        IrcColor.Red + "Red",
                            //                        IrcColor.Orange + "Orange",
                            //                        IrcColor.Yellow + "Yellow",
                            //                        IrcColor.Green + "Green",
                            //                        IrcColor.Teal + "Teal",
                            //                        IrcColor.Aqua + "Aqua",
                            //                        IrcColor.DarkBlue + "DarkBlue",
                            //                        IrcColor.Pink + "Pink",
                            //                        IrcColor.Grey + "Grey",
                            //                        IrcColor.Purple + "Purple",
                            //                        IrcColor.DarkGrey + "DarkGrey",
                            //                        IrcColor.LightGrey + "LightGrey",
                            //                        String.Format
                            //                            ("{0}R{1}a{2}i{3}n{4}b{5}o{6}w!",
                            //                             IrcColor.Red,
                            //                             IrcColor.Orange,
                            //                             IrcColor.Yellow,
                            //                             IrcColor.Green,
                            //                             IrcColor.Blue,
                            //                             IrcColor.Purple,
                            //                             IrcColor.Pink)
                            //                    };
                            //    RequestList
                            //        (e.Data,
                            //         testcolor);
                            //    break;

                            case "wowkill":
                                if (ConfigValues.Instance.AllowProcessKill)
                                {
                                    Run
                                        (e.Data,
                                         () =>
                                         {
                                             Request
                                                 (e.Data,
                                                  String.Format("Killing process, bye!"));
                                             StyxWoW.Memory.Process.Kill();
                                             //ObjectManager.WoWProcess.Kill();
                                         });
                                }
                                break;
                                //Check if nick/username is valid.
                            case "valid":
                                _irc.SendReply
                                    (e.Data,
                                     IsValid(e.Data)
                                         ? String.Format
                                               ("{0} you seem to be valid.",
                                                e.Data.Nick)
                                         : String.Format
                                               ("{0} you don't seem to be valid.",
                                                e.Data.Nick));
                                break;
                            case "time":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("Its {0}!",
                                          DateTime.Now.ToShortTimeString()));
                                break;
                            case "level":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("I'm level {0}",
                                          StyxWoW.Me.Level));
                                break;
                            case "pos":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("I'm at {0} in {1}",
                                          Lua.GetReturnVal<string>
                                              ("return GetMinimapZoneText()",
                                               0),
                                          Lua.GetReturnVal<string>
                                              ("return GetZoneText()",
                                               0)));

                                break;
                            case "deaths":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("I've died {0} times!",
                                          GameStats.Deaths));
                                break;
                            case "exp":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("Currently {0} XP/per hour, Time to level is {1}",
                                          GameStats.XPPerHour,
                                          String.Format
                                              ("{0:hh\\:mm\\:ss}",
                                               GameStats.TimeToLevel)));
                                break;
                            case "honor":

                                Request
                                    (e.Data,
                                     String.Format
                                         ("Honor gained : {0}, Honor/per hour : {1}",
                                          GameStats.HonorGained,
                                          GameStats.HonorPerHour));

                                break;
                                //Stop the bot
                            case "stop":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         TreeRoot.Stop();
                                         //Request(e.Data, "Stopping bot");
                                         GC.Collect();
                                     });
                                break;
                                //Start the bot
                            case "start":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         TreeRoot.Start();
                                         //Request(e.Data, "Starting bot");
                                         GC.Collect();
                                     });
                                break;
                                //Switch to BGBuddy
                            case "bg":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         BotManager.Instance.SetCurrent(BotManager.Instance.Bots["BGBuddy"]);
                                         TreeRoot.Start();
                                         Request
                                             (e.Data,
                                              GColor
                                                  (IrcColor.Red,
                                                   "Starting BGBuddy"));
                                     });
                                break;
                                //Switch to grind bot
                            case "grind":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         if (ConfigValues.Instance.ChangeProfiles)
                                             ProfileManager.LoadNew(ConfigValues.Instance.GrindProfile);
                                         TreeRoot.Stop();
                                         BotManager.Instance.SetCurrent(BotManager.Instance.Bots["Grind Bot"]);
                                         TreeRoot.Start();
                                         Request
                                             (e.Data,
                                              "Starting Grind Bot");
                                     });
                                break;
                                //Switch to Archaeology bot
                            case "arch":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         BotManager.Instance.SetCurrent(BotManager.Instance.Bots["ArchaeologyBuddy"]);
                                         TreeRoot.Start();
                                         Request
                                             (e.Data,
                                              "Starting ArchaeologyBot");
                                     });
                                break;
                                //Switch to Questing Bot
                            case "quest":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         if (ConfigValues.Instance.ChangeProfiles)
                                             ProfileManager.LoadNew(ConfigValues.Instance.QuestProfile);
                                         BotManager.Instance.SetCurrent(BotManager.Instance.Bots["Questing"]);
                                         TreeRoot.Start();
                                         Request
                                             (e.Data,
                                              "Starting Questing bot");
                                     });
                                break;
                                //Switch to Gatherbuddy2
                            case "gather":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         if (ConfigValues.Instance.ChangeProfiles)
                                             ProfileManager.LoadNew(ConfigValues.Instance.Gatherbuddy2Profile);
                                         BotManager.Instance.SetCurrent(BotManager.Instance.Bots["Gatherbuddy2"]);
                                         TreeRoot.Start();
                                         Request
                                             (e.Data,
                                              "Starting Gatherbuddy2");
                                     });

                                break;
                                //Send the characters Name
                            case "name":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("My name is {0}",
                                          StyxWoW.Me.Name));
                                break;
                                //Send how many times we've looted.
                            case "loots":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("Loots, Total: {0} Loots/hr: {1}",
                                          GameStats.Loots,
                                          GameStats.LootsPerHour));
                                break;
                                //Send how many times we've killed.
                            case "kills":
                                Request
                                    (e.Data,
                                     String.Format
                                         ("Kills: {0}",
                                          GameStats.MobsKilled));
                                break;
                                //Send Items List
                            case "item":
                                Run
                                    (e.Data,
                                     () => GetInventoryItems(e.Data));
                                break;
                            case "items":
                                Run
                                    (e.Data,
                                     () => SendInventoryItemList(e.Data));
                                break;
                                //Send a status list.
                            case "status":
                                Run
                                    (e.Data,
                                     () => BotStatus(e.Data));
                                break;
                            case "update":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         BotPoi.Clear("Update POI");
                                         _irc.SendReply
                                             (e.Data,
                                              GColor
                                                  (IrcColor.Green,
                                                   "Cleared BotPoi!"));
                                     });
                                break;
                            case "chat":
                                Run
                                    (e.Data,
                                     () => SendChat(e.Data));
                                break;
                            case "whisper":
                                Run
                                    (e.Data,
                                     () => SendWhisper(e.Data));
                                break;
                            case "reply":
                                Run
                                    (e.Data,
                                     () => ReplyToWhisper(e.Data));
                                break;
                            case "poi":
                                _irc.SendReply
                                    (e.Data,
                                     String.Format
                                         ("[BotPoi] : Type : {0} Name : {1}",
                                          BotPoi.Current.Type,
                                          BotPoi.Current.Name));
                                break;
                            case "opme":
                                Run
                                    (e.Data,
                                     () => OpMe(e.Data));
                                break;
                            case "gc":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         Request
                                             (e.Data,
                                              "Garbage Collector called!");
                                         GC.Collect();
                                     });
                                break;
                            case "die":
                                Run
                                    (e.Data,
                                     () =>
                                     {
                                         Request
                                             (e.Data,
                                              "Exit command");
                                         _irc.RfcDie();
                                         _irc.Disconnect();
                                     });
                                break;
                        }
                        break;
                }
            }

            catch
            {
                // Catch
            }
        }

        #endregion

        #region BattleNet

        internal string GetFriendInfo(int index)
        {
            var info = Lua.GetReturnValues(string.Format("return BNGetFriendInfo({0})", index));
            return string.Format("{0} Pid : {1}", info[3], info[0]);
        }

        internal string GetFriendInfoByPid(int presenceId)
        {
            var info = Lua.GetReturnValues(string.Format("return BNGetFriendInfoByID({0})", presenceId));
            return string.Format("{0}", info[3]);
        }

        internal void MessageBNetFriend(IrcMessageData data)
        {
            if (!IsValid(data))
                RejectUser(data);
            else
            {
                var wList = new List<string>();
                for (int i = 2; i < data.MessageArray.Length; i++)
                    wList.Add(data.MessageArray[i]);

                string wS = wList.Aggregate("", (current, s) => current + (" " + s));
                string msg = wS.TrimStart(' ');

                int index = int.Parse(data.MessageArray[1]);

                BnetReply = index;

                Lua.DoString(string.Format("BNSendWhisper('{0}', '{1}')", index, Lua.Escape(msg)));
            }
        }

        internal void ReplyBnFriend(IrcMessageData data)
        {
            if (!IsValid(data))
                RejectUser(data);
            else
            {
                var wList = new List<string>();
                for (int i = 1; i < data.MessageArray.Length; i++)
                    wList.Add(data.MessageArray[i]);

                string wS = wList.Aggregate("", (current, s) => current + (" " + s));
                string msg = wS.TrimStart(' ');

                if (BnetReply != 0)
                    Lua.DoString(string.Format("BNSendWhisper('{0}', '{1}')", BnetReply, Lua.Escape(msg)));
                else
                    Write("Whisper someone first.");
            }
        }

        internal int PresenceId(int index)
        {
            return Lua.GetReturnVal<int>((string.Format("return BNGetFriendInfo({0})", index)), 1);
        }



        private void BnChatMonitor(object sender, LuaEventArgs args)
        {
            if (!ConfigValues.Instance.LogBattleNet)
                return;

            var outMsg = string.Format("[BNet Whisper] from {0} (PID:{2}) msg : {1}",
                                       GetFriendInfoByPid(Convert.ToInt16(args.Args[12])), args.Args[0], args.Args[12]);


            Write(outMsg);
        }

        private void BnInform(object sender, LuaEventArgs args)
        {
            if (!ConfigValues.Instance.LogBattleNet)
                return;

            var outMsg = string.Format("[BNet Whisper] to {0} (PID:{2}) msg : {1}",
                                       GetFriendInfoByPid(Convert.ToInt16(args.Args[12])), args.Args[0], args.Args[12]);

            Write(outMsg);
        }

        private void BnFriendsOff(object sender, LuaEventArgs args)
        {
            var outMsg = (string.Format("{0} logged off", GetFriendInfoByPid(Convert.ToInt16(args.Args[0]))));
            Write(outMsg);
        }

        private void BnFriendsOn(object sender, LuaEventArgs args)
        {
            var outMsg = (string.Format("{0} logged on", GetFriendInfoByPid(Convert.ToInt16(args.Args[0]))));
            Write(outMsg);
        }

        #endregion

        #region ItemQuality enum

        /// <summary>
        ///   ItemQuality
        /// </summary>
        public enum ItemQuality
        {
            Grey = 0, // 0. Poor      (gray)  : Broken I.W.I.N. Button
            White = 1, // 1. Common    (white) : Archmage Vargoth's Staff
            Green = 2, // 2. Uncommon  (green) : X-52 Rocket Helmet
            Blue = 3, // 3. Rare      (blue)  : Onyxia Scale Cloak
            Epic = 4, // 4. Epic      (purple): Talisman of Ephemeral Power
            Legendary = 5, // 5. Legendary (orange): Fragment of Val'anyr
            Artifact = 6, // 6. Artifact  (golden): The Twin Blades of Azzinoth
            Heirloom = 7, // 7. Heirloom  (light) : Bloodied Arcanite Reaper
        }

        #endregion

        #region Misc

        internal void DumpValues(IrcMessageData data)
        {
            if (!IsValid(data))
                RejectUser(data);
            else
            {
                for (int i = 1; i <= TotalFriendsOnline; i++)
                {
                    var msg = string.Format("{0}", GetFriendInfo(i));
                    SendIrc(msg);
                }
            }
        }

        public void BotStatus(IrcMessageData data)
        {
            _irc.SendMessage
                (SendType.Message,
                 ConfigValues.Instance.IrcChannel,
                 "Sometimes the list takes a while to load, be patient.");
            var list = new List<string>();

            try
            {
                list.Add(String.Format("Class: {0}", StyxWoW.Me.Class));
                list.Add(String.Format("We are running {0}!", BotManager.Current.Name));
                list.Add
                    (String.Format
                         ("I'm at {0} in {1}",
                          Lua.GetReturnVal<string>("return GetMinimapZoneText()", 0),
                          Lua.GetReturnVal<string>("return GetZoneText()", 0)));

                if (!string.IsNullOrEmpty(TreeRoot.StatusText))
                    list.Add(TreeRoot.StatusText);
                if (!string.IsNullOrEmpty(TreeRoot.GoalText))
                    list.Add(TreeRoot.GoalText);

                if (StyxWoW.Me.Level < 85)
                {
                    list.Add(String.Format("EXP/h: {0}", GameStats.XPPerHour));
                    list.Add(String.Format("Time to Level: {0}", ExtendedTimeFormat(GameStats.TimeToLevel)));
                }

                if (BotManager.Current.Name == BotManager.Instance.Bots["Grind Bot"].Name)
                {
                    list.Add(String.Format("Loots: {0}, Per/hr: {1}", GameStats.Loots, GameStats.LootsPerHour));
                    list.Add(String.Format("Kills: {0}", GameStats.MobsKilled));
                    list.Add(String.Format("Kills per hour: {0}", GameStats.MobsPerHour));
                    list.Add(String.Format("Deaths: {0}", GameStats.Deaths));
                }
                else if (BotManager.Current.Name == BotManager.Instance.Bots["Questing"].Name)
                {
                    list.Add(String.Format("Loots: {0}, Per/hr: {1}", GameStats.Loots, GameStats.LootsPerHour));
                    list.Add(String.Format("Kills: {0}", GameStats.MobsKilled));
                    list.Add(String.Format("Deaths: {0}", GameStats.Deaths));
                    list.Add(String.Format("Poi {0}", BotPoi.Current));
                }
                else if (BotManager.Current.Name == BotManager.Instance.Bots["BGBuddy"].Name)
                {
                    list.Add
                        (String.Format
                             ("BGs: {0} (Won:{1}, Lost:{2})",
                              GameStats.BGsCompleted,
                              GameStats.BGsWon,
                              GameStats.BGsLost));
                    list.Add
                        (string.Format
                             ("BGs/hour: {0} Lost/hr:{1} Won/hr:{2}",
                              GameStats.BGsPerHour,
                              GameStats.BGsLostPerHour,
                              GameStats.BGsWonPerHour));
                    list.Add
                        (String.Format
                             ("Honor Gained: {0}, Honor/hour : {1}", GameStats.HonorGained, GameStats.HonorPerHour));
                }
                else if (BotManager.Current.Name == BotManager.Instance.Bots["Gatherbuddy2"].Name)
                {
                    int i = GatherbuddyBot.NodeCollectionCount.Values.Sum(value => value);

                    try
                    {
                        list.Add(TimeFormat(GatherbuddyBot.runningTime));
                        list.Add(String.Format("Nodes per hour {0}", PerHour(GatherbuddyBot.runningTime, i)));
                        list.Add(String.Format("Total Nodes: {0}", i));
                        list.AddRange
                            (GatherbuddyBot.NodeCollectionCount.Select
                                 (value => String.Format("{0} : {1}", value.Key, value.Value)));
                    }
                    catch (Exception e)
                    {
                        Logging.WriteException(e);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
            finally
            {
                RequestList(data, list);
            }
        }

        /// <summary>
        ///   Handle Commands
        /// </summary>
        /// <param name="data"> IrcMessageData </param>
        /// <param name="message"> Message </param>
        public void Request(IrcMessageData data, object message)
        {
            // TODO
            try
            {
                if (!IsValid(data))
                    RejectUser(data);
                else
                {
                    string msg = String.Format
                        ("{1} used {0} in {2} Replying : {3}", data.Message, data.Nick, data.Channel, message);
                    _irc.SendReply(data, message.ToString());

                    Write(msg);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        /// <summary>
        ///   Handle List Requests
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="list"> </param>
        public void RequestList(IrcMessageData data, List<string> list)
        {
            RequestList(data, list, "status");
        }

        /// <summary>
        ///   Handle List Requests
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="list"> </param>
        /// <param name="message"> </param>
        public void RequestList(IrcMessageData data, List<string> list, string message)
        {
            try
            {
                if (!IsValid(data))
                    RejectUser(data);
                else
                {
                    var msg = string.Format(
                                            "{1} used {0} in {2} Replying {3} list",
                                            data.Message.ToUpper(),
                                            data.Nick,
                                            data.Channel,
                                            message);

                    Write(msg);


                    list.ForEach(t => Run(data, () =>
                    {
                        int num = list.IndexOf(t) + 1;
                        var item = (string.Format("{0}/{1} : {2}",
                                                  num.ToString("00"),
                                                  list.Count, t));

                        Write(item);
                    }));
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        /// <summary>
        ///   Send IrcMessages, channel/privmsg depending on settings.
        /// </summary>
        /// <param name="message"> </param>
        public void SendIrc(string message) {
            _irc.SendMessage
                (SendType.Message,
                 ConfigValues.Instance.LogAllInPrivateMessages
                     ? ConfigValues.Instance.LogAllInPrivateMessagesNick : ConfigValues.Instance.IrcChannel, message);
        }


        /// <summary>
        ///   Return items based on overloaded command
        /// </summary>
        /// <param name="data"> </param>
        public void GetInventoryItems(IrcMessageData data)
        {
            switch (data.MessageArray[1].Trim(' '))
            {
                case "all":
                    SendInventoryItemList(data);
                    break;
                case "gray":
                    SendInventoryQualityItemList(data, (int)ItemQuality.Grey);
                    break;
                case "grey":
                    SendInventoryQualityItemList(data, (int)ItemQuality.Grey);
                    break;
                case "white":
                    SendInventoryQualityItemList(data, (int)ItemQuality.White);
                    break;
                case "green":
                    SendInventoryQualityItemList(data, (int)ItemQuality.Green);
                    break;
                case "blue":
                    SendInventoryQualityItemList(data, (int)ItemQuality.Blue);
                    break;
                case "epic":
                    SendInventoryQualityItemList(data, (int)ItemQuality.Epic);
                    break;
                case "ore":
                    SendInventoryItemList(data, "ore");
                    break;
                case "leather":
                    SendInventoryItemList(data, "leather");
                    break;
                case "cloth":
                    SendInventoryItemList(data, "cloth");
                    break;
                default:
                    _irc.SendReply
                        (data,
                         IrcColor.Red + String.Format("Incorrect, {0}items <type>", ConfigValues.Instance.CommandPrefix));
                    _irc.SendReply
                        (data,
                         IrcColor.Teal +
                         String.Format
                             ("For example, \"{0}items green\" will list all green items.",
                              ConfigValues.Instance.CommandPrefix));
                    _irc.SendReply(data, IrcColor.Teal + String.Format("Available overloads"));
                    _irc.SendReply(data, IrcColor.Teal + String.Format("Green, Grey/Gray,"));
                    _irc.SendReply(data, IrcColor.Teal + String.Format("White, Blue, Epic"));
                    _irc.SendReply(data, IrcColor.Teal + String.Format("Ore, leather and cloth."));
                    break;
            }
        }

        /// <summary>
        ///   Sends all the Items in the bag.
        /// </summary>
        /// <param name="data"> </param>
        public void SendInventoryItemList(IrcMessageData data)
        {
            var itemList = new List<string>();
            foreach (WoWItem bagItem in StyxWoW.Me.BagItems)
            {
                if (!itemList.Contains(bagItem.Name)) itemList.Add(bagItem.Name);
            }

            try
            {
                itemList.ForEach(t =>
                {
                    int ind = itemList.IndexOf(t) + 1;
                    var outmsg =
                        Lua.GetReturnVal<int>(String.Format("return GetItemCount(\"{0}\")", t), 0);
                    _irc.SendReply
                        (data,
                         String.Format
                             ("Item: {0} {1}{2}",
                              string.Format("{0}/{1}", ind.ToString("00"), itemList.Count),
                              ItemColor(t),
                              outmsg > 1 ? String.Format(" (Count: {0})", outmsg) : ""));
                });
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///   Gets the quality of the item.
        /// </summary>
        /// <param name="id"> ItemID </param>
        /// <returns> ItemQuality </returns>
        public int GetItemQuality(uint id)
        {
            return Lua.GetReturnVal<int>(String.Format("return GetItemInfo(\"{0}\")", id), 2);
        }

        /// <summary>
        ///   Send a list of items based on the quality.
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="quality"> </param>
        public void SendInventoryQualityItemList(IrcMessageData data, int quality)
        {
            var itemList = new List<string>();
            foreach (WoWItem bagItem in StyxWoW.Me.BagItems)
            {
                if (!itemList.Contains(bagItem.Name) && GetItemQuality(bagItem.ItemInfo.Id) == quality) itemList.Add(bagItem.Name);
            }
            try
            {
                itemList.ForEach(t =>
                {
                    int ind = itemList.IndexOf(t) + 1;
                    var outmsg =
                        Lua.GetReturnVal<int>(String.Format("return GetItemCount(\"{0}\")", t), 0);
                    _irc.SendReply
                        (data,
                         String.Format
                             ("Item: {0} {1}{2}",
                              string.Format("{0}/{1}", ind.ToString("00"), itemList.Count),
                              ItemColor(t),
                              outmsg > 1 ? String.Format(" (Count: {0})", outmsg) : ""));
                });
            }
            catch (Exception)
            {
            }
            _irc.SendReply
                (data, "I'm sorry if I didn't find what you were looking for, blame Geeekzor for being a shitty coder");
        }

        /// <summary>
        ///   Send a list of items based on types.
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="type"> Item Type </param>
        public void SendInventoryItemList(IrcMessageData data, string type)
        {
            var itemList = new List<string>();
            foreach (WoWItem bagItem in StyxWoW.Me.BagItems)
            {
                if (!itemList.Contains(bagItem.Name) && bagItem.Name.ToLower().Contains(type.ToLower())) itemList.Add(bagItem.Name);
            }

            _irc.SendReply
                (data, String.Format("Yes master, here is a list with items containing {0}", UppercaseFirst(type)));


            try
            {
                itemList.ForEach(t =>
                {
                    int ind = itemList.IndexOf(t) + 1;
                    var outmsg =
                        Lua.GetReturnVal<int>(String.Format("return GetItemCount('{0}')", t), 0);
                    _irc.SendReply
                        (data,
                         String.Format
                             ("Item: {0} {1}{2}",
                              string.Format("{0}/{1}", ind.ToString("00"), itemList.Count),
                              ItemColor(t),
                              outmsg > 1 ? String.Format(" (Count: {0})", outmsg) : ""));
                });
            }
            catch (Exception)
            {
            }

            _irc.SendReply
                (data, "I'm sorry if I didn't find what you were looking for, blame Geeekzor for being a shitty coder");
        }

        /// <summary>
        ///   If a user isn't valid we will announce the person and the command he tried to use.
        /// </summary>
        /// <param name="data"> </param>
        public void RejectUser(IrcMessageData data)
        {
            try
            {
                string msg = String.Format("Invalid user {0} tried {1}!", data.Nick, data.Message);
                Write(msg);

                _irc.SendReply(data, msg);
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        /// <summary>
        ///   Run if a person is allowed.
        /// </summary>
        /// <param name="data"> Data </param>
        /// <param name="rAction"> Method </param>
        public void Run(IrcMessageData data, Action rAction)
        {
            if (IsValid(data))
                rAction.Invoke();
            else
                RejectUser(data);
        }

        /// <summary>
        ///   Check if a person is allowed to use commands.
        /// </summary>
        /// <param name="data"> Data </param>
        /// <returns> True if valid </returns>
        public bool IsValid(IrcMessageData data)
        {
            return !ConfigValues.Instance.UseNickSecurity ||
                   (ConfigValues.Instance.UseNickSecurity &&
                    ConfigValues.Instance.ListenToSpecificNick.Any(nick => data.Nick == nick));
        }

        public void SendWhisper(IrcMessageData data)
        {
            try
            {
                var wList = new List<string>();
                for (int i = 2; i < data.MessageArray.Length; i++)
                    wList.Add(data.MessageArray[i]);
                string wS = wList.Aggregate("", (current, s) => current + (" " + s));

                string msg = wS.TrimStart(' ');
                string target = data.MessageArray[1];
                string outMsg = String.Format("[Whisper] Sending {0} to {1}", msg, target);



                ReplyTarget = target;

                if (ConfigValues.Instance.UseNickSecurity &&
                    !ConfigValues.Instance.ListenToSpecificNick.Contains(data.Nick))
                    RejectUser(data);
                else
                {
                    if (ConfigValues.Instance.DebugLogging)
                    {
                        string dbg = String.Format("[Whisper] Sending {0} to {1}", Lua.Escape(msg), StyxWoW.Me.Name);
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", msg, StyxWoW.Me.Name);
                        _irc.SendReply(data, dbg);
                    }
                    else
                    {
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", UppercaseFirst(Lua.Escape(msg)),
                                     target);
                        _irc.SendReply(data, IrcColor.Pink + outMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        protected string ReplyTarget
        {
            get;
            set;
        }

        public void SendChat(IrcMessageData data)
        {
            try
            {
                var oList = new List<string>();
                for (int i = 2; i < data.MessageArray.Length; i++)
                    if (data.MessageArray.Length > i)
                        oList.Add(data.MessageArray[i]);

                string oS = oList.Aggregate("", (current, s) => current + (" " + s));
                string target = data.MessageArray[1];
                string msg = oS.TrimStart(' ');

                string outMsg = String.Format
                    ("[{1}] Sending message : {0}", UppercaseFirst(msg), UppercaseFirst(target));

                if (!ConfigValues.Instance.LogItAll && !ConfigValues.Instance.LogOwn)
                    _irc.SendReply(data, IrcColor.Blue + outMsg);

                else
                {
                    if (target.ToUpper() == "PARTY" && !StyxWoW.Me.GroupInfo.IsInParty)
                    {
                        _irc.SendReply(data, IrcColor.Blue + "[PARTY] Not in a party!");
                        return;
                    }
                    if (target.ToUpper() == "RAID" && !StyxWoW.Me.GroupInfo.IsInRaid)
                    {
                        _irc.SendReply(data, IrcColor.Blue + "[PARTY] Not in a raid!");
                        return;
                    }

                    Lua.DoString("SendChatMessage('{0}', '{1}', nil, '{1}')", UppercaseFirst(Lua.Escape(msg)),
                                 target.ToUpper());
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        private void ReplyToWhisper(IrcMessageData data)
        {
            try
            {
                if (!IsValid(data))
                    RejectUser(data);
                else
                {
                    var rList = new List<string>();
                    for (int i = 1; i < data.MessageArray.Length; i++)
                        rList.Add(data.MessageArray[i] + " ");
                    string rS = rList.Aggregate("", (current, s) => current + (" " + s));
                    string message = rS.TrimStart(' ').Replace("\"", "\\\"").Replace("'", "\\'");

                    if (ReplyTarget == null)
                    {
                        _irc.SendReply(data, String.Format("[Reply] is null, whisper someone first."));
                        return;
                    }

                    string outMsg = String.Format("[Whisper] Sending {0} to {1}", message, ReplyTarget);

                    if (ConfigValues.Instance.DebugLogging)
                    {
                        string dbg = String.Format("[Whisper] Sending {0} to {1}", message, StyxWoW.Me.Name);
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", message, StyxWoW.Me.Name);
                        _irc.SendReply(data, IrcColor.Pink + dbg);
                    }
                    else
                    {
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", message, ReplyTarget);
                        _irc.SendReply(data, IrcColor.Pink + outMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }


        public void OpMe(IrcMessageData data)
        {
            foreach (var meNick in ConfigValues.Instance.IrcUsername)
            {
                ChannelUser meUsr = _irc.GetChannelUser(ConfigValues.Instance.IrcChannel, meNick);

                if (!meUsr.IsOp)
                    _irc.SendReply(data, IrcColor.Red + "I'm not a channel op, sorry!");
                else
                {
                    if (_irc.GetChannelUser(data.Channel, data.Nick).IsOp)
                        _irc.SendReply(data, IrcColor.Red + "You're already a channel operator!");
                    else
                    {
                        _irc.RfcPrivmsg(data.Nick, "+o on you!");
                        _irc.Op(data.Channel, data.Nick);
                    }
                }
            }
        }

        /// <summary>
        ///   Colors an outgoing message with the specified color.
        /// </summary>
        /// <param name="color"> The color. </param>
        /// <param name="text"> The text you want colored. </param>
        /// <returns> Colored IrcMessage </returns>
        public static string GColor(string color, string text)
        {
            return color + text;
        }

        #endregion

        #region Thanks to Timglide

        /// <summary>
        ///   Timglide's Regex
        /// </summary>
        public static readonly Regex ChatColorRegex = new Regex("\\|c[A-Za-z0-9]{6,8}"),
                                     ChatLinkRegex = new Regex("\\|H.*?\\|h");

        /// <summary>
        ///   Timglide's Regex, remove that pesky formatting!
        /// </summary>
        /// <param name="str"> </param>
        /// <returns> </returns>
        public static string RemoveChatFormatting(string str)
        {
            str = ChatColorRegex.Replace(str, "");
            str = ChatLinkRegex.Replace(str, "");
            str = str.Replace("|h", "");
            str = str.Replace("|r", "");
            return str;
        }

        #endregion

        #region Format

        public static string PerHour(TimeSpan time, int item)
        {
            return string.Format("{0}", Round(item / GatherbuddyBot.runningTime.TotalSeconds * 3600));
        }

        /// <summary>
        ///   Round the input
        /// </summary>
        /// <param name="d"> Double type to round </param>
        /// <returns> Rounded value with 1 decimal </returns>
        public static double Round(double d)
        {
            return Math.Round(d, 1);
        }

        /// <summary>
        ///   Round the input
        /// </summary>
        /// <param name="i"> Int type to round </param>
        /// <returns> Rounded value with 1 decimal </returns>
        public static int Round(int i)
        {
            return (int)Math.Round((double)i, 1);
        }

        /// <summary>
        ///   Round the input
        /// </summary>
        /// <param name="d"> Decimal type to round </param>
        /// <returns> Rounded value with 1 decimal </returns>
        public static decimal Round(decimal d)
        {
            return Math.Round(d, 1);
        }

        /// <summary>
        ///   Formats a TimeSpan value.
        /// </summary>
        /// <param name="time"> TimeSpan </param>
        /// <returns> "Been running for HH:MM" </returns>
        public static string TimeFormat(TimeSpan time)
        {
            return string.Format("{0:D2}:{1:D2}", time.Hours, time.Minutes);
        }

        public static string TimeFormat(string name, TimeSpan time)
        {
            return string.Format(name + "{0:D2}:{1:D2}", time.Hours, time.Minutes);
        }

        public static string ExtendedTimeFormat(TimeSpan time)
        {
            string hours = String.Format("{0:D0} hour{1}", time.Hours, (time.Hours > 1 ? "s" : ""));
            string minutes = String.Format("{0:D0} minute{1}", time.Minutes, (time.Minutes > 1 ? "s" : ""));
            string seconds = String.Format("{0:D0} second{1}", time.Seconds, (time.Seconds > 1 ? "s" : ""));
            if (time.Hours > 0 && time.Minutes > 0 && time.Seconds > 0)
                return String.Format("{0}, {1} and {2}", time.Hours > 0 ? hours : "", time.Minutes > 0 ? minutes : "",
                                     time.Seconds > 0 ? seconds : "");
            if (time.Hours > 0 && time.Minutes > 0 && time.Seconds < 1)
                return String.Format("{0} and {1}", time.Hours > 0 ? hours : "", time.Minutes > 0 ? minutes : "");
            if (time.Hours > 0 && time.Minutes < 1 && time.Seconds < 1)
                return String.Format("{0}", time.Hours > 0 ? hours : "");
            if (time.Hours < 1 && time.Minutes > 0 && time.Seconds > 0)
                return String.Format("{0} and {1}", time.Minutes > 0 ? minutes : "", time.Seconds > 0 ? seconds : "");
            if (time.Hours > 0 && time.Minutes < 1 && time.Seconds > 1)
                return String.Format("{0} and {1}", time.Hours > 0 ? hours : "", time.Seconds > 0 ? seconds : "");
            if (time.Hours < 1 && time.Minutes > 0 && time.Seconds < 1)
                return String.Format("{0}", time.Minutes > 0 ? minutes : "");
            if (time.Hours < 1 && time.Minutes < 1 && time.Seconds < 1) return String.Format("Empty");
            return String.Format("{0}", time.Seconds > 0 ? seconds : "");
        }

        /// <summary>
        ///   Color an item based on the item quality.
        /// </summary>
        /// <param name="item"> </param>
        /// <returns> Colored item </returns>
        public static string ItemColor(string item)
        {
            string cleanString = item.Replace("\\", "").Replace("[", "").Replace("]", "").Trim();
            var quality = Lua.GetReturnVal<int>(string.Format("return GetItemInfo(\"{0}\")", cleanString), 2);

            switch (quality)
            {
                //Grey
                case 0:
                    return GColor(IrcColor.LightGrey, item);

                //White
                case 1:
                    return GColor(IrcColor.White, item);

                //Greens
                case 2:
                    return GColor(IrcColor.Green, item);

                //Bluez
                case 3:
                    return GColor(IrcColor.Blue, item);

                //Epix
                case 4:
                    return GColor(IrcColor.Purple, item);

                //Legen... Wait for it... Dary!
                case 5:
                    return GColor(IrcColor.Orange, item);

                // Adding this because we can.
                case 6:
                    return GColor(IrcColor.LightGrey, item);

                // Adding this because we can.
                case 7:
                    return GColor(IrcColor.LightGrey, item);
                default:
                    return item;
            }
        }

        /// <summary>
        ///   Convert the first character in the string to uppercase.
        /// </summary>
        /// <param name="s"> string </param>
        /// <returns> String with first char as uppercase </returns>
        public static string UppercaseFirst(string s)
        {
            if (ConfigValues.Instance.UseUppercaseFormat)
                return String.IsNullOrEmpty(s) ? String.Empty : Char.ToUpper(s[0]) + s.Substring(1);
            return s;
        }

        public static string Ordinal(int number)
        {
            string suffix;
            int ones = number % 10;
            int tens = (int)Math.Floor(number / 10M) % 10;
            if (tens == 1)
                suffix = "th";
            else
            {
                switch (ones)
                {
                    case 0:
                        suffix = "";
                        break;
                    case 1:
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;
                    case 3:
                        suffix = "rd";
                        break;
                    default:
                        suffix = "th";
                        break;
                }
            }
            return String.Format("{0}{1}", number, suffix);
        }

        ///// <summary>
        /////   Return Class color, limited cause Irc doesn't have that many colors.
        ///// </summary>
        ///// <param name="wClass"> </param>
        ///// <param name="message"> </param>
        ///// <returns> </returns>
        //public static string ClassColor(WoWClass wClass, string message)
        //{
        //    switch (wClass)
        //    {
        //        case WoWClass.Priest:
        //            return GColor(IrcColor.White, message);
        //        case WoWClass.Rogue:
        //            return GColor(IrcColor.Yellow, message);
        //        case WoWClass.DeathKnight:
        //            return GColor(IrcColor.Red, message);
        //        case WoWClass.Paladin:
        //            return GColor(IrcColor.Pink, message);
        //        case WoWClass.Warlock:
        //            return GColor(IrcColor.Purple, message);
        //        case WoWClass.Shaman:
        //            return GColor(IrcColor.Blue, message);
        //        case WoWClass.Mage:
        //            return GColor(IrcColor.Teal, message);
        //        case WoWClass.Druid:
        //            return GColor(IrcColor.Orange, message);
        //        default:
        //            return message;
        //    }
        //}

        /// <summary>
        ///   Convert the first character in the string to uppercase.
        /// </summary>
        /// <param name="o"> </param>
        /// <returns> </returns>
        public static string UppercaseFirst(object o)
        {
            return UppercaseFirst(o.ToString());
        }

        #endregion

        #region Nested type: IrcConstants

        public class IrcConstants
        {
            public const char CtcpChar = '\x1';
            public const char IrcBold = '\x2';
            public const char IrcColor = '\x3';
            public const char IrcReverse = '\x16';
            public const char IrcNormal = '\xf';
            public const char IrcUnderline = '\x1f';
            public const char CtcpQuoteChar = '\x20';
        }

        #endregion
    }

    public struct IrcColor
    {
        public static string White
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "0") : ""; }
        }

        public static string Black
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "1") : ""; }
        }

        public static string BlueNavy
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "2") : ""; }
        }

        public static string Blue
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "3") : ""; }
        }

        public static string Red
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "4") : ""; }
        }

        public static string Orange
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "5") : ""; }
        }

        public static string Yellow
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "6") : ""; }
        }

        public static string Green
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "7") : ""; }
        }

        public static string Teal
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "8") : ""; }
        }

        public static string Aqua
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "9") : ""; }
        }

        public static string DarkBlue
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "10") : ""; }
        }

        public static string Pink
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "11") : ""; }
        }

        public static string Grey
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "12") : ""; }
        }

        public static string Purple
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "13") : ""; }
        }

        public static string DarkGrey
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "14") : ""; }
        }

        public static string LightGrey
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char)3 + "15") : ""; }
        }

        //public static string GetColorSetting(string color)
        //{
        //    switch (color)
        //    {
        //        case "White":
        //            return White;
        //        case "Black":
        //            return Black;
        //        case "BlueNavy":
        //            return BlueNavy;
        //        case "Blue":
        //            return Blue;
        //        case "Red":
        //            return Red;
        //        case "Orange":
        //            return Orange;
        //        case "Yellow":
        //            return Yellow;
        //        case "Green":
        //            return Green;
        //        case "Teal":
        //            return Teal;
        //        case "Aqua":
        //            return Aqua;
        //        case "DarkBlue":
        //            return DarkBlue;
        //        case "Pink":
        //            return Pink;
        //        case "Grey":
        //            return Grey;
        //        case "Purple":
        //            return Purple;
        //        case "DarkGrey":
        //            return DarkGrey;
        //        case "LightGrey":
        //            return LightGrey;
        //        default:
        //            return "";
        //    }
        //}
    }
}
