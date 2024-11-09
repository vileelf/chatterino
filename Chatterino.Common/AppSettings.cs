﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public static class AppSettings
    {
        public static string CurrentVersion { get; set; } = "0.0";

        public static string SelectedUser { get; set; } = "";

        public static string SavePath { get; set; }

        // Theme
        public static event EventHandler ThemeChanged;

        public static string CurrentTheme { get; private set; }

        private static string _theme = "Dark";

        public static string Theme
        {
            get { return _theme; }
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    UpdateCurrentTheme();
                }
            }
        }

        private static string _nightTheme = "Dark";

        public static string NightTheme
        {
            get { return _nightTheme; }
            set
            {
                if (_nightTheme != value)
                {
                    _nightTheme = value;
                    UpdateCurrentTheme();
                }
            }
        }

        private static int _nightThemeStart = 20;

        public static int NightThemeStart
        {
            get { return _nightThemeStart; }
            set
            {
                _nightThemeStart = value;

                UpdateCurrentTheme();
            }
        }

        private static int _nightThemeEnd = 6;

        public static int NightThemeEnd
        {
            get { return _nightThemeEnd; }
            set
            {
                _nightThemeEnd = value;

                UpdateCurrentTheme();
            }
        }

        private static bool _enableNightTheme;

        public static bool EnableNightTheme
        {
            get { return _enableNightTheme; }
            set
            {
                _enableNightTheme = value;
                UpdateCurrentTheme();
            }
        }

        private static double themeHue = 0.6;

        public static double ThemeHue
        {
            get { return themeHue; }
            set
            {
                themeHue = value;
                ThemeChanged?.Invoke(null, null);
            }
        }
        
        public static bool IsLightTheme() {
            switch (CurrentTheme)
            {
                case "White":
                    return true;
                case "Light":
                    return true;
                case "Dark":
                    return false;
                case "Black":
                    return false;
            }
            return true;
        }

        public static void UpdateCurrentTheme()
        {
            string theme;

            if (!EnableNightTheme)
            {
                theme = Theme;
            }
            else
            {
                var now = DateTime.Now;

                // start: 20:00, end: 6:00
                if (NightThemeStart > NightThemeEnd)
                {
                    theme = (NightThemeStart <= now.Hour || NightThemeEnd > now.Hour) ? NightTheme : Theme;
                }
                else
                {
                    theme = (NightThemeStart <= now.Hour && NightThemeEnd > now.Hour) ? NightTheme : Theme;
                }
            }

            if (theme != CurrentTheme)
            {
                CurrentTheme = theme;

                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static string _quality = "best";

        public static string Quality
        {
            get { return _quality; }
            set
            {
                if (_quality != value)
                {
                    _quality = value;
                }
            }
        }
        
        public static string SkipVersionNumber { get; set; }

        public static bool IgnoreSystemProxy { get; set; } = false;
        public static bool EnableStreamlinkPath { get; set; }
        public static string StreamlinkPath { get; set; }
        public static string CustomStreamlinkArguments { get; set; }
        public static bool PreferEmotesOverUsernames { get; set; }
        public static bool UseSingleConnection { get; set; }

        public static bool RemoveXButton { get; set; }
        
        public static bool ChangeTabTitle { get; set; } = true;
        
        public static bool ShowEmoteTooltip { get; set; } = true;
        
        public static bool HighlightHighlightedMessages { get; set; } = true;
        
        public static bool CacheEmotes { get; set; } = true;
        
        public static bool CheckUpdates { get; set; } = true;
        public static bool EnableReplys { get; set; } = true;
        
        public static bool RecentlyUsedEmoteList { get; set; } = true;
        
        public static bool IgnoreViaTwitch { get; set; } = true;

    // Chat
        public static double ScrollMultiplyer { get; set; } = 1;

        public static double EmoteScale { get; set; } = 1;
        public static bool EmoteScaleByLineHeight { get; set; } = false;
        public static bool EmoteScaleChanged {get; set;} = false;

        public static bool EnableBanButton { get; set; } = false;
        public static bool EnableTimeoutButton { get; set; } = false;
        public static bool EnableDeleteButton { get; set; } = false;


        public static List<int> TimeoutButtons { get; set; } = new List<int> { 5 * 60 };

        // 0 = no, 1 = if mod, 2 = if broadcaster
        public static int ChatShowIgnoredUsersMessages { get; set; } = 1;

        public static bool ChatShowLastReadMessageIndicator { get; set; } = false;

        public static bool ChatShowTimestamps { get; set; } = true;
        public static bool ChatShowTimestampSeconds { get; set; } = false;
        public static bool ChatAllowSameMessage { get; set; } = false;
        public static bool ChatLinksDoubleClickOnly { get; set; } = false;
        public static bool ChatHideInputIfEmpty { get; set; } = false;
        public static bool ChatInputShowMessageLength { get; set; } = false;
        public static bool ChatSeparateMessages { get; set; } = false;
        public static bool ChatTabLocalizedNames { get; set; } = true;

        public static bool ChatAllowCommandsAtEnd { get; set; } = false;

        public static bool ChatMentionUsersWithAt { get; set; } = false;

        public static event EventHandler MessageLimitChanged;

        private static int chatMessageLimit = 1000;
        public static int ChatMessageLimit
        {
            get { return chatMessageLimit; }
            set
            {
                if (chatMessageLimit != value)
                {
                    chatMessageLimit = value;
                    MessageLimitChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static bool ChatEnableHighlight { get; set; } = true;
        public static bool ChatEnableHighlightSound { get; set; } = false;
        public static bool ChatEnableHighlightTaskbar { get; set; } = true;
        public static bool ChatCustomHighlightSound { get; set; } = false;
        
        public static bool ChatEnableGoLiveSound { get; set; } = false;
        public static bool ChatEnableGoLiveTaskbar { get; set; } = true;
        public static bool ChatCustomGoLiveSound { get; set; } = false;
        public static string ChatCustomGoLiveSoundPath { get; set; } = "";

        public static ConcurrentDictionary<string, object> IgnoredUsers { get; private set; } = new ConcurrentDictionary<string, object>();
        public static ConcurrentDictionary<string, object> HighlightIgnoredUsers { get; private set; } = new ConcurrentDictionary<string, object>();
        public static ConcurrentDictionary<string, object> HighlightUserNames { get; private set; } = new ConcurrentDictionary<string, object>();
        public static ConcurrentDictionary<string, object> ChatIgnoredEmotes { get; private set; } = new ConcurrentDictionary<string, object>();
        
        public static ConcurrentDictionary<string, string> UserNotes { get; set; } = new ConcurrentDictionary<string, string>();

        // Custom Highlights
        private static string[] chatCustomHighlights = new string[0];
        public static string[] ChatCustomHighlights
        {
            get
            {
                return chatCustomHighlights;
            }
            set
            {
                chatCustomHighlights = value;
                UpdateCustomHighlightRegex();
            }
        }

        public static void UpdateCustomHighlightRegex()
        {
            CustomHighlightRegex = new Regex($@"\b({(IrcManager.Account.Username)}{(IrcManager.Account.Username == null || chatCustomHighlights.Length == 0 ? "" : "|")}{string.Join("|", chatCustomHighlights.Select(x => Regex.Escape(x)))})\b".Log(), RegexOptions.IgnoreCase);
        }
        public static Regex CustomHighlightRegex { get; private set; } = null;

        // Ignored words
        private static string[] chatIgnoredKeywords = new string[0];
        public static string[] ChatIgnoredKeywords
        {
            get
            {
                return chatIgnoredKeywords;
            }
            set
            {
                chatIgnoredKeywords = value;
                UpdateIgnoredKeywordsRegex();
            }
        }
        
        public static string GetNotes (string userid) {
            string notes;
            UserNotes.TryGetValue(userid, out notes);
            return notes;
        }
        
        public static void SetNotes (string userid, string notes) {
            UserNotes.AddOrUpdate(userid, notes, (olduserid, oldnote) => notes);
        }

        public static void UpdateIgnoredKeywordsRegex()
        {
            if (chatIgnoredKeywords.Length == 0)
            {
                IgnoredKeywordsRegex = null;
            }
            else
            {
                IgnoredKeywordsRegex = new Regex($@"\b({string.Join("|", chatIgnoredKeywords.Select(x => Regex.Escape(x)))})\b".Log(), RegexOptions.IgnoreCase);
            }
        }
        public static Regex IgnoredKeywordsRegex { get; private set; } = null;

        public static bool EnableTwitchUserIgnores { get; set; } = true;

        public static bool ChatEnableTwitchEmotes { get; set; } = true;
        public static bool ChatEnableBttvEmotes { get; set; } = true;
        public static bool ChatEnableFfzEmotes { get; set; } = true;
        public static bool ChatEnable7tvEmotes { get; set; } = true;
        public static bool ChatEnableEmojis { get; set; } = true;
        public static bool ChatEnableGifAnimations { get; set; } = true;

        public static bool ChatEnableInlineWhispers { get; set; } = false;
        public static bool Rainbow { get; set; } = false;
        public static string OldColor { get; set; }
        public static bool TimestampsAmPm { get; set; } = false;

        public static bool ProxyEnable { get; set; } = false;
        public static string ProxyType { get; set; } = "http";
        public static string ProxyHost { get; set; } = "";
        public static string ProxyUsername { get; set; } = "";
        public static string ProxyPassword { get; set; } = "";
        public static int ProxyPort { get; set; } = 80;

        public static int WindowX { get; set; } = 200;
        public static int WindowY { get; set; } = 200;
        public static int WindowWidth { get; set; } = 600;
        public static int WindowHeight { get; set; } = 400;

        public static event EventHandler<ValueEventArgs<bool>> WindowTopMostChanged;

        private static bool windowTopMost;

        public static bool WindowTopMost
        {
            get { return windowTopMost; }
            set
            {
                if (windowTopMost != value)
                {
                    windowTopMost = value;

                    WindowTopMostChanged?.Invoke(null, new ValueEventArgs<bool>(value));
                }
            }
        }

        public static event EventHandler FontChanged;

        public static string FontFamily { get; private set; } = "Helvetica Neue";
        public static double FontBaseSize { get; private set; } = 10;

        public static void SetFont(string family, float size)
        {
            FontFamily = family;
            FontBaseSize = size;

            FontChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string BrowserExtensionKey { get; set; } = "pass123";


        // static stuff
        public static ConcurrentDictionary<string, PropertyInfo> Properties = new ConcurrentDictionary<string, PropertyInfo>();

        private static System.Threading.Timer everyHourTimer;

        static AppSettings()
        {
            var T = typeof(AppSettings);

            foreach (var property in T.GetProperties())
            {
                if (property.Name != "SavePath")
                {
                    if (property.CanRead && property.CanWrite)
                        Properties[property.Name] = property;
                }
            }

            Func<long> timeUntilFullHour = () => ((59 - DateTime.Now.Minute) * 60 + 60 - DateTime.Now.Second) * 1000;

            everyHourTimer = new Timer(state =>
            {
                UpdateCurrentTheme();
                everyHourTimer.Change(timeUntilFullHour(), -1);
            }, null, timeUntilFullHour(), -1);
        }


        // IO
        public static void Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = SavePath;
            }

            var settings = new IniSettings();
            settings.Load(path);
            var parser = new JsonParser();
                

            foreach (var prop in Properties.Values)
            {
                if (prop.PropertyType == typeof(string))
                    prop.SetValue(null, settings.GetString(prop.Name, (string)prop.GetValue(null)));
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(null, settings.GetInt(prop.Name, (int)prop.GetValue(null)));
                else if (prop.PropertyType == typeof(double))
                    prop.SetValue(null, settings.GetDouble(prop.Name, (double)prop.GetValue(null)));
                else if (prop.PropertyType == typeof(bool))
                    prop.SetValue(null, settings.GetBool(prop.Name, (bool)prop.GetValue(null)));
                else if (prop.PropertyType == typeof(string[]))
                {
                    string[] vals;
                    if (settings.TryGetStrings(prop.Name, out vals))
                        prop.SetValue(null, vals);
                }
                else if (prop.PropertyType == typeof(ConcurrentDictionary<string, object>))
                {
                    var dict = (ConcurrentDictionary<string, object>)prop.GetValue(null);

                    dict.Clear();

                    string[] vals;
                    if (settings.TryGetStrings(prop.Name, out vals))
                    {
                        foreach (var s in vals)
                            dict[s] = null;
                    }
                }
                else if (prop.PropertyType == typeof(ConcurrentDictionary<string, string>))
                {
                    var dict = (ConcurrentDictionary<string, string>)prop.GetValue(null);

                    dict.Clear();
                    
                    string val;
                    val = settings.GetString(prop.Name, "");
                    if (!String.IsNullOrEmpty(val)) {
                        try {
                            var newdict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(val);
                            if (newdict != null) {
                                prop.SetValue(null ,newdict);
                            }
                        } catch {}
                        
                    }
                }
                else if (prop.PropertyType == typeof(List<int>))
                {
                    int xd;
                    prop.SetValue(null, settings.GetString(prop.Name, "").Split(',').Select(x => x.Trim())
                        .Where(x => int.TryParse(x, out xd)).Select(x => int.Parse(x)).ToList());
                }
            }
        }

        public static void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = SavePath;
            }

            var settings = new IniSettings();

            foreach (var prop in Properties.Values)
            {
                if (prop.PropertyType == typeof(string))
                    settings.Set(prop.Name, (string)prop.GetValue(null));
                else if (prop.PropertyType == typeof(int))
                    settings.Set(prop.Name, (int)prop.GetValue(null));
                else if (prop.PropertyType == typeof(double))
                    settings.Set(prop.Name, (double)prop.GetValue(null));
                else if (prop.PropertyType == typeof(bool))
                    settings.Set(prop.Name, (bool)prop.GetValue(null));
                else if (prop.PropertyType == typeof(string[]))
                    settings.Set(prop.Name, (string[])prop.GetValue(null));
                else if (prop.PropertyType == typeof(ConcurrentDictionary<string, object>))
                    settings.Set(prop.Name, ((ConcurrentDictionary<string, object>)prop.GetValue(null)).Keys.OrderBy(s => s));
                else if (prop.PropertyType == typeof(ConcurrentDictionary<string, string>)) 
                    settings.Set(prop.Name, JsonConvert.SerializeObject(((ConcurrentDictionary<string, string>)prop.GetValue(null))));  
                else if (prop.PropertyType == typeof(List<int>))
                    settings.Set(prop.Name, string.Join(",", (List<int>)prop.GetValue(null)));
            }

            settings.Save(path);
        }
    }
}
