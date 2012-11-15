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


using Styx.Common;

namespace GeekyIrc
{
    using System;
    using System.Globalization;
    using System.IO;
    using Meebey.SmartIrc4net;
    using Styx;
    using Styx.Helpers;
    using System.Windows.Media;
    using Color = System.Drawing.Color;

    public partial class GeekyIrc
    {
        public static System.Windows.Media.Color GetColor(Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static void Write(Color clr, string format, params object[] args)
        {
            Logging.Write(GetColor(clr), string.Format("[GeekyIrc] {0}", String.Format(format, args)));
        }

        public static void Write(string format, params object[] args)
        {
            Logging.Write(GetColor(Color.Yellow), string.Format("[GeekyIrc] {0}", String.Format(format, args)));
        }

        public static void Debug(string format, params object[] args)
        {
            Debug(Color.Tomato, format, args);
        }

        public static void Debug(Color clr, string format, params object[] args)
        {
            Logging.WriteDiagnostic(GetColor(clr), string.Format("[GeekyIrc] {0}", String.Format(format, args)));
        }

        public static void WriteChatLog(object msg)
        {
            string path = String.Format("{0}\\GeekyIrc\\Logs\\", Styx.Plugins.PluginManager.PluginsDirectory);

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                File.AppendAllText(string.Format("{0}\\{1}",path, StyxWoW.Me.Name), msg.ToString());
            }
            catch (Exception file)
            {
                Logging.WriteDiagnostic(String.Format("[CHATLOG] {0}", file.Message));
            }
        }

        public static void OpMe(IrcMessageData data)
        {
            foreach (var meNick in ConfigValues.Instance.IrcUsername)
            {
                ChannelUser meUsr = Irc.GetChannelUser(ConfigValues.Instance.IrcChannel, meNick);

                if (!meUsr.IsOp)
                    Irc.SendReply(data, IrcColor.Red + "I'm not a channel op, sorry!");
                else
                {
                    if (Irc.GetChannelUser(data.Channel, data.Nick).IsOp)
                        Irc.SendReply(data, IrcColor.Red + "You're already a channel operator!");
                    else
                    {
                        Irc.RfcPrivmsg(data.Nick, "+o on you!");
                        Irc.Op(data.Channel, data.Nick);
                    }
                }
            }
        }

        public static void WowChatOut(Color clr, string ircClr, string message)
        {
            if (ConfigValues.Instance.LogInHbsLog) Write(clr, message);

            SendIrc(ircClr + message);
        }
    }
}