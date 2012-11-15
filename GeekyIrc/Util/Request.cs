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
// Updated 2012 05 30 21:34


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bots.Gatherbuddy;
using Meebey.SmartIrc4net;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GeekyIrc
{
    public partial class GeekyIrc
    {
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

        public static void BotStatus(IrcMessageData data)
        {
            Irc.SendMessage
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
                        Write(e.Message);
                    }
                }
            }
            catch (Exception listEx)
            {
                Write(string.Format("{0}", listEx));
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
        public static void Request(IrcMessageData data, object message)
        {
            try
            {
                if (!IsValid(data))
                    RejectUser(data);
                else
                {
                    string outmsg = String.Format
                        ("{1} used {0} in {2} Replying : {3}", data.Message, data.Nick, data.Channel, message);
                    Irc.SendReply(data, message.ToString());
                    if (!ConfigValues.Instance.LogItAll)
                        Write(outmsg);
                    Write(outmsg);
                }
            }
            catch (Exception rException)
            {
                Debug("{0}", rException);
            }
        }

        /// <summary>
        ///   Handle List Requests
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="list"> </param>
        public static void RequestList(IrcMessageData data, List<string> list)
        {
            RequestList(data, list, "status");
        }

        /// <summary>
        ///   Handle List Requests
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="list"> </param>
        /// <param name="message"> </param>
        public static void RequestList(IrcMessageData data, List<string> list, string message)
        {
            try
            {
                if (!IsValid(data))
                    RejectUser(data);
                else
                {
                    Write
                        (Color.Coral,
                         String.Format
                             ("{1} used {0} in {2} Replying {3} list",
                              data.Message.ToUpper(),
                              data.Nick,
                              data.Channel,
                              message));


                    list.ForEach(t => Run(data, () =>
                                                    {
                                                        int num = list.IndexOf(t) + 1;
                                                        SendIrc(string.Format("{0}/{1} : {2}",
                                                                              num.ToString("00"),
                                                                              list.Count, t));
                                                    }));
                }
            }
            catch (Exception rListException)
            {
                Debug("{0}", rListException);
            }
        }

        /// <summary>
        ///   Send IrcMessages, channel/privmsg depending on settings.
        /// </summary>
        /// <param name="message"> </param>
        public static void SendIrc(string message)
        {
            if (ConfigValues.Instance.LogAllInPrivateMessages)
                Irc.SendMessage(SendType.Message, ConfigValues.Instance.LogAllInPrivateMessagesNick, message);
            else
                Irc.SendMessage(SendType.Message, ConfigValues.Instance.IrcChannel, message);
        }


        /// <summary>
        ///   Return items based on overloaded command
        /// </summary>
        /// <param name="data"> </param>
        public static void GetInventoryItems(IrcMessageData data)
        {
            switch (data.MessageArray[1].Trim(' '))
            {
                case "all":
                    SendInventoryItemList(data);
                    break;
                case "gray":
                    SendInventoryQualityItemList(data, (int) ItemQuality.Grey);
                    break;
                case "grey":
                    SendInventoryQualityItemList(data, (int) ItemQuality.Grey);
                    break;
                case "white":
                    SendInventoryQualityItemList(data, (int) ItemQuality.White);
                    break;
                case "green":
                    SendInventoryQualityItemList(data, (int) ItemQuality.Green);
                    break;
                case "blue":
                    SendInventoryQualityItemList(data, (int) ItemQuality.Blue);
                    break;
                case "epic":
                    SendInventoryQualityItemList(data, (int) ItemQuality.Epic);
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
                    Irc.SendReply
                        (data,
                         IrcColor.Red + String.Format("Incorrect, {0}items <type>", ConfigValues.Instance.CommandPrefix));
                    Irc.SendReply
                        (data,
                         IrcColor.Teal +
                         String.Format
                             ("For example, \"{0}items green\" will list all green items.",
                              ConfigValues.Instance.CommandPrefix));
                    Irc.SendReply(data, IrcColor.Teal + String.Format("Available overloads"));
                    Irc.SendReply(data, IrcColor.Teal + String.Format("Green, Grey/Gray,"));
                    Irc.SendReply(data, IrcColor.Teal + String.Format("White, Blue, Epic"));
                    Irc.SendReply(data, IrcColor.Teal + String.Format("Ore, leather and cloth."));
                    break;
            }
        }

        /// <summary>
        ///   Sends all the Items in the bag.
        /// </summary>
        /// <param name="data"> </param>
        public static void SendInventoryItemList(IrcMessageData data)
        {
            var itemList = new List<string>();
            foreach (WoWItem bagItem in
                StyxWoW.Me.BagItems.Where(bagItem => !itemList.Contains(bagItem.Name)))
                itemList.Add(bagItem.Name);

            try
            {
                itemList.ForEach(t =>
                                     {
                                         int ind = itemList.IndexOf(t) + 1;
                                         var outmsg =
                                             Lua.GetReturnVal<int>(String.Format("return GetItemCount(\"{0}\")", t), 0);
                                         Irc.SendReply
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

            Irc.SendReply
                (data, "I'm sorry if I didn't find what you were looking for, blame Geeekzor for being a shitty coder");
        }

        /// <summary>
        ///   Gets the quality of the item.
        /// </summary>
        /// <param name="id"> ItemID </param>
        /// <returns> ItemQuality </returns>
        public static int GetItemQuality(uint id)
        {
            return Lua.GetReturnVal<int>(String.Format("return GetItemInfo(\"{0}\")", id), 2);
        }

        /// <summary>
        ///   Send a list of items based on the quality.
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="quality"> </param>
        public static void SendInventoryQualityItemList(IrcMessageData data, int quality)
        {
            var itemList = new List<string>();
            foreach (WoWItem bagItem in
                StyxWoW.Me.BagItems.Where
                    (bagItem => !itemList.Contains(bagItem.Name) && GetItemQuality(bagItem.ItemInfo.Id) == quality))
                itemList.Add(bagItem.Name);
            try
            {
                itemList.ForEach(t =>
                                     {
                                         int ind = itemList.IndexOf(t) + 1;
                                         var outmsg =
                                             Lua.GetReturnVal<int>(String.Format("return GetItemCount(\"{0}\")", t), 0);
                                         Irc.SendReply
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
            Irc.SendReply
                (data, "I'm sorry if I didn't find what you were looking for, blame Geeekzor for being a shitty coder");
        }

        /// <summary>
        ///   Send a list of items based on types.
        /// </summary>
        /// <param name="data"> </param>
        /// <param name="type"> Item Type </param>
        public static void SendInventoryItemList(IrcMessageData data, string type)
        {
            var itemList = new List<string>();
            foreach (WoWItem bagItem in
                StyxWoW.Me.BagItems.Where
                    (bagItem => !itemList.Contains(bagItem.Name) && bagItem.Name.ToLower().Contains(type.ToLower())))
                itemList.Add(bagItem.Name);

            Irc.SendReply
                (data, String.Format("Yes master, here is a list with items containing {0}", UppercaseFirst(type)));


            try
            {
                itemList.ForEach(t =>
                                     {
                                         int ind = itemList.IndexOf(t) + 1;
                                         var outmsg =
                                             Lua.GetReturnVal<int>(String.Format("return GetItemCount('{0}')", t), 0);
                                         Irc.SendReply
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

            Irc.SendReply
                (data, "I'm sorry if I didn't find what you were looking for, blame Geeekzor for being a shitty coder");
        }

        /// <summary>
        ///   If a user isn't valid we will announce the person and the command he tried to use.
        /// </summary>
        /// <param name="data"> </param>
        public static void RejectUser(IrcMessageData data)
        {
            try
            {
                string outMsg = String.Format("Invalid user {0} tried {1}!", data.Nick, IrcColor.Red + data.Message);

                WriteChatLog(outMsg);
                Irc.SendReply(data, outMsg);
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
        public static void Run(IrcMessageData data, Action rAction)
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
        public static bool IsValid(IrcMessageData data)
        {
            return !ConfigValues.Instance.UseNickSecurity ||
                   (ConfigValues.Instance.UseNickSecurity &&
                    ConfigValues.Instance.ListenToSpecificNick.Any(nick => data.Nick == nick));
        }
    }
}