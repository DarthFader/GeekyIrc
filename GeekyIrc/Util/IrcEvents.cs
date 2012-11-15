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
// Updated 2012 05 30 21:35


using System;
using System.Collections.Generic;
using Meebey.SmartIrc4net;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals;

namespace GeekyIrc
{
    public partial class GeekyIrc
    {
        public void OnError
            (object sender,
             ErrorEventArgs e)
        {
            Debug
                (String.Format
                     ("OnErrorEvent: {0}",
                      e.ErrorMessage));
        }

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
                                     () => { Lua.DoString(string.Format("RunMacroText(\"/AFK\")")); });
                                break;
                            case "dnd":
                                Run
                                    (e.Data,
                                     () => { Lua.DoString(string.Format("RunMacroText(\"/DND\")")); });
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
                                             Irc.SendMessage
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
                            case "testcolor":
                                var testcolor = new List<string>
                                                {
                                                    IrcColor.White + "White",
                                                    IrcColor.Black + "Black",
                                                    IrcColor.BlueNavy + "BlueNavy",
                                                    IrcColor.Blue + "Blue",
                                                    IrcColor.Red + "Red",
                                                    IrcColor.Orange + "Orange",
                                                    IrcColor.Yellow + "Yellow",
                                                    IrcColor.Green + "Green",
                                                    IrcColor.Teal + "Teal",
                                                    IrcColor.Aqua + "Aqua",
                                                    IrcColor.DarkBlue + "DarkBlue",
                                                    IrcColor.Pink + "Pink",
                                                    IrcColor.Grey + "Grey",
                                                    IrcColor.Purple + "Purple",
                                                    IrcColor.DarkGrey + "DarkGrey",
                                                    IrcColor.LightGrey + "LightGrey",
                                                    String.Format
                                                        ("{0}R{1}a{2}i{3}n{4}b{5}o{6}w!",
                                                         IrcColor.Red,
                                                         IrcColor.Orange,
                                                         IrcColor.Yellow,
                                                         IrcColor.Green,
                                                         IrcColor.Blue,
                                                         IrcColor.Purple,
                                                         IrcColor.Pink)
                                                };
                                RequestList
                                    (e.Data,
                                     testcolor);
                                break;

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
                                Irc.SendReply
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
                                         Irc.SendReply
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
                                Irc.SendReply
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
                                         DisconnectIrc("die command");
                                     });
                                break;
                        }
                        break;
                    case false:
                        // Todo handle this
                        //WriteDebugLog
                        //    (String.Format
                        //         ("[IRC] Command is wrong, We got {0} + {2}, we expected {1}",
                        //          e.Data.MessageArray[0],
                        //          ConfigValues.Instance.CommandPrefix, e.Data.Message));
                        break;
                }
            }

            catch (Exception ircMessException)
            {
                //TODO: Handle this.
            }
        }
    }
}