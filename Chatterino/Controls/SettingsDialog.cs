﻿using Chatterino.Common;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace Chatterino.Controls
{
    public partial class SettingsDialog : Form
    {
        public string OriginalSettings = "";
        private Button btnCancel;
        private Button BtnOK;
        //private Button btnResetCurrent;
        //private Button btnResetAll;
        //CTOR
        public SettingsDialog()
        {
            InitializeComponent();

            TopMost = AppSettings.WindowTopMost;

            AppSettings.WindowTopMostChanged += (s, e) =>
            {
                TopMost = AppSettings.WindowTopMost;
            };

            Icon = App.Icon;

            try
            {
                //this.Icon = Program.AppIcon;
            }
            catch { }

            tabs.SelectedIndex = 0;
            tabs.PageSelected += tabs_PageSelected;
            tabs_PageSelected(this, EventArgs.Empty);

            // Accounts
            #region Accounts
            var originalAccounts = AccountManager.Accounts.ToArray();

            foreach (var account in originalAccounts)
            {
                dataGridViewAccounts.Rows.Add(account.Username);
            }

            dataGridViewAccounts.Sort(dataGridViewAccounts.Columns[0], ListSortDirection.Descending);

            LoginForm loginForm = null;

            buttonAccountAdd.Click += delegate
            {
                if (loginForm == null)
                {
                    loginForm = new LoginForm();

                    loginForm.FormClosed += (s, e) =>
                    {
                        if (loginForm.Account != null)
                        {
                            AccountManager.AddAccount(loginForm.Account);

                            IrcManager.Account = loginForm.Account;
                            IrcManager.Connect();

                            if (!GuiEngine.Current.GlobalBadgesLoaded)
                            {
                                foreach (var channel in TwitchChannel.Channels)
                                {
                                    channel.ReloadEmotes();
                                }
                                GuiEngine.Current.LoadBadges(); 
                            }

                            var username = loginForm.Account.Username.ToLowerInvariant();

                            var addGridViewItem = true;

                            foreach (DataGridViewRow row in dataGridViewAccounts.Rows)
                            {
                                if (((string)row.Cells[0].Value).ToLowerInvariant() == username)
                                {
                                    addGridViewItem = false;
                                    break;
                                }
                            }

                            if (addGridViewItem)
                            {
                                dataGridViewAccounts.Rows.Add(loginForm.Account.Username);
                                dataGridViewAccounts.Sort(dataGridViewAccounts.Columns[0], ListSortDirection.Ascending);
                            }
                        }
                        loginForm = null;
                    };

                    loginForm.Show();
                }
                else
                {
                    loginForm.BringToFront();
                }
            };

            Closed += delegate
            {
                AccountManager.SaveToJson(Path.Combine(Util.GetUserDataPath(), "Login.json"));

                loginForm?.Close();
            };

            buttonAccountRemove.Click += (s, e) =>
            {
                if (dataGridViewAccounts.SelectedCells.Count != 1) { return; }


                var username = (string)dataGridViewAccounts.SelectedCells[0].Value;
                if (MessageBox.Show($"Are you sure you want to remove {username}?", "Remove account", MessageBoxButtons.YesNo) == DialogResult.No) { return; }

                AccountManager.RemoveAccount(username);

                dataGridViewAccounts.Rows.RemoveAt(dataGridViewAccounts.SelectedCells[0].RowIndex);
                buttonAccountRemove.Enabled = dataGridViewAccounts.RowCount != 0;
            };

            buttonAccountRemove.Enabled = dataGridViewAccounts.RowCount != 0;
            #endregion

            // Appearance
            #region Appearance

            Action setNightThemeVisibility = () =>
            {
                comboThemeNight.Visible = labelNightDesc.Visible = comboThemeNight.Visible =
                    labelThemeNight.Visible = labelThemeNightFrom.Visible = labelNightThemeUntil.Visible =
                    numThemeNightFrom.Visible = numThemeNightUntil.Visible =
                    checkBoxDifferentThemeAtNight.Checked;
            };

            var originalTheme = comboTheme.Text = AppSettings.Theme;
            var originalThemeNight = comboThemeNight.Text = AppSettings.NightTheme;
            var originalThemeHue = AppSettings.ThemeHue;
            var originalThemeNightStart = AppSettings.NightThemeStart;
            var originalThemeNightEnd = AppSettings.NightThemeEnd;
            var originalQuality = comboQuality.Text = AppSettings.Quality;
            var originalPath = AppSettings.StreamlinkPath;
            int originalMsgLimit = AppSettings.ChatMessageLimit;

            BindCheckBox(checkBoxDifferentThemeAtNight, "EnableNightTheme");

            checkBoxDifferentThemeAtNight.CheckedChanged += (s, e) =>
            {
                AppSettings.UpdateCurrentTheme();
                setNightThemeVisibility();
            };

            comboTheme.SelectedValueChanged += (s, e) =>
            {
                AppSettings.Theme = comboTheme.Text;
            };

            comboThemeNight.SelectedValueChanged += (s, e) =>
            {
                AppSettings.NightTheme = comboThemeNight.Text;
            };

            comboQuality.SelectedValueChanged += (s, e) =>
            {
                AppSettings.Quality = comboQuality.Text;
            };

            numThemeNightFrom.Value = Math.Max(1, Math.Min(24, AppSettings.NightThemeStart));

            numThemeNightFrom.ValueChanged += (s, e) =>
            {
                AppSettings.NightThemeStart = (int)numThemeNightFrom.Value;
            };

            numThemeNightUntil.Value = Math.Max(1, Math.Min(24, AppSettings.NightThemeEnd));

            numThemeNightUntil.ValueChanged += (s, e) =>
            {
                AppSettings.NightThemeEnd = (int)numThemeNightUntil.Value;
            };

            setNightThemeVisibility();

            onCancel += (s, e) =>
            {
                AppSettings.Theme = originalTheme;
                AppSettings.NightTheme = originalThemeNight;
                AppSettings.ThemeHue = originalThemeHue;
                AppSettings.NightThemeStart = originalThemeNightStart;
                AppSettings.NightThemeEnd = originalThemeNightEnd;
                AppSettings.Quality = originalQuality;
                AppSettings.ChatMessageLimit = originalMsgLimit;
                App.MainForm.Refresh();
            };


            trackBar1.Value = Math.Max(Math.Min((int)Math.Round(originalThemeHue * 360), 360), 0);

            trackBar1.ValueChanged += (s, e) =>
            {
                AppSettings.ThemeHue = trackBar1.Value / 360.0;
                App.MainForm.Refresh();
            };

            var defaultScrollSpeed = AppSettings.ScrollMultiplyer;

            trackBar2.Value = Math.Min(400, Math.Max(100, (int)(AppSettings.ScrollMultiplyer * 200)));

            onCancel += (s, e) =>
            {
                AppSettings.ScrollMultiplyer = defaultScrollSpeed;
            };

            lblScrollSpeed.Text = (int)(AppSettings.ScrollMultiplyer * 100) + "%";

            trackBar2.ValueChanged += (s, e) =>
            {
                AppSettings.ScrollMultiplyer = (double)trackBar2.Value / 200;

                lblScrollSpeed.Text = (int)(AppSettings.ScrollMultiplyer * 100) + "%";
            };

            btnSelectFont.Click += (s, e) =>
            {
                using (var dialog = new CustomFontDialog.FontDialog())
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        AppSettings.SetFont(dialog.Font.Name, dialog.Font.Size);
                    }
                }

                updateFontName();
            };
            
            btnDefaultFont.Click += (s, e) =>
            {
                int fontsize = 10;
                using (InputDialogForm dialog = new InputDialogForm("Select Font size") { Value = "" }) {
                    DialogResult res = dialog.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        try {
                            fontsize = Int32.Parse(dialog.Value);
                        } catch {
                            fontsize = 10;
                        }
                        AppSettings.SetFont("Helvetica Neue", fontsize);
                        updateFontName();
                    }
                }
            };

            BindCheckBox(chkTimestamps, "ChatShowTimestamps");
            BindCheckBox(chkTimestampSeconds, "ChatShowTimestampSeconds");
            BindCheckBox(chkTimestampAmPm, "TimestampsAmPm");
            BindCheckBox(chkAllowSameMessages, "ChatAllowSameMessage");
            BindCheckBox(chkDoubleClickLinks, "ChatLinksDoubleClickOnly");
            BindCheckBox(chkHideInput, "ChatHideInputIfEmpty");
            BindCheckBox(chkRainbow, "Rainbow");
            BindCheckBox(chkMessageSeparators, "ChatSeparateMessages");
            BindCheckBox(chkStreamlinkPath, "EnableStreamlinkPath");
            BindCheckBox(chkPreferEmotes, "PreferEmotesOverUsernames");

            chkMessageSeparators.CheckedChanged += (s, e) =>
            {
                App.MainForm.Refresh();
            };

            chkRainbow.CheckedChanged += (s, e) =>
            {
                if (!AppSettings.Rainbow)
                {
                    (App.MainForm.Selected as ChatControl)?.Channel.SendMessage($"/color {AppSettings.OldColor}");
                }
            };

            txtMsgLimit.Text = AppSettings.ChatMessageLimit.ToString();
            onSave += (s, e) =>
            {
                AppSettings.ChatMessageLimit = int.Parse(txtMsgLimit.Text);
            };

            BindCheckBox(chkInputShowMessageLength, "ChatInputShowMessageLength");

            BindCheckBox(chkMentionUserWithAt, "ChatMentionUsersWithAt");

            BindCheckBox(chkTabLocalizedNames, "ChatTabLocalizedNames");
            BindCheckBox(chkTopMost, "WindowTopMost");
            BindCheckBox(chkRemoveX, "RemoveXButton");
            BindCheckBox(chkRecentlyUsedEmoteList, "RecentlyUsedEmoteList");
            BindCheckBox(chkHighlightHighlightedMessages, "HighlightHighlightedMessages");
            BindCheckBox(chkChangeTabTitle, "ChangeTabTitle");
            BindCheckBox(chkCacheEmotes, "CacheEmotes");
            BindCheckBox(chkAutomaticUpdate, "CheckUpdates");
            BindCheckBox(chkSingleConnection, "UseSingleConnection");
            BindCheckBox(chkReplys, "EnableReplys");
            BindCheckBox(chkShowEmoteTooltip, "ShowEmoteTooltip");
            BindCheckBox(chkIgnoreViaTwitch, "IgnoreViaTwitch");

            BindCheckBox(chkLastReadMessageIndicator, "ChatShowLastReadMessageIndicator");
            chkLastReadMessageIndicator.CheckedChanged += (s, e) =>
            {
                App.MainForm.Refresh();
            };
            chkSingleConnection.CheckedChanged += (s, e) => {
                IrcManager.Connect();
            };

            txtStreamlinkCustomArguments.Text = AppSettings.CustomStreamlinkArguments;

            txtStreamlinkCustomArguments.TextChanged += (s, e) =>
            {
                AppSettings.CustomStreamlinkArguments = txtStreamlinkCustomArguments.Text;
            };

            var originalCustomStreamlinkArguments = AppSettings.CustomStreamlinkArguments;

            onCancel += (s, e) =>
            {
                AppSettings.CustomStreamlinkArguments = originalCustomStreamlinkArguments;
            };
            #endregion

            // Commands
            #region Commands
            lock (Commands.CustomCommandsLock)
            {
                foreach (var c in Commands.CustomCommands)
                {
                    dgvCommands.Rows.Add(c.Raw);
                }
            }

            //ChatAllowCommandsAtEnd
            var defaultAllowCommandAtEnd = AppSettings.ChatAllowCommandsAtEnd;

            chkAllowCommandAtEnd.Checked = AppSettings.ChatAllowCommandsAtEnd;

            chkAllowCommandAtEnd.CheckedChanged += (s, e) =>
            {
                AppSettings.ChatAllowCommandsAtEnd = chkAllowCommandAtEnd.Checked;
            };

            onCancel += (s, e) =>
            {
                AppSettings.ChatAllowCommandsAtEnd = defaultAllowCommandAtEnd;
            };

            dgvCommands.MultiSelect = false;
            dgvCommands.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvCommands.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            dgvCommands.KeyDown += (s, e) =>
            {
                if (e.KeyData == (Keys.Control | Keys.Back))
                {
                    e.Handled = true;
                }
            };

            btnCommandAdd.Click += (s, e) =>
            {
                dgvCommands.Rows.Add();
                dgvCommands.Rows[dgvCommands.Rows.Count - 1].Selected = true;
            };

            Action updateCustomCommands = () =>
            {
                lock (Commands.CustomCommandsLock)
                {
                    Commands.CustomCommands.Clear();

                    foreach (DataGridViewRow row in dgvCommands.Rows)
                    {
                        Commands.CustomCommands.Add(new Command((string)row.Cells[0].Value));
                    }
                }
            };

            btnCommandRemove.Click += (s, e) =>
            {
                if (dgvCommands.SelectedCells.Count != 0)
                {
                    dgvCommands.Rows.RemoveAt(dgvCommands.SelectedCells[0].RowIndex);
                }

                updateCustomCommands();
            };

            var originalCustomCommand = Commands.CustomCommands;

            lock (Commands.CustomCommandsLock)
            {
                Commands.CustomCommands = new List<Command>(Commands.CustomCommands);
            }

            dgvCommands.CellValueChanged += (s, e) =>
            {
                updateCustomCommands();
            };

            onCancel += (s, e) =>
            {
                lock (Commands.CustomCommandsLock)
                {
                    Commands.CustomCommands = originalCustomCommand;
                }
            };
            #endregion

            // Emotes
            #region emotes
            BindCheckBox(chkTwitchEmotes, "ChatEnableTwitchEmotes");
            BindCheckBox(chkBttvEmotes, "ChatEnableBttvEmotes");
            BindCheckBox(chkFFzEmotes, "ChatEnableFfzEmotes");
            BindCheckBox(chk7tvEmotes, "ChatEnable7tvEmotes");
            BindCheckBox(chkEmojis, "ChatEnableEmojis");
            BindCheckBox(chkGifEmotes, "ChatEnableGifAnimations");

            var originalIgnoredEmotes = rtbIngoredEmotes.Text = string.Join(Environment.NewLine, AppSettings.ChatIgnoredEmotes.Keys);

            rtbIngoredEmotes.LostFocus += (s, e) =>
            {
                AppSettings.ChatIgnoredEmotes.Clear();
                var reader = new StringReader(rtbIngoredEmotes.Text);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    AppSettings.ChatIgnoredEmotes[line.Trim()] = null;
                }
            };

            onCancel += (s, e) =>
            {
                AppSettings.ChatIgnoredEmotes.Clear();
                var reader = new StringReader(originalIgnoredEmotes);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    AppSettings.ChatIgnoredEmotes[line.Trim()] = null;
                }
            };

            BindCheckBox(checkBoxEmoteSizeBasedOnTextHeight, "EmoteScaleByLineHeight");

            var originialEmoteScale = AppSettings.EmoteScale;

            onCancel += (s, e) =>
            {
                if (AppSettings.EmoteScale != originialEmoteScale) {
                    AppSettings.EmoteScaleChanged = true;
                }
                AppSettings.EmoteScale = originialEmoteScale;
                App.TriggerEmoteLoaded();
            };

            checkBoxEmoteSizeBasedOnTextHeight.CheckedChanged += (s, e) =>
            {
                labelEmoteScale.Text = $"Emote scale: {AppSettings.EmoteScale:0.##}x";
                if (AppSettings.EmoteScale != originialEmoteScale) {
                    AppSettings.EmoteScaleChanged = true;
                }
                App.TriggerEmoteLoaded();
            };

            trackBarEmoteScale.Value = Math.Min(trackBarEmoteScale.Maximum,
                Math.Max(trackBarEmoteScale.Minimum, (int)Math.Round((AppSettings.EmoteScale - 0.5) * 10)));

            trackBarEmoteScale.ValueChanged += (s, e) =>
            {
                AppSettings.EmoteScale = trackBarEmoteScale.Value / 10.0 + 0.5;
                labelEmoteScale.Text = $"Emote scale: {AppSettings.EmoteScale:0.##}x";
                if (AppSettings.EmoteScale != originialEmoteScale) {
                    AppSettings.EmoteScaleChanged = true;
                }
                App.TriggerEmoteLoaded();
            };

            labelEmoteScale.Text = $"Emote scale: {AppSettings.EmoteScale:0.##}x";
            #endregion

            // Ignored Users
            #region ignored users
            //BindCheckBox(chkTwitchIgnores, "EnableTwitchUserIgnores");

            foreach (var user in IrcManager.IgnoredUsers)
            {
                dgvIgnoredUsers.Rows.Add(user);
            }

            switch (AppSettings.ChatShowIgnoredUsersMessages)
            {
                case 1:
                    comboShowIgnoredUsersMessagesIf.Text = "You are moderator";
                    break;
                case 2:
                    comboShowIgnoredUsersMessagesIf.Text = "You are broadcaster";
                    break;
                default:
                    comboShowIgnoredUsersMessagesIf.Text = "Never";
                    break;
            }

            comboShowIgnoredUsersMessagesIf.SelectedIndexChanged += (s, e) =>
            {
                if (comboShowIgnoredUsersMessagesIf.Text.Contains("moderator"))
                {
                    AppSettings.ChatShowIgnoredUsersMessages = 1;
                }
                else if (comboShowIgnoredUsersMessagesIf.Text.Contains("broadcaster"))
                {
                    AppSettings.ChatShowIgnoredUsersMessages = 2;
                }
                else
                {
                    AppSettings.ChatShowIgnoredUsersMessages = 0;
                }
            };

            dgvIgnoredUsers.MultiSelect = false;
            dgvIgnoredUsers.ReadOnly = true;
            dgvIgnoredUsers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvIgnoredUsers.Columns[0].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            btnIgnoredUserAdd.Click += (s, e) =>
            {
                using (var dialog = new InputDialogForm("Input Username"))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string message;

                        if (IrcManager.TryAddIgnoredUser(dialog.Value.Trim(), null, out message))
                        {
                            dgvIgnoredUsers.Rows.Add(dialog.Value.Trim());
                        }
                        else
                        {
                            MessageBox.Show(message, "Error while ignoring user");
                        }
                    }
                }
            };

            btnIgnoredUserRemove.Click += (s, e) =>
            {
                if (dgvIgnoredUsers.SelectedCells.Count != 0)
                {
                    string message;
                    var username = (string)dgvIgnoredUsers.SelectedCells[0].Value;

                    if (IrcManager.TryRemoveIgnoredUser(username, null, out message))
                    {
                        dgvIgnoredUsers.Rows.RemoveAt(dgvIgnoredUsers.SelectedCells[0].RowIndex);
                    }
                    else
                    {
                        MessageBox.Show(message, "Error while unignoring user");
                    }
                }
            };
            #endregion

            // Ignored Messages
            #region ignored messages
            var ignoreKeywordsOriginal = rtbIgnoreKeywords.Text = string.Join(Environment.NewLine, AppSettings.ChatIgnoredKeywords);

            rtbIgnoreKeywords.LostFocus += (s, e) =>
            {
                var list = new List<string>();
                var reader = new StringReader(rtbIgnoreKeywords.Text);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    list.Add(line.Trim());
                }
                AppSettings.ChatIgnoredKeywords = list.ToArray();
            };

            onCancel += (s, e) =>
            {
                // highlight keywords
                {
                    var list = new List<string>();
                    var reader = new StringReader(ignoreKeywordsOriginal);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        list.Add(line.Trim());
                    }
                    AppSettings.ChatIgnoredKeywords = list.ToArray();
                }
            };
            #endregion

            // Proxy
            BindCheckBox(chkProxyEnabled, "ProxyEnable");
            BindTextBox(textBox1, "ProxyHost");
            BindTextBox(textBox4, "ProxyPort");
            BindTextBox(textBox2, "ProxyUsername");
            BindTextBox(textBox3, "ProxyPassword");

            // Highlights
            #region highlights
            var customHighlightsOriginal = rtbHighlights.Text = string.Join(Environment.NewLine, AppSettings.ChatCustomHighlights);
            var highlightIgnoredUsersOriginal = rtbUserBlacklist.Text = string.Join(Environment.NewLine, AppSettings.HighlightIgnoredUsers.Keys);
            var usernameHighlightOriginal = rtbUsernamelist.Text = string.Join(Environment.NewLine, AppSettings.HighlightUserNames.Keys);

            rtbUsernamelist.LostFocus += (s, e) =>
            {
                AppSettings.HighlightUserNames.Clear();
                var reader = new StringReader(rtbUsernamelist.Text);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    AppSettings.HighlightUserNames[line.Trim().ToLower()] = null;
                }
            };

            rtbHighlights.LostFocus += (s, e) =>
            {
                var list = new List<string>();
                var reader = new StringReader(rtbHighlights.Text);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    list.Add(line.Trim());
                }
                AppSettings.ChatCustomHighlights = list.ToArray();
            };

            rtbUserBlacklist.LostFocus += (s, e) =>
            {
                AppSettings.HighlightIgnoredUsers.Clear();
                var reader = new StringReader(rtbUserBlacklist.Text);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    AppSettings.HighlightIgnoredUsers[line.Trim().ToLower()] = null;
                }
            };

            onCancel += (s, e) =>
            {
                // highlight keywords
                {
                    var list = new List<string>();
                    var reader = new StringReader(customHighlightsOriginal);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        list.Add(line.Trim());
                    }
                    AppSettings.ChatCustomHighlights = list.ToArray();
                }

                // user blacklist
                {
                    AppSettings.HighlightIgnoredUsers.Clear();
                    var reader = new StringReader(highlightIgnoredUsersOriginal);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        AppSettings.HighlightIgnoredUsers[line.Trim().ToLower()] = null;
                    }
                }

                // username list
                {
                    AppSettings.HighlightUserNames.Clear();
                    var reader = new StringReader(usernameHighlightOriginal);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        AppSettings.HighlightUserNames[line.Trim().ToLower()] = null;
                    }
                }
            };

            updateFontName();

            btnCustomHighlightOpenFile.Click += (s, e) =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "wave sound file|*.wav";

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            (GuiEngine.Current as WinformsGuiEngine).HighlightSound?.Reload();

                            if (!Directory.Exists(Path.Combine(Util.GetUserDataPath(), "Custom")))
                                Directory.CreateDirectory(Path.Combine(Util.GetUserDataPath(), "Custom"));

                            File.Copy(dialog.FileName, Path.Combine(Util.GetUserDataPath(), "Custom", "Ping.wav"), true);
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(exc.Message, "Error copying the highlight sound");
                        }
                    }
                }
            };
            
            btnTextCustomPing.Click += (s, e) =>
            {
                GuiEngine.Current.PlaySound(NotificationSound.Ping, true);
            };
            
            BindCheckBox(chkHighlight, "ChatEnableHighlight");
            BindCheckBox(chkPings, "ChatEnableHighlightSound");
            BindCheckBox(chkFlashTaskbar, "ChatEnableHighlightTaskbar");
            BindCheckBox(chkCustomPingSound, "ChatCustomHighlightSound");
            
            BindCheckBox(chkGoLiveSound, "ChatEnableGoLiveSound");
            BindCheckBox(chkGoLiveTaskbar, "ChatEnableGoLiveTaskbar");
            BindCheckBox(chkCustomGoLiveSound, "ChatCustomGoLiveSound");
            
            btnCustomGoLiveOpenFile.Click += (s, e) =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "wave sound file|*.wav";

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            (GuiEngine.Current as WinformsGuiEngine).GoLiveSound.SetPath(dialog.FileName);
                            (GuiEngine.Current as WinformsGuiEngine).GoLiveSound.Reload();
                            AppSettings.ChatCustomGoLiveSoundPath = dialog.FileName;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(exc.Message, "Error copying the Go Live sound");
                        }
                    }
                }
            };
            
            btnTestGoLiveCustomPing.Click += (s, e) =>
            {
                GuiEngine.Current.PlaySound(NotificationSound.GoLive, true);
            };
            
            #endregion

            // Moderation
            BindCheckBox(chkBanButton, "EnableBanButton");
            BindCheckBox(chkDeleteButton, "EnableDeleteButton");
            BindCheckBox(chkTimeoutButton, "EnableTimeoutButton");

            var originalTimeoutButtons = new List<int>(AppSettings.TimeoutButtons);

            Action updateTimeoutButtons = () =>
            {
                List<int> values = new List<int>();
                foreach (var control in listView1.Controls)
                {
                    values.Add((control as TimespanSelectControl).GetValue());
                }
                AppSettings.TimeoutButtons = values;
            };

            foreach (var i in AppSettings.TimeoutButtons)
            {
                var c = new TimespanSelectControl(i);
                c.ValueChanged += (s, e) => updateTimeoutButtons();
                listView1.Controls.Add(c);
            }

            onCancel += (s, e) =>
            {
                AppSettings.TimeoutButtons = originalTimeoutButtons;
            };

            listView1.ControlRemoved += (s, e) => updateTimeoutButtons();
            listView1.ControlAdded += (s, e) => updateTimeoutButtons();

            btnAddTimeout.Click += (s, e) =>
            {
                listView1.Controls.Add(new TimespanSelectControl());
            };

            // Whispers
            BindCheckBox(chkEnableInlineWhispers, "ChatEnableInlineWhispers");

            //Buttons
            var x = 0;

            ///Cancel
            btnCancel = new Button();
            btnCancel.AutoSize = true;
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(tabs.Panel.Width - 12 - btnCancel.Width - x, tabs.Panel.Height - 12 - btnCancel.Height);
            btnCancel.Anchor = (AnchorStyles.Right | AnchorStyles.Bottom);
            btnCancel.BackColor = Color.FromArgb(0);
            btnCancel.Click += new EventHandler(btnCancel_Click);
            tabs.Panel.Controls.Add(btnCancel);
            x += 12 + btnCancel.Width;

            ///OK
            BtnOK = new Button();
            BtnOK.AutoSize = true;
            BtnOK.Text = "Apply";
            BtnOK.Location = new Point(tabs.Panel.Width - 12 - BtnOK.Width - x, tabs.Panel.Height - 12 - btnCancel.Height);
            BtnOK.Anchor = (AnchorStyles.Right | AnchorStyles.Bottom);
            BtnOK.BackColor = Color.FromArgb(0);
            BtnOK.Click += new EventHandler(btnOK_Click);
            tabs.Panel.Controls.Add(BtnOK);
            x += 12 + BtnOK.Width;

            Closed += (s, e) =>
            {
                AppSettings.Save(null);
                Commands.Save(Path.Combine(Util.GetUserDataPath(), "Custom", "Commands.txt"));
            };
        }

        event EventHandler onSave;
        event EventHandler onCancel;

        void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            onSave?.Invoke(this, EventArgs.Empty);
            Close();
            foreach(TabPage tp in App.MainForm.TabControl.TabPages)
            {
                tp.Reload();
            }
            Emotes.EmoteAdded();
            
        }

        void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            onCancel?.Invoke(this, EventArgs.Empty);
            Close();
        }

        //SHOW
        public void Show(string key)
        {
            base.Show();

            //OriginalSettings = Options.Settings.GetRawData();

            for (var i = 1; i < tabs.Controls.Count; i++)
            {
                if (tabs.Controls[i].Name == key)
                {
                    tabs.SelectedIndex = i - 1;
                }
            }
        }

        //BIND
        Dictionary<Control, KeyValuePair<string, object>> bindings = new Dictionary<Control, KeyValuePair<string, object>>();
        Dictionary<PropertyInfo, object> originalValue = new Dictionary<PropertyInfo, object>();

        private void BindCheckBox(CheckBox c, string name)
        {
            bool original;

            PropertyInfo prop;
            if (AppSettings.Properties.TryGetValue(name, out prop))
            {
                original = (bool)prop.GetValue(null);
                c.Checked = original;
            }
            else
                throw new ArgumentException($"The settings {name} doesn't exist.");

            c.CheckedChanged += (s, e) =>
            {
                prop.SetValue(null, c.Checked);
            };

            onCancel += (s, e) =>
            {
                prop.SetValue(null, original);
            };
        }

        private void BindTextBox(TextBox c, string name)
        {
            PropertyInfo prop;
            bool isNumeric;

            object original;

            if (AppSettings.Properties.TryGetValue(name, out prop))
            {
                isNumeric = prop.PropertyType == typeof(int);

                original = prop.GetValue(null);
                c.Text = original.ToString(); ;
            }
            else
            {
                throw new ArgumentException($"The settings {name} doesn't exist.");
            }

            c.TextChanged += (s, e) =>
            {
                if (isNumeric)
                {
                    try
                    {
                        prop.SetValue(null, int.Parse(c.Text));
                    }
                    catch { }
                }
                else
                {
                    prop.SetValue(null, c.Text);
                }
            };

            onCancel += (s, e) =>
            {
                prop.SetValue(null, original);
            };

            if (isNumeric)
            {
                c.TextChanged += (s, e) =>
                {
                    c.Text = Regex.Replace(c.Text, "[^0-9]+", "");
                };
            }
        }

        private void tabs_PageSelected(object sender, EventArgs e)
        {
            Text = "Preferences - " + tabs.SelectedTab.Text;
        }

        private void updateFontName()
        {
            lblFont.Text = $"{Fonts.GetFont(FontType.Medium).Name}, {Fonts.GetFont(FontType.Medium).Size}";
        }

        private void btnStreamlinkPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog streamlink = new OpenFileDialog();
            streamlink.InitialDirectory = "C:\\";
            streamlink.Filter = "Streamlink binary (streamlink.exe)|*.exe";
            streamlink.FilterIndex = 1;
            streamlink.RestoreDirectory = true;
            if (streamlink.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    AppSettings.StreamlinkPath = streamlink.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void linkStreamlinkWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://streamlink.github.io/");
            }
            catch { }
        }

        private void linkStreamlinkDownload_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://github.com/streamlink/streamlink/releases");
            }
            catch { }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {

        }
    }
}
