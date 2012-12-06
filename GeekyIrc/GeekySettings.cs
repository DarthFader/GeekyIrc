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

using Styx.Common;

namespace GeekyIrc
{
    using System;
    using System.ComponentModel;
    using System.Drawing.Design;
    using System.IO;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using Styx;
    using Styx.Helpers;
    using Styx.WoWInternals;

    #region Form

    public class ConfigForm : Form
    {
        //////////////////////////////////////////////////////////////////////////
        //                          Credits to Highvoltz                        //
        //////////////////////////////////////////////////////////////////////////
        private IContainer components;
        private Button button1;
        private PropertyGrid pGrid;

        public ConfigForm()
        {
            InitializeComponent();
            Instance = this;
            pGrid.SelectedObject = ConfigValues.Instance;

            GridItem root = pGrid.SelectedGridItem;
            while (root.Parent != null)
                root = root.Parent;

            foreach (GridItem g in root.GridItems)
                g.Expanded = false;
        }

        public static bool IsValid
        {
            get { return Instance != null && Instance.Visible && !Instance.Disposing && !Instance.IsDisposed; }
        }

        /// <summary>
        ///   Required designer variable.
        /// </summary>
        public static ConfigForm Instance { get; private set; }

        /// <summary>
        ///   Clean up any resources being used.
        /// </summary>
        /// <param name="disposing"> true if managed resources should be disposed; otherwise, false. </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        ///   Required method for Designer support - do not modify the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pGrid = new System.Windows.Forms.PropertyGrid();
            this.button1 = new System.Windows.Forms.Button();

            this.SuspendLayout();
            // 
            // pGrid
            // 
            this.pGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pGrid.Location = new System.Drawing.Point(0, 26);
            this.pGrid.Name = "pGrid";
            this.pGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.pGrid.Size = new System.Drawing.Size(283, 320);
            this.pGrid.TabIndex = 0;
            this.pGrid.ToolbarVisible = false;
            this.pGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid1_PropertyValueChanged);
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Top;
            this.button1.Location = new System.Drawing.Point(0, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(283, 26);
            this.button1.TabIndex = 2;
            this.button1.Text = "Select another profile";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // ConfigForm
            // 
            this.ClientSize = new System.Drawing.Size(283, 368);
            this.Controls.Add(this.pGrid);
            this.Controls.Add(this.button1);
            this.MinimumSize = new System.Drawing.Size(250, 300);
            this.Name = "ConfigForm";
            this.Text = "Config";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ConfigForm_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            ConfigValues.Instance.Save();
        }


        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ConfigValues.Instance.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // TODO: Fix the path
                string pth = Settings.SettingsDirectory;
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.InitialDirectory = pth;
                    ofd.Filter =
                        "GeekyIrc Setting Files (.xml)|GeekyIrc-Settings-*.xml|GeekyIrc Backup Setting Files (.bak)|GeekyIrc-Settings-*.bak";
                    ofd.FilterIndex = 1;

                    ofd.ShowDialog();

                    ConfigValues.Instance.LoadFromXML(XElement.Load(ofd.FileName));

                    ConfigValues.Instance.SaveToFile(string.Format("{0}\\GeekyIrc-Settings-{1}.bak", pth,
                                                                   StyxWoW.Me));

                    ConfigValues.Instance.Save();
                }
            }
            catch
            {
            }
        }
    }

    #endregion

    #region ConfigValues

    public class ConfigValues : Settings
    {
        public static ConfigValues Instance = new ConfigValues();

        public ConfigValues()
            : base(
                Path.Combine
                    (CharacterSettingsDirectory, string.Format("Settings\\GeekyIrc-Settings-{0}.xml", StyxWoW.Me.Name)))
        {
        }

        #region Settings

        #region Logging
        #region Debug
        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Debug")]
        [DisplayName("Debug Logging")]
        [Description("Will print the Debug to the main log window and whispers will be sent to the player.")]
        public bool DebugLogging { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Debug")]
        [DisplayName("Debug Loot Logging")]
        [Description("Debug Loot Logging")]
        public bool DebugLootLogging { get; set; }
        #endregion
        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Extreme")]
        [DisplayName("Extreme Logging")]
        [Description("Will print everything from the main log window")]
        public bool LogItAll { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Extreme")]
        [DisplayName("Use Private Messages")]
        [Description("Will print everything from the main log as private messages")]
        public bool LogAllInPrivateMessages { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("LogNick")]
        [Category("Extreme")]
        [DisplayName("Nickname")]
        [Description("Nick to send Private Messages to")]
        public string LogAllInPrivateMessagesNick { get; set; }

        #endregion

        #region IRCSettings

        [Setting]
        [Styx.Helpers.DefaultValue(new[] {"IrcUsername"})]
        [Category("IRCSettings")]
        [DisplayName("Bot Nickname")]
        [Description("The nick the bot will use")]
        public string[] IrcUsername { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(0)]
        [Browsable(false)]
        [Category("IRCSettings")]
        [DisplayName("Usermode")]
        [Description("Usermode, leave at 0")]
        public int IrcUsermode { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("IrcPassword")]
        [Category("IRCSettings")]
        [DisplayName("Password")]
        [Description("Password for the server")]
        public string IrcPassword { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("#IrcChannel")]
        [Category("IRCSettings")]
        [DisplayName("Channel")]
        [Description("Channel to join, you must start the line with #")]
        public string IrcChannel { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("IrcAddress")]
        [Category("IRCSettings")]
        [DisplayName("IpAddress")]
        [Description("IpAddress or hostname for the server")]
        public string IrcAddress { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(6667)]
        [Category("IRCSettings")]
        [DisplayName("Port")]
        [Description("Port for the IRC Server")]
        public int IrcPort { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(200)]
        [Category("IRCSettings")]
        [DisplayName("Irc Send Delay")]
        [Description("Delay for sending messages, can cause kicks if set too low, (Default: 200 milliseconds)")]
        public int IrcSendDelay { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(true)]
        [Category("IRCSettings")]
        [DisplayName("Use SendDelay")]
        [Description("Use SendDelay, can cause kicks if set to false, (Default is true)")]
        public bool UseIrcSendDelay { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(30)]
        [Category("IRCSettings")]
        [DisplayName("Reconnect Delay")]
        [Description("Reconnect Delay in seconds")]
        public int ReconnectDelay { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(true)]
        [Category("IRCSettings")]
        [DisplayName("Use AutoReconnect")]
        [Description("Use AutoReconnect")]
        public bool AutoReconnect { get; set; }

        #endregion

        #region ChatLogging

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat Formatting")]
        [DisplayName("Use Uppercase Format")]
        [Description("Convert first letter in a message to uppercase.")]
        public bool UseUppercaseFormat { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Chat in HB")]
        [Description("Bot will Log in Honorbuddys log as well")]
        public bool LogInHbsLog { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Party Chat")]
        [Description("Bot will log Party Chat")]
        public bool LogParty { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Whispers")]
        [Description("Bot will log whispers")]
        public bool LogWhispers { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Guild Chat")]
        [Description("Bot will log guild chat")]
        public bool LogGuild { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Officer Chat")]
        [Description("Bot will log officer chat")]
        public bool LogOfficer { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Raid Chat")]
        [Description("Bot will log raid chat")]
        public bool LogRaid { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Battleground Chat")]
        [Description("Bot will log battleground chat")]
        public bool LogBattleground { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Say Chat")]
        [Description("Bot will log Say chat")]
        public bool LogSay { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Your Messages")]
        [Description("Bot will log chat sent by your character.")]
        public bool LogOwn { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Chat")]
        [DisplayName("Log Battle.Net Whispers")]
        [Description("Bot will log Battle.Net Whispers")]
        public bool LogBattleNet { get; set; }

        #endregion

        #region Loot

        [Setting]
        [Styx.Helpers.DefaultValue(true)]
        [Category("Loot")]
        [DisplayName("Log Looting")]
        [Description("Bot will log looting")]
        public bool LogLoot { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(3)]
        [Category("Loot")]
        [DisplayName("LootFilter")]
        [Description("0 for Grays, 1 for Whites, 2 for greens, 3 for blues (Default is 3 / Blues)")]
        public int LootFilter { get; set; }

        #endregion

        #region Security

        [Setting]
        [Styx.Helpers.DefaultValue(new[] {"MyNick"})]
        [Category("Security")]
        [DisplayName("Nick")]
        [Description("We will only obey to this Nick")]
        public string[] ListenToSpecificNick { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Security")]
        [DisplayName("Use Nick Security")]
        [Description("Use Nick Security to prevent random persons from using Commands")]
        public bool UseNickSecurity { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Security")]
        [DisplayName("Allow Process.Kill()")]
        [Description("Allow WoWKill command")]
        public bool AllowProcessKill { get; set; }

        #endregion

        [Setting]
        [Styx.Helpers.DefaultValue("!")]
        [Category("Commands")]
        [DisplayName("Command Prefix")]
        [Description("Prefix to use with commands, single characters only, (Default is = ! )")]
        public string CommandPrefix { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("Irc Channel Notice")]
        [Category("Notifications")]
        [Description("Notify as Channel Notice")]
        public bool IrcNotice { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("If mentioned in Guild")]
        [Category("Notifications")]
        [Description("Notify if our name is mentioned Guild chat")]
        public bool NotifyGuild { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("If mentioned in Say")]
        [Category("Notifications")]
        [Description("Notify if our name is mentioned Say chat")]
        public bool NotifySay { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("If mentioned in BG")]
        [Category("Notifications")]
        [Description("Notify if our name is mentioned Battleground chat")]
        public bool NotifyBg { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("If mentioned in Raid")]
        [Category("Notifications")]
        [Description("Notify if our name is mentioned inf Raid chat")]
        public bool NotifyRaid { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("If mentioned in Party")]
        [Category("Notifications")]
        [Description("Notify if our name is mentioned in Party chat")]
        public bool NotifyParty { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [DisplayName("If mentioned in Officer")]
        [Category("Notifications")]
        [Description("Notify if our name is mentioned in Officer chat")]
        public bool NotifyOfficer { get; set; }

        [Setting]
        [Category("Notifications")]
        [Description("Send a notification if we level up")]
        [Styx.Helpers.DefaultValue(false)]
        public bool NotifyLevelUp { get; set; }

        [Setting]
        [Category("Notifications")]
        [Description("Send a notification if we died")]
        [Styx.Helpers.DefaultValue(false)]
        public bool NotifyDeath { get; set; }

        [Setting]
        [Category("Notifications")]
        [Description("Send a notification if we killed a mob")]
        [Styx.Helpers.DefaultValue(false)]
        public bool NotifyMobKilled { get; set; }

        #region profiles

        [Setting]
        [Styx.Helpers.DefaultValue(false)]
        [Category("Profiles")]
        [Description("Allow changing profiles when changing botbases")]
        public bool ChangeProfiles { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("")]
        [Category("Profiles")]
        [Description("Change to this profile when changing to Grind bot")]
        [Editor(typeof (FilteredFileNameEditor), typeof (UITypeEditor))]
        public string GrindProfile { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("")]
        [Category("Profiles")]
        [Description("Change to this profile when changing to Questing bot")]
        [Editor(typeof (FilteredFileNameEditor), typeof (UITypeEditor))]
        public string QuestProfile { get; set; }

        [Setting]
        [Styx.Helpers.DefaultValue("")]
        [Category("Profiles")]
        [Description("Change to this profile when changing to Gatherbuddy2")]
        [Editor(typeof (FilteredFileNameEditor), typeof (UITypeEditor))]
        public string Gatherbuddy2Profile { get; set; }

        #endregion

        #region IrcColors

        [Setting]
        [Styx.Helpers.DefaultValue(true)]
        [Category("IrcColor")]
        [DisplayName("Color Irc Messages")]
        [Description("Send messages with colors.")]
        public bool ColorIrcMessages { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Pink")]
        public string WhisperColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Blue")]
        public string PartyColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Green")]
        public string GuildColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Green")]
        public string OfficerColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("White")]
        public string SayColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Aqua")]
        public string BattleNetColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Orange")]
        public string RaidColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Orange")]
        public string BgColor { get; set; }

        [Setting]
        [Browsable(true)]
        [Category("IrcColor")]
        [TypeConverter(typeof (IrcColorList))]
        [Styx.Helpers.DefaultValue("Yellow")]
        public string AchievementColor { get; set; }

        #endregion

        #endregion
    }

    internal class FilteredFileNameEditor : UITypeEditor
    {
        private readonly OpenFileDialog _openFileDialog = new OpenFileDialog();

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            _openFileDialog.InitialDirectory = Settings.CharacterSettingsDirectory;
            if (value != null)
                _openFileDialog.FileName = value.ToString();
            _openFileDialog.Filter = "Profile|*.xml|All Files|*.*";
            return _openFileDialog.ShowDialog() == DialogResult.OK
                       ? _openFileDialog.FileName
                       : base.EditValue(context, provider, value);
        }
    }

    #region IrcColorList

    internal class IrcColorList : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            //True - means show a Combobox
            //and False for show a Modal 
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            //False - a option to edit values 
            //and True - set values to state readonly
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection
                (new[]
                     {
                         "White", "Black", "BlueNavy", "Blue", "Red", "Orange", "Yellow", "Green", "Teal", "Aqua",
                         "DarkBlue",
                         "Pink", "Grey", "Purple", "DarkGrey", "LightGrey"
                     });
        }
    }

    #endregion

    #endregion
}