﻿using Chatterino.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Web.SessionState;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Chatterino.Controls;


namespace Chatterino
{
    public static class App
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern bool SetWindowPos(int hWnd,int hWndInsertAfter,int X,int Y,int cx,int cy,uint uFlags);
        // Updates
        public static VersionNumber CurrentVersion { get; private set; }
        private static bool installUpdatesOnExit = false;
        private static bool restartAfterUpdates = false;

        public static bool CanShowChangelogs { get; set; } = true;
        public static string UpdaterPath { get; private set; } = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName, "Updater", "Chatterino.Updater.exe");

        // Drawing
        public static bool UseDirectX { get; private set; } = false;

        public const TextFormatFlags DefaultTextFormatFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        public static event EventHandler GifEmoteFramesUpdating;
        public static event EventHandler GifEmoteFramesUpdated;

        // Color Scheme
        public static event EventHandler ColorSchemeChanged;

        private static ColorScheme colorScheme;

        public static ColorScheme ColorScheme
        {
            get { return colorScheme; }
            set
            {
                if (colorScheme != value)
                {
                    colorScheme = value;
                    ColorSchemeChanged?.Invoke(null, null);
                }
            }
        }

        // Window
        public static MainForm MainForm { get; set; }

        public static Icon Icon { get; private set; }

        public static Controls.SettingsDialog SettingsDialog { get; set; }

        static Controls.ToolTip ToolTip { get; set; } = null;
        public static Controls.EmoteListPopup EmoteList { get; set; } = null;

        private static bool windowFocused = true;

        public static bool WindowFocused
        {
            get { return windowFocused; }
            set
            {
                windowFocused = value;
                Common.Message.EnablePings = !value;
            }
        }

        // Emotes
        public static event EventHandler EmoteLoaded;

        [System.Runtime.InteropServices.DllImport("shcore.dll")]
        static extern int SetProcessDpiAwareness(_Process_DPI_Awareness value);

        enum _Process_DPI_Awareness
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }

        // Main Entry Point
        [STAThread]
        static void Main()
        {
            CurrentVersion = VersionNumber.Parse(
                    AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version.ToString());

            Directory.SetCurrentDirectory(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName);
            if (!File.Exists("./removeupdatenew") && Directory.Exists("./Updater.new"))
            {
                string path = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName,
                    "Updater.new", "Chatterino.Updater.exe");
                if (Directory.Exists(path)) {
                    UpdaterPath = path;
                }
            }
            else if (File.Exists("./update2")) {
                string path = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName,
                        "Updater2", "Chatterino.Updater.exe");
                if (File.Exists(path)) {
                    UpdaterPath = path;
                }
            }
            GuiEngine.Initialize(new WinformsGuiEngine());
            GuiEngine.Current.log("update path " + UpdaterPath + " " + File.Exists("./update2") + " " + 
            Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName,"Updater2", "Chatterino.Updater.exe") + " " + 
            File.Exists(Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName,"Updater2", "Chatterino.Updater.exe")));
            ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            //ServicePointManager.UseNagleAlgorithm = false;
            //ServicePointManager.MaxServicePoints = 10000;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //SetProcessDpiAwareness(_Process_DPI_Awareness.Process_Per_Monitor_DPI_Aware);

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Fonts
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 1)
            {
                UseDirectX = true;
            }

            // Exceptions
            Application.ThreadException += (s, e) =>
            {
                e.Exception.Log("exception", "{0}\n");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                (e.ExceptionObject as Exception).Log("exception", "{0}\n");
            };
            EmoteCache.init();
            // Update gif emotes
            int offset = 0;
            new System.Windows.Forms.Timer { Interval = 30, Enabled = true }.Tick += (s, e) =>
                {
                    if (AppSettings.ChatEnableGifAnimations)
                    {
                        lock (GuiEngine.Current.GifEmotesLock)
                        {
                            offset += 3;
                            if (offset<0) {
                                offset=0;
                            }
                            try {
                                if (EmoteList!=null && EmoteList.GetGifEmotes()!=null) {
                                    var gifemotes = EmoteList.GetGifEmotes();
                                    lock (gifemotes) {
                                        GuiEngine.Current.GifEmotesOnScreen.UnionWith(gifemotes);
                                    }
                                }
                                foreach (LazyLoadedImage emote in GuiEngine.Current.GifEmotesOnScreen) {
                                    if (emote.HandleAnimation != null) {
                                        emote.HandleAnimation(offset);
                                    }
                                }
                                if (ToolTip != null && ToolTip.Visible && ToolTip.Image != null && ToolTip.Image.HandleAnimation != null) {
                                    ToolTip.Image.HandleAnimation(offset);
                                    lock (ToolTip) {
                                        ToolTip.redraw();
                                    }
                                }
                            } catch (Exception err) {
                                GuiEngine.Current.log("error updating gifs "+ err.ToString());
                            }
                        }
                        GifEmoteFramesUpdated?.Invoke(null, EventArgs.Empty);
                    }
                };

            // Settings/Colors
            try
            {
                if (!Directory.Exists(Util.GetUserDataPath()))
                {
                    Directory.CreateDirectory(Util.GetUserDataPath());
                }

                if (!Directory.Exists(Path.Combine(Util.GetUserDataPath(), "Custom")))
                {
                    Directory.CreateDirectory(Path.Combine(Util.GetUserDataPath(), "Custom"));
                }
            }
            catch
            {

            }

            AppSettings.SavePath = Path.Combine(Util.GetUserDataPath(), "Settings.ini");

            var showWelcomeForm = false;

            try
            {
                if (!File.Exists(AppSettings.SavePath))
                {
                    CanShowChangelogs = false;

                    showWelcomeForm = true;

                    if (File.Exists("./Settings.ini") && !File.Exists(AppSettings.SavePath))
                    {
                        File.Move("./Settings.ini", AppSettings.SavePath);

                        try

                        {
                            File.Delete("./Settings.ini");
                        }
                        catch { }
                    }

                    if (File.Exists("./Custom/Commands.txt") &&
                        !File.Exists(Path.Combine(Util.GetUserDataPath(), "Custom", "Commands.txt")))
                    {
                        File.Move("./Custom/Commands.txt", Path.Combine(Util.GetUserDataPath(), "Custom", "Commands.txt"));

                        try
                        {
                            File.Delete("./Custom/Commands.txt");
                        }
                        catch { }
                    }

                    if (File.Exists("./Custom/Ping.wav") &&
                        !File.Exists(Path.Combine(Util.GetUserDataPath(), "Custom", "Ping.wav")))
                    {
                        File.Move("./Custom/Ping.wav", Path.Combine(Util.GetUserDataPath(), "Custom", "Ping.wav"));

                        try
                        {
                            File.Delete("./Custom/Ping.wav");
                        }
                        catch { }
                    }

                    if (File.Exists("./Layout.xml") &&
                        !File.Exists(Path.Combine(Util.GetUserDataPath(), "Layout.xml")))
                    {
                        File.Move("./Layout.xml", Path.Combine(Util.GetUserDataPath(), "Layout.xml"));

                        try
                        {
                            File.Delete("./Layout.xml");
                        }
                        catch { }
                    }
                }
            }
            catch
            {

            }

            AppSettings.Load(null);

            AccountManager.LoadFromJson(Path.Combine(Util.GetUserDataPath(), "Login.json"));

            IrcManager.Account = AccountManager.FromUsername(AppSettings.SelectedUser) ?? Account.AnonAccount;
            IrcManager.Connect();

            Commands.LoadOrDefault(Path.Combine(Util.GetUserDataPath(), "Custom", "Commands.txt"));
            Cache.Load();

            _updateTheme();
            

            AppSettings.ThemeChanged += (s, e) => _updateTheme();

            Updates.UpdateFound += (s, e) =>
            {
                try
                {
                    using (var dialog = new UpdateDialog(e.PatchNotes, e.Version.ToString()))
                    {
                        if (File.Exists(UpdaterPath))
                        {
                            var result = dialog.ShowDialog();

                            // OK -> install now
                            // Yes -> install on exit
                            // Ignore -> skip this update
                            if (result == DialogResult.OK || result == DialogResult.Yes)
                            {
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(e.Url, Path.Combine(Util.GetUserDataPath(), "update.zip"));
                                }

                                installUpdatesOnExit = true;

                                if (result == DialogResult.OK)
                                {
                                    restartAfterUpdates = true;
                                    MainForm?.Close();
                                }
                            } else if (result == DialogResult.Ignore) {
                                AppSettings.SkipVersionNumber = e.Version.ToString();
                            }
                        }
                        else
                        {
                            MessageBox.Show("An update is available but the update executable could not be found. If you want to update chatterino you will have to do it manually.");
                        }
                    }
                }
                catch { }
            };

#if !DEBUG
            if (AppSettings.CheckUpdates) {
                if (!String.IsNullOrEmpty(AppSettings.SkipVersionNumber)) {
                    VersionNumber SkipVersion = VersionNumber.Parse(AppSettings.SkipVersionNumber);
                    Updates.CheckForUpdate(SkipVersion);
                } else {
                    Updates.CheckForUpdate(CurrentVersion);
                }
            }
#endif

            // Start irc
            Emotes.LoadGlobalEmotes();
            Badges.LoadGlobalBadges();
            Emojis.LoadEmojis();
            GuiEngine.Current.LoadBadges();
            GuiEngine.Current.UpdateSoundPaths();
            Net.StartHttpServer();

            // Show form
            MainForm = new MainForm();

            MainForm.Show();

            if (showWelcomeForm)
            {
                new WelcomeForm().Show();
            }

            MainForm.FormClosed += (s, e) =>
            {
                Application.Exit();

                Cache.Save();
                
                EmoteCache.SaveEmoteList();
                
                // Install updates
                if (installUpdatesOnExit)
                {
                    try {
                        Process.Start(UpdaterPath, restartAfterUpdates ? "--restart" : "");
                    } catch (Exception ex) {
                        MessageBox.Show($"Failed to install update. You could try running the updater manually at {UpdaterPath}.");
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            };

            Application.Run();
            Environment.Exit(0);
        }

        // Public Functions
        public static void TriggerEmoteLoaded()
        {
            EmoteLoaded?.Invoke(null, EventArgs.Empty);
        }

        public static void ShowSettings()
        {
            if (SettingsDialog == null)
            {
                SettingsDialog = new Controls.SettingsDialog
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(MainForm.Location.X + 32, MainForm.Location.Y + 64)
                };

                SettingsDialog.Show(MainForm);
                SettingsDialog.FormClosing += (s, e) =>
                {
                    SettingsDialog = null;
                };
            }
            else
            {
                SettingsDialog.Focus();
            }
        }

        private static Point calcTooltipLocation(Point point) {
            Point ret;
            Screen.FromPoint(point);

            var screen = Screen.FromPoint(Cursor.Position);
            
            int x = point.X, y = point.Y;

            if (point.X < screen.WorkingArea.X)
            {
                x = screen.WorkingArea.X;
            }
            else if (point.X + ToolTip.Width > screen.WorkingArea.Right)
            {
                x = screen.WorkingArea.Right - ToolTip.Width;
            }

            if (point.Y < screen.WorkingArea.Y)
            {
                y = screen.WorkingArea.Y;
            }
            else if (point.Y + ToolTip.Height > screen.WorkingArea.Bottom)
            {
                y = y - 24 - ToolTip.Height;
            }

            ret = new Point(x, y);
            
            return ret;
        }
    
        private static object tooltiplock = new object();


        
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;

        public static void ShowToolTip(Point point, string text, string imgurl, LazyLoadedImage tooltipimage, bool force = false)
        {
            //if (force || WindowFocused || (EmoteList?.ContainsFocus ?? false))
            
            try
            {
                lock (tooltiplock) {
                    if (ToolTip == null)
                    {
                        ToolTip = new Controls.ToolTip() { Enabled = false };
                    }
                }

                lock (ToolTip) {
                    ToolTip.TooltipText = text;
                }
                if (AppSettings.ShowEmoteTooltip && !String.IsNullOrEmpty(imgurl) && (ToolTip.Image == null || !ToolTip.Image.Url.Equals(imgurl))) {
                    LazyLoadedImage img;
                    if (tooltipimage != null) {
                        img = tooltipimage;
                    } else {
                        img = new LazyLoadedImage();
                        img.Url = imgurl;
                    }
                    img.ImageLoaded += (s, e) => {
                        lock (ToolTip) {
                            point = calcTooltipLocation(point);
                            if (ToolTip.Location != point)
                            {
                                ToolTip.Location = point;
                            }
                            ToolTip.redraw();
                        }
                    };
                    lock (ToolTip) {
                        ToolTip.Image = img;
                    }
                }  else if (String.IsNullOrEmpty(imgurl)) {
                    lock (ToolTip) {
                        ToolTip.Image = null;
                    }
                }
                lock (ToolTip) {
                    if (!ToolTip.Visible)
                    {
                        ToolTip.Show();
                        SetWindowPos(ToolTip.Handle.ToInt32(), HWND_TOPMOST,
                            ToolTip.Left, ToolTip.Top, ToolTip.Width, ToolTip.Height,
                            SWP_NOACTIVATE);
                    }

                    point = calcTooltipLocation(point);

                    if (ToolTip.Location != point)
                    {
                        ToolTip.Location = point;
                    }
                }
            } catch (Exception e) {
                GuiEngine.Current.log("exception loading tooltip " + e.ToString());
            }
        }

        public static void HideToolTip()
        {
            if (ToolTip != null)
            {
                try {
                    lock (ToolTip) {
                        ToolTip.Hide();
                    }
                } catch (Exception e) {
                    GuiEngine.Current.log("exception hiding tooltip " + e.ToString());
                }
            }
        }

        public static void ShowEmoteList(TwitchChannel channel, bool show_only_channel_emotes)
        {
            if (EmoteList == null)
            {
                EmoteList = new Controls.EmoteListPopup();
            }
            
            EmoteList.setShowOnlyChannelEmotes(show_only_channel_emotes);

            EmoteList.SetChannel(channel);

            EmoteList.Show();

            EmoteList.FormClosed += (s, e) =>
            {
                EmoteList = null;
            };
        }

        public static void SetEmoteListChannel(TwitchChannel channel)
        {
            EmoteList?.SetChannel(channel);
        }

        private static void _updateTheme()
        {
            var multiplier = -0.8f;

            switch (AppSettings.CurrentTheme)
            {
                case "White":
                    multiplier = 1f;
                    break;
                case "Light":
                    multiplier = 0.8f;
                    break;
                case "Dark":
                    multiplier = -0.8f;
                    break;
                case "Black":
                    multiplier = -1f;
                    break;
            }

            ColorScheme = ColorScheme.FromHue((float)Math.Max(Math.Min(AppSettings.ThemeHue, 1), 0), multiplier);

            MainForm?.Invoke(() => MainForm.Refresh());
        }
    }
}
