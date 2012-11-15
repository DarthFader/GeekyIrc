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
// Created 2012 05 31 07:24
// Updated 2012 06 02 14:19


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Meebey.SmartIrc4net;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;

namespace GeekyIrc
{
    public partial class GeekyIrc
    {
        public static List<string> ChatList = new List<string>();

        private static void PartyChatMonitor(Chat.ChatLanguageSpecificEventArgs args)
        {
            if (ConfigValues.Instance.NotifyParty &&
                args.Args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                OurNameNotification("[Party]", args.Args[1], ConfigValues.Instance.IrcChannel);

            if (!ConfigValues.Instance.LogParty)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Args[1].ToString() == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format("[Party] {1} wrote : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.DeepSkyBlue, IrcColor.GetColorSetting(ConfigValues.Instance.PartyColor), outMsg);
        }

        private void WhisperChatMonitor(Chat.ChatWhisperEventArgs args)
        {
            if (!ConfigValues.Instance.LogWhispers)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Author == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format
                ("[Whisper] from {1} Msg : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.Magenta, IrcColor.GetColorSetting(ConfigValues.Instance.WhisperColor), outMsg);
        }


        private void GuildChatMonitor(Chat.ChatGuildEventArgs args)
        {
            if (ConfigValues.Instance.NotifyGuild &&
                args.Args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                OurNameNotification("[Guild]", args.Args[1], ConfigValues.Instance.IrcChannel);

            if (!ConfigValues.Instance.LogGuild)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Args[1].ToString() == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format("[Guild] {1} wrote : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.Green, IrcColor.GetColorSetting(ConfigValues.Instance.GuildColor), outMsg);
        }

        private void BgChatMonitor(Chat.ChatLanguageSpecificEventArgs args)
        {
            if (ConfigValues.Instance.NotifyBg && args.Args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                OurNameNotification("[Battleground]", args.Args[1], ConfigValues.Instance.IrcChannel);

            if (!ConfigValues.Instance.LogBattleground)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Args[1].ToString() == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format("[BG] {1} wrote : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.Orange, IrcColor.GetColorSetting(ConfigValues.Instance.BgColor), outMsg);
        }

        private void RaidChatMonitor(Chat.ChatLanguageSpecificEventArgs args)
        {
            if (ConfigValues.Instance.NotifyRaid &&
                args.Args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                OurNameNotification("[Raid]", args.Args[1], ConfigValues.Instance.IrcChannel);

            if (!ConfigValues.Instance.LogRaid)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Args[1].ToString() == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format("[Raid] {1} wrote : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.Orange, IrcColor.GetColorSetting(ConfigValues.Instance.RaidColor), outMsg);
        }

        private void OfficerChatMonitor(Chat.ChatLanguageSpecificEventArgs args)
        {
            if (ConfigValues.Instance.NotifyOfficer &&
                args.Args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                OurNameNotification("[Officer]", args.Args[1], ConfigValues.Instance.IrcChannel);

            if (!ConfigValues.Instance.LogOfficer)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Args[1].ToString() == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format("[Officer] {1} wrote : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.Green, IrcColor.GetColorSetting(ConfigValues.Instance.OfficerColor), outMsg);
        }

        private void SayChatMonitor(Chat.ChatLanguageSpecificEventArgs args)
        {
            if (ConfigValues.Instance.NotifySay && args.Args[0].ToString().ToLower().Contains(StyxWoW.Me.Name.ToLower()))
                OurNameNotification("[Say]", args.Args[1], ConfigValues.Instance.IrcChannel);

            if (!ConfigValues.Instance.LogSay)
                return;
            if (!ConfigValues.Instance.LogOwn && args.Args[1].ToString() == StyxWoW.Me.Name)
                return;

            string outMsg = String.Format("[Say] {1} wrote : {0}", RemoveChatFormatting(args.Message), args.Author);

            WowChatOut(Color.White, IrcColor.GetColorSetting(ConfigValues.Instance.SayColor), outMsg);
        }

        private void Player_OnLevelUp(BotEvents.Player.LevelUpEventArgs args)
        {
            if (!ConfigValues.Instance.NotifyLevelUp)
                return;

            WowChatOut(Color.Yellow, IrcColor.Yellow, string.Format("We leveled up, I'm {0} now!", args.NewLevel));
        }

        private void Player_OnMobKilled(BotEvents.Player.MobKilledEventArgs args)
        {
            if (ConfigValues.Instance.NotifyMobKilled)
            {
                SendIrc(String.Format("{0} killed {1}", StyxWoW.Me.Name, args.KilledMob.Name));
            }
        }

        private void Player_OnPlayerDied()
        {
            if (!ConfigValues.Instance.NotifyDeath)
                return;

            SendIrc(string.Format("{0} died.", StyxWoW.Me.Name));
            //WowChatOut(IrcColor.Red,
            //           string.Format("We died! ({0} time)", Ordinal(Convert.ToInt32(GameStats.Deaths) + 1)),
            //           false);
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
                // Catch some wierd exceptions
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

        private void WoWChat_WhisperTo(Chat.ChatLanguageSpecificEventArgs e)
        {
            if (!ConfigValues.Instance.LogOwn)
                return;
            string outmsg = string.Format("[Whispered] {0} : {1}", e.Author, e.Message);
            WowChatOut
                (Color.Magenta,
                 IrcColor.GetColorSetting(ConfigValues.Instance.WhisperColor),
                 RemoveChatFormatting(outmsg));
        }

        public static void SendWhisper(IrcMessageData data)
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

                if (!ConfigValues.Instance.LogItAll)
                    Write(Color.Magenta, outMsg);

                ReplyTarget = target;

                if (ConfigValues.Instance.UseNickSecurity &&
                    !ConfigValues.Instance.ListenToSpecificNick.Contains(data.Nick))
                    RejectUser(data);
                else
                {
                    if (ConfigValues.Instance.DebugLogging)
                    {
                        string dbg = String.Format("[Whisper] Sending {0} to {1}", Lua.Escape(msg), StyxWoW.Me.Name);
                        Debug("[Whisper Debug] Whispering self with Message: {0}", msg);
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", msg, StyxWoW.Me.Name);
                        Irc.SendReply(data, dbg);
                    }
                    else
                    {
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", UppercaseFirst(Lua.Escape(msg)),
                                     target);
                        Irc.SendReply(data, IrcColor.Pink + outMsg);
                    }
                }
            }
            catch (Exception swExcep)
            {
                Debug("[SendWhisper Exception] Error: {0}", swExcep.Message);
            }
        }

        public static void SendChat(IrcMessageData data)
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
                    Irc.SendReply(data, IrcColor.Blue + outMsg);

                if (ConfigValues.Instance.DebugLogging)
                    Debug(String.Format("Chat should write in \"{0}\" and the message would be {1}", target,
                                        Lua.Escape(msg)));
                else
                {
                    if (target.ToUpper() == "PARTY" && !StyxWoW.Me.GroupInfo.IsInParty)
                    {
                        Irc.SendReply(data, IrcColor.Blue + "[PARTY] Not in a party!");
                        return;
                    }
                    if (target.ToUpper() == "RAID" && !StyxWoW.Me.GroupInfo.IsInRaid)
                    {
                        Irc.SendReply(data, IrcColor.Blue + "[PARTY] Not in a raid!");
                        return;
                    }

                    Write(Color.Cyan, outMsg);
                    Lua.DoString("SendChatMessage('{0}', '{1}', nil, '{1}')", UppercaseFirst(Lua.Escape(msg)),
                                 target.ToUpper());
                }
            }
            catch (Exception scExcep)
            {
                Debug(Color.Yellow, "[SendChat Exception] Error: {0}", scExcep.Message);
            }
        }

        private static void ReplyToWhisper(IrcMessageData data)
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
                        Irc.SendReply(data, String.Format("[Reply] is null, whisper someone first."));
                        return;
                    }

                    string outMsg = String.Format("[Whisper] Sending {0} to {1}", message, ReplyTarget);

                    if (!ConfigValues.Instance.LogItAll)
                        Write(Color.Magenta, outMsg);

                    if (ConfigValues.Instance.DebugLogging)
                    {
                        string dbg = String.Format("[Whisper] Sending {0} to {1}", message, StyxWoW.Me.Name);
                        Debug("[Reply Debug] Whispering self with Message: {0}", message);
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", message, StyxWoW.Me.Name);
                        Irc.SendReply(data, IrcColor.Pink + dbg);
                    }
                    else
                    {
                        Lua.DoString("SendChatMessage('{0}', 'WHISPER', nil, '{1}')", message, ReplyTarget);
                        Irc.SendReply(data, IrcColor.Pink + outMsg);
                    }
                }
            }
            catch (Exception swExcep)
            {
                Debug("[SendWhisper Exception] Error: {0}", swExcep.Message);
            }
        }

        /// <summary>
        ///   Send a Irc Message or Notice if the current players name is mentioned in a chat.
        /// </summary>
        /// <param name="args"> Chat Event Args </param>
        public static void OurNameNotification(params object[] args)
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
                    Irc.RfcNotice(ConfigValues.Instance.IrcChannel, IrcColor.Yellow + outMsg);
                else
                    Irc.SendMessage(SendType.Message, ConfigValues.Instance.IrcChannel, IrcColor.Yellow + outMsg);
                if (!ConfigValues.Instance.LogItAll)
                    Write(outMsg);
            }
            catch (Exception nException)
            {
                Debug(String.Format("[Notification Exception] {0}", nException));
                throw;
            }
        }
    }
}