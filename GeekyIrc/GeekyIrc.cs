// Copyright (c) 2012, Geeekzor
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met: 
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer. 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
// The views and conclusions contained in the software and documentation are those
// of the authors and should not be interpreted as representing official policies, 
// either expressed or implied, of the GeekyIrc Project.
// 
// Created 2012 05 29 16:15
// Updated 2012 05 30 21:32


using System.Collections.ObjectModel;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Plugins;

namespace GeekyIrc
{
    using System;
    using System.Drawing;
    using System.Text;
    using System.Threading;
    using Meebey.SmartIrc4net;
    using Styx.Helpers;
    using Styx.WoWInternals;

    public partial class GeekyIrc : HBPlugin
    {
        #region Fields

        public static IrcClient Irc = new IrcClient();
        private readonly WaitTimer _ircListen = WaitTimer.OneSecond;
        public Thread listen;

        public static string ReplyTarget { get; set; }

        public bool Blocking { get; set; }
        public bool IsInitialized { get; set; }

        #endregion

        #region HBEvents


        private void Logging_OnWrite(ReadOnlyCollection<Logging.LogMessage> messages)
        {
            if (!ConfigValues.Instance.LogItAll)
                return;
            
            //SendIrc(string.Format("[XLog] : {0}", LoggingColor(messages., message)));

            messages.ForEach(e =>
                                 {
                                     if (!e.Message.Contains("[GeekyIrc]") && e.Level == LogLevel.Normal)
                                     {
                                         SendIrc(string.Format("[XLog] : {0}", e.Message));
                                     }
                                 });
        }

        private void BotEvents_OnBotChanged(BotEvents.BotChangedEventArgs args)
        {
            SendIrc(string.Format("Changed bot to {0} (Old: {1} )", args.NewBot.Name, args.OldBot.Name));
        }

        private void BotEvents_OnBotStarted(EventArgs args)
        {
            if (ConfigValues.Instance.LogItAll)
                SendIrc("Bot Started");
        }

        private void BotEvents_OnBotStopped(EventArgs args)
        {
            if (ConfigValues.Instance.LogItAll)
                SendIrc("Bot Stopped");
        }

        #endregion

        private void ListenThread()
        {
            try
            {
                Irc.Connect(ConfigValues.Instance.IrcAddress, ConfigValues.Instance.IrcPort);
                Irc.RfcJoin(ConfigValues.Instance.IrcChannel);
            }
            catch (ConnectionException ce)
            {
                Debug(String.Format("[Connection Error] Reason: {0}", ce.Message));
            }

            try
            {
                Irc.Login
                    (ConfigValues.Instance.IrcUsername,
                     "MrSmith",
                     ConfigValues.Instance.IrcUsermode,
                     ConfigValues.Instance.IrcUsername[0],
                     ConfigValues.Instance.IrcPassword);
            }
            catch (ConnectionException ce)
            {
                Debug(String.Format("[Connection Error] Reason: {0}", ce.Message));
            }
            catch (Exception e)
            {
                Debug(String.Format("[Error] Reason: {0} Source:{1}", e.Message, e.Source));
            }
            try
            {
                Write("Listening to IRC");

                listen = new Thread
                    (() =>
                         {
                             Irc.Listen(Blocking);
                             DisconnectIrc
                                 (string.Format
                                      ("Listen ended {0}",
                                       this.IsInitialized ? ", disconnected. " : " plugin isn't enabled."));
                         }) {IsBackground = true, Name = "IrcListeningThread"};
                listen.Start();
            }
            catch
            {
            }
        }

        private void Irc_OnConnected(object sender, EventArgs e)
        {
        }

        public static void ConnectIrc(bool block, params object[] message)
        {
            if (block)
                return;

            if (message.Length > 1)
                Debug(string.Format("{0}", message));
            try
            {
                if (!Irc.IsConnected)
                    Irc.Connect(ConfigValues.Instance.IrcAddress, ConfigValues.Instance.IrcPort);
                if (!Irc.IsJoined(ConfigValues.Instance.IrcChannel, ConfigValues.Instance.IrcUsername[0]))
                    Irc.RfcJoin(ConfigValues.Instance.IrcChannel);
            }
            catch (ConnectionException ce)
            {
                Debug(String.Format("[Connection Error] Reason: {0}", ce.Message));
            }

            try
            {
                Irc.Login(ConfigValues.Instance.IrcUsername, "MrSmith");
            }
            catch (ConnectionException ce)
            {
                Debug(String.Format("[Connection Error] Reason: {0}", ce.Message));
            }
            catch (Exception e)
            {
                Debug(String.Format("[Error] Reason: {0} Source:{1}", e.Message, e.Source));
            }
        }

        public static void DisconnectIrc(params object[] message)
        {
            try
            {
                if (message != null)
                    Write(string.Format("{0}", message));
                Irc.Disconnect();
                Irc.RfcDie();
            }
            catch (Exception dex)
            {
                Debug(dex.Message);
            }
        }

        #region {{ HB Override }}

        public override string Name
        {
            get { return "GeekyIrc"; }
        }

        public override string Author
        {
            get { return "Geeekzor"; }
        }

        public override Version Version
        {
            get { return MVersion; }
        }

        public override bool WantButton
        {
            get { return true; }
        }

        public override string ButtonText
        {
            get { return Name + " Config"; }
        }

        public override void Pulse()
        {
            try
            {
                if (!_ircListen.IsFinished)
                    return;

                if (!Irc.IsConnected) ListenThread();

                if (ConfigForm.Instance != null)
                {
                    ConfigForm.Instance._lblAddress.ForeColor = Irc.IsConnected ? Color.Green : Color.Red;
                    ConfigForm.Instance._lblChannel.ForeColor = Irc.IsJoined
                                                                    (ConfigValues.Instance.IrcChannel,
                                                                     ConfigValues.Instance.IrcUsername[0])
                                                                    ? Color.Green
                                                                    : Color.Red;
                    ConfigForm.Instance._lblBlocking.Text = string.Format("Blocking : {0}", Blocking);
                    ConfigForm.Instance._lblAddress.Text = string.Format
                        ("{0}", Irc.IsConnected ? "Connected" : "Not Connected");
                    ConfigForm.Instance._lblChannel.Text = Irc.JoinedChannels.ToRealString();
                }

                _ircListen.Reset();
            }
            catch
            {
            }
        }

        public override void Initialize()
        {
            if (this.IsInitialized)
                return;

            ConfigValues.Instance.Load();

            Blocking = true;
            this.IsInitialized = true;

            Write(Color.Yellow, string.Format("Started, Version {0}", MVersion));

            Irc.Encoding = Encoding.UTF8;
            Irc.SendDelay = ConfigValues.Instance.UseIrcSendDelay ? ConfigValues.Instance.IrcSendDelay : 200;

            Irc.ActiveChannelSyncing = Blocking;

            Irc.AutoRetry = Blocking;
            Irc.AutoRejoin = Blocking;
            Irc.AutoRejoinOnKick = Blocking;
            Irc.AutoRelogin = Blocking;
            Irc.AutoReconnect = Blocking && ConfigValues.Instance.AutoReconnect;
            Irc.AutoRetryDelay = ConfigValues.Instance.ReconnectDelay;
            Irc.CtcpVersion = String.Format("GeekyIrc {0}", MVersion);

            Irc.OnRawMessage += OnRawMessage;
            Irc.OnError += OnError;
            Irc.OnConnected += Irc_OnConnected;

            BotEvents.OnBotStopped += BotEvents_OnBotStopped;
            BotEvents.OnBotStarted += BotEvents_OnBotStarted;
            BotEvents.OnBotChanged += BotEvents_OnBotChanged;

            BotEvents.Player.OnLevelUp += Player_OnLevelUp;
            BotEvents.Player.OnPlayerDied += Player_OnPlayerDied;
            BotEvents.Player.OnMobKilled += Player_OnMobKilled;

            //Logging.OnWrite += Logging_OnWrite;

            Logging.OnLogMessage += Logging_OnWrite;

            Lua.Events.AttachEvent("CHAT_MSG_LOOT", LootChatMonitor);
            Lua.Events.AttachEvent("ACHIEVEMENT_EARNED", AchievementMonitor);
            Lua.Events.AttachEvent("CHAT_MSG_BN_WHISPER", BnChatMonitor);
            Lua.Events.AttachEvent("CHAT_MSG_BN_CONVERSATION", BnChatMonitor);
            Lua.Events.AttachEvent("BN_FRIEND_ACCOUNT_ONLINE", BnFriendsOn);
            Lua.Events.AttachEvent("BN_FRIEND_ACCOUNT_OFFLINE", BnFriendsOff);
            Lua.Events.AttachEvent("CHAT_MSG_BN_WHISPER_INFORM", BnInform);


            
            Chat.Whisper += WhisperChatMonitor;
            Chat.WhisperTo += WoWChat_WhisperTo;
            Chat.Say += SayChatMonitor;
            Chat.Battleground += BgChatMonitor;
            Chat.BattlegroundLeader += BgChatMonitor;
            Chat.Party += PartyChatMonitor;
            Chat.PartyLeader += PartyChatMonitor;
            Chat.Guild += GuildChatMonitor;
            Chat.Officer += OfficerChatMonitor;
            Chat.Raid += RaidChatMonitor;
            Chat.RaidLeader += RaidChatMonitor;

            ListenThread();
        }

        public override void Dispose()
        {
            if (listen != null)
                listen.Abort();

            Irc.Disconnect();
            Irc.RfcQuit(Priority.High);
            Irc.RfcDie();

            BotEvents.OnBotStopped -= BotEvents_OnBotStopped;
            BotEvents.OnBotStarted -= BotEvents_OnBotStarted;
            BotEvents.OnBotChanged -= BotEvents_OnBotChanged;

            BotEvents.Player.OnLevelUp -= Player_OnLevelUp;
            BotEvents.Player.OnPlayerDied -= Player_OnPlayerDied;
            BotEvents.Player.OnMobKilled -= Player_OnMobKilled;

            //Logging.OnWrite -= Logging_OnWrite;
            Logging.OnLogMessage += Logging_OnWrite;

            Lua.Events.DetachEvent("CHAT_MSG_LOOT", LootChatMonitor);
            Lua.Events.DetachEvent("ACHIEVEMENT_EARNED", AchievementMonitor);
            Lua.Events.DetachEvent("CHAT_MSG_BN_WHISPER", BnChatMonitor);
            Lua.Events.DetachEvent("CHAT_MSG_BN_CONVERSATION", BnChatMonitor);
            Lua.Events.DetachEvent("BN_FRIEND_ACCOUNT_ONLINE", BnFriendsOn);
            Lua.Events.DetachEvent("BN_FRIEND_ACCOUNT_OFFLINE", BnFriendsOff);

            Chat.Whisper -= WhisperChatMonitor;
            Chat.WhisperTo -= WoWChat_WhisperTo;
            Chat.Say -= SayChatMonitor;
            Chat.Battleground -= BgChatMonitor;
            Chat.BattlegroundLeader -= BgChatMonitor;
            Chat.Party -= PartyChatMonitor;
            Chat.PartyLeader -= PartyChatMonitor;
            Chat.Guild -= GuildChatMonitor;
            Chat.Officer -= OfficerChatMonitor;
            Chat.Raid -= RaidChatMonitor;
            Chat.RaidLeader -= RaidChatMonitor;

            this.IsInitialized = false;
        }

        public override void OnButtonPress()
        {
            if (!ConfigForm.IsValid)
                new ConfigForm().Show();
            else
                ConfigForm.Instance.Activate();
        }

        #endregion
    }
}