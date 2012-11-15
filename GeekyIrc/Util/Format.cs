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


using Styx;

namespace GeekyIrc
{
    using System;
    using System.Text.RegularExpressions;
    using Bots.Gatherbuddy;
    using Styx.WoWInternals;

    public partial class GeekyIrc
    {
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
    }

    public partial class GeekyIrc
    {
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

        public static string PerHour(TimeSpan time, int item)
        {
            return string.Format("{0}", Round(item/GatherbuddyBot.runningTime.TotalSeconds*3600));
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
            return (int) Math.Round((double) i, 1);
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
            else
                return s;
        }

        public static string Ordinal(int number)
        {
            string suffix;
            int ones = number%10;
            int tens = (int) Math.Floor(number/10M)%10;
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

        /// <summary>
        ///   Return Class color, limited cause Irc doesn't have that many colors.
        /// </summary>
        /// <param name="wClass"> </param>
        /// <param name="message"> </param>
        /// <returns> </returns>
        public static string ClassColor(WoWClass wClass, string message)
        {
            switch (wClass)
            {
                case WoWClass.Priest:
                    return GColor(IrcColor.White, message);
                case WoWClass.Rogue:
                    return GColor(IrcColor.Yellow, message);
                case WoWClass.DeathKnight:
                    return GColor(IrcColor.Red, message);
                case WoWClass.Paladin:
                    return GColor(IrcColor.Pink, message);
                case WoWClass.Warlock:
                    return GColor(IrcColor.Purple, message);
                case WoWClass.Shaman:
                    return GColor(IrcColor.Blue, message);
                case WoWClass.Mage:
                    return GColor(IrcColor.Teal, message);
                case WoWClass.Druid:
                    return GColor(IrcColor.Orange, message);
                default:
                    return message;
            }
        }

        /// <summary>
        ///   Convert the first character in the string to uppercase.
        /// </summary>
        /// <param name="o"> </param>
        /// <returns> </returns>
        public static string UppercaseFirst(object o)
        {
            return UppercaseFirst(o.ToString());
        }

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
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "0") : ""; }
        }

        public static string Black
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "1") : ""; }
        }

        public static string BlueNavy
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "2") : ""; }
        }

        public static string Blue
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "3") : ""; }
        }

        public static string Red
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "4") : ""; }
        }

        public static string Orange
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "5") : ""; }
        }

        public static string Yellow
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "6") : ""; }
        }

        public static string Green
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "7") : ""; }
        }

        public static string Teal
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "8") : ""; }
        }

        public static string Aqua
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "9") : ""; }
        }

        public static string DarkBlue
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "10") : ""; }
        }

        public static string Pink
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "11") : ""; }
        }

        public static string Grey
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "12") : ""; }
        }

        public static string Purple
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "13") : ""; }
        }

        public static string DarkGrey
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "14") : ""; }
        }

        public static string LightGrey
        {
            get { return ConfigValues.Instance.ColorIrcMessages ? ((char) 3 + "15") : ""; }
        }

        public static string GetColorSetting(string color)
        {
            switch (color)
            {
                case "White":
                    return White;
                case "Black":
                    return Black;
                case "BlueNavy":
                    return BlueNavy;
                case "Blue":
                    return Blue;
                case "Red":
                    return Red;
                case "Orange":
                    return Orange;
                case "Yellow":
                    return Yellow;
                case "Green":
                    return Green;
                case "Teal":
                    return Teal;
                case "Aqua":
                    return Aqua;
                case "DarkBlue":
                    return DarkBlue;
                case "Pink":
                    return Pink;
                case "Grey":
                    return Grey;
                case "Purple":
                    return Purple;
                case "DarkGrey":
                    return DarkGrey;
                case "LightGrey":
                    return LightGrey;
                default:
                    return "";
            }
        }
    }
}