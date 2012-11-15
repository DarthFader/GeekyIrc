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
// Created 2012 05 30 15:28
// Updated 2012 05 30 21:35


namespace GeekyIrc
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Meebey.SmartIrc4net;
    using Styx.WoWInternals;

    public partial class GeekyIrc
    {
        #region BattleNet

        private int BnetReply;

        internal int TotalFriendsOnline
        {
            get { return Lua.GetReturnVal<int>(string.Format("return BNGetNumFriends()"), 1); }
        }

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
                    Write(msg);
                }
            }
        }

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
                    SendIrc("Whisper someone first.");
            }
        }

        internal int PresenceId(int index)
        {
            return Lua.GetReturnVal<int>((string.Format("return BNGetFriendInfo({0})", index)), 1);
        }

        #endregion

        private void BnChatMonitor(object sender, LuaEventArgs args)
        {
            if (!ConfigValues.Instance.LogBattleNet)
                return;
            if (ConfigValues.Instance.DebugLogging)
                Write(args.EventName);

            var outMsg = string.Format("[BNet Whisper] from {0} (PID:{2}) msg : {1}",
                                       GetFriendInfoByPid(Convert.ToInt16(args.Args[12])), args.Args[0], args.Args[12]);

            WowChatOut(Color.Aqua, IrcColor.GetColorSetting(ConfigValues.Instance.BattleNetColor), outMsg);
        }

        private void BnInform(object sender, LuaEventArgs args)
        {
            if (!ConfigValues.Instance.LogBattleNet)
                return;
            if (ConfigValues.Instance.DebugLogging)
                Write(args.EventName);

            var outMsg = string.Format("[BNet Whisper] to {0} (PID:{2}) msg : {1}",
                                       GetFriendInfoByPid(Convert.ToInt16(args.Args[12])), args.Args[0], args.Args[12]);

            WowChatOut(Color.Aqua, IrcColor.GetColorSetting(ConfigValues.Instance.BattleNetColor), outMsg);
        }

        private void BnFriendsOff(object sender, LuaEventArgs args)
        {
            var outMsg = (string.Format("{0} logged off", GetFriendInfoByPid(Convert.ToInt16(args.Args[0]))));
            //WowChatOut(Color.Aqua, IrcColor.GetColorSetting(ConfigValues.Instance.BattleNetColor), outMsg);
        }

        private void BnFriendsOn(object sender, LuaEventArgs args)
        {
            var outMsg = (string.Format("{0} logged on", GetFriendInfoByPid(Convert.ToInt16(args.Args[0]))));
            WowChatOut(Color.Aqua, IrcColor.GetColorSetting(ConfigValues.Instance.BattleNetColor), outMsg);
        }
    }
}