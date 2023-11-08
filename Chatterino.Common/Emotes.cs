using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public static class Emotes
    {
        public static event EventHandler EmotesLoaded;

        public static ConcurrentDictionary<string, LazyLoadedImage> RecentlyUsedEmotes =
            new ConcurrentDictionary<string, LazyLoadedImage>();

        public static ConcurrentDictionary<string, IrcManager.TwitchEmoteValue> TwitchEmotes =
            new ConcurrentDictionary<string, IrcManager.TwitchEmoteValue>();

        public static ConcurrentDictionary<string, LazyLoadedImage> BttvGlobalEmotes =
            new ConcurrentDictionary<string, LazyLoadedImage>();
            
        public static ConcurrentDictionary<string, LazyLoadedImage> SeventvGlobalEmotes =
            new ConcurrentDictionary<string, LazyLoadedImage>();


        public static ConcurrentDictionary<string, LazyLoadedImage> FfzGlobalEmotes =
            new ConcurrentDictionary<string, LazyLoadedImage>();

        public static ConcurrentDictionary<string, LazyLoadedImage> BttvChannelEmotesCache =
            new ConcurrentDictionary<string, LazyLoadedImage>();
            
        public static ConcurrentDictionary<string, LazyLoadedImage> SeventvChannelEmotesCache =
            new ConcurrentDictionary<string, LazyLoadedImage>();

        public static ConcurrentDictionary<string, LazyLoadedImage> FfzChannelEmotesCache =
            new ConcurrentDictionary<string, LazyLoadedImage>();

        public static ConcurrentDictionary<string, LazyLoadedImage> TwitchEmotesByIDCache =
            new ConcurrentDictionary<string, LazyLoadedImage>();

        public static ConcurrentDictionary<string, LazyLoadedImage> MiscEmotesByUrl =
            new ConcurrentDictionary<string, LazyLoadedImage>();

        private static ConcurrentDictionary<string, string> twitchEmotesCodeReplacements =
            new ConcurrentDictionary<string, string>();

        public const string TwitchEmoteTemplate = "https://static-cdn.jtvnw.net/emoticons/v2/{id}/{animated}/{darkmode}/{scale}.0";

        private static string twitchemotesGlobalCache = Path.Combine(Util.GetUserDataPath(), "Cache",
            "twitchemotes_global.json");

        private static string bttvEmotesGlobalCache = Path.Combine(Util.GetUserDataPath(), "Cache", "bttv_global.json");
        private static string ffzEmotesGlobalCache = Path.Combine(Util.GetUserDataPath(), "Cache", "ffz_global.json");
        private static string seventvEmotesGlobalCache = Path.Combine(Util.GetUserDataPath(), "Cache", "7tv_global.json");

        static Emotes()
        {
            Func<string, string, string, LazyLoadedImage> getEmoteReplacement = (id, name, url) =>
            {
                var emote = new LazyLoadedImage()
                {
                    Url = url,
                    Name = name,
                    Tooltip = $"{name}\nTwitch Global Emote\n(chatterino dankmode friendly version)",
                    IsEmote = true
                };
                emote.LoadAction = () =>
                {
                    ChatterinoImage img;

                    try
                    {
                        var request = WebRequest.Create(url);
                        if (AppSettings.IgnoreSystemProxy)
                        {
                            request.Proxy = null;
                        }
                        using (var response = request.GetResponse()) {
                            using (var stream = response.GetResponseStream())
                            {
                                MemoryStream mem = new MemoryStream();
                                stream.CopyTo(mem);
                                img = GuiEngine.Current.ReadImageFromStream(mem);
                            }
                            response.Close();
                        }

                        GuiEngine.Current.FreezeImage(img);
                    }
                    catch
                    {
                        img = null;
                    }

                    if (img == null)
                    {
                        double scale;
                        double fake;
                        string tooltipurl;
                        url = GetTwitchEmoteLink(id, false, out scale);
                        tooltipurl = GetTwitchEmoteLink(id, true, out fake);
                        emote.Url = url;
                        emote.TooltipImageUrl = tooltipurl;

                        try
                        {
                            var request = WebRequest.Create(url);
                            if (AppSettings.IgnoreSystemProxy)
                            {
                                request.Proxy = null;
                            }
                            using (var response = request.GetResponse()) {
                                using (var stream = response.GetResponseStream())
                                {
                                    MemoryStream mem = new MemoryStream();
                                    stream.CopyTo(mem);
                                    img = GuiEngine.Current.ReadImageFromStream(mem);
                                }
                                response.Close();
                            }

                            GuiEngine.Current.FreezeImage(img);
                        }
                        catch
                        {
                            img = null;
                        }
                    }

                    return img;
                };
                return emote;
            };
            twitchEmotesCodeReplacements[@"[oO](_|\.)[oO]"] = "o_O";
            twitchEmotesCodeReplacements[@"\&gt\;\("] = ">(";
            twitchEmotesCodeReplacements[@"\&lt\;3"] = "<3";
            twitchEmotesCodeReplacements[@"\:-?(o|O)"] = ":O";
            twitchEmotesCodeReplacements[@"\:-?(p|P)"] = ":P";
            twitchEmotesCodeReplacements[@"\:-?[\\/]"] = ":/";
            twitchEmotesCodeReplacements[@"\:-?[z|Z|\|]"] = ":z";
            twitchEmotesCodeReplacements[@"\:-?\("] = ":(";
            twitchEmotesCodeReplacements[@"\:-?\)"] = ":)";
            twitchEmotesCodeReplacements[@"\:-?D"] = ":D";
            twitchEmotesCodeReplacements[@"\;-?(p|P)"] = ";P";
            twitchEmotesCodeReplacements[@"\;-?\)"] = ";)";
            twitchEmotesCodeReplacements[@"R-?\)"] = "R-)";

            _bttvHatEmotes["IceCold"] = null;
            _bttvHatEmotes["SoSnowy"] = null;
            _bttvHatEmotes["TopHat"] = null;
            _bttvHatEmotes["SantaHat"] = null;
            _bttvHatEmotes["ReinDeer"] = null;
            _bttvHatEmotes["CandyCane"] = null;
            _bttvHatEmotes["cvHazmat"] = null;
            _bttvHatEmotes["cvMask"] = null;
        }

        public static void ReloadSubEmotes()
        {
            TwitchEmotes.Clear();
            IrcManager.LoadUsersEmotes();
        }

        public static string GetTwitchEmoteLink(string id, bool getMax, out double scale)
        {
            var _scale = AppSettings.EmoteScale > 2 ? 4 : (AppSettings.EmoteScale > 1 ? 2 : 1);
            if (getMax) {
              _scale = 4;  
            }
            scale = 1.0 / _scale;

            return TwitchEmoteTemplate.Replace("{id}", id.ToString()).Replace("{scale}", (_scale == 4) ? 3 + "" : _scale.ToString())
                                        .Replace("{darkmode}", (AppSettings.IsLightTheme())?"light":"dark")
                                        .Replace("{animated}", (AppSettings.ChatEnableGifAnimations)?"default":"static");
        }

        public static string GetBttvEmoteLink(string link, bool getMax, out double scale)
        {
            var _scale = AppSettings.EmoteScale > 2 ? 4 : (AppSettings.EmoteScale > 1 ? 2 : 1);
            if (getMax) {
              _scale = 4;  
            }

            scale = 1.0 / _scale;

            return link.Replace("{{image}}", ((_scale == 4) ? 3 : _scale) + "x");
        }

        public static string fixFFZUrl(string url)
        {
            if (url.IndexOf("https:") != 0)
            {
                url = "https:" + url;
            }
            return url;
        }

        public static string getUrlFromScale(string url1, string url2, string url4, double curscale, int maxScale, out double scale)
        {
            var _scale = curscale > 2 ? 4 : (curscale > 1 ? 2 : 1);

            

            string url;
            if (maxScale >= 2 && _scale == 2)
            {
                _scale = 2;
                url = url2;
            }
            else if (maxScale == 4 && _scale == 4)
            {
                _scale = 4;
                url = url4;
            }
            else
            {
                _scale = 1;
                url = url1;
            }

            scale = 1.0 / _scale;

            return url;
        }

        public static IEnumerable<LazyLoadedImage> GetFfzEmoteFromDynamic(dynamic d, bool global)
        {
            List<LazyLoadedImage> emotes = new List<LazyLoadedImage>();

            foreach (var emote in d)
            {
                var name = emote["name"];

                dynamic urls = emote["urls"];

                int maxScale = 1;

                string urlX1 = fixFFZUrl(urls["1"]);

                string urlX2 = null;
                if (urls.ContainsKey("2"))
                {
                    urlX2 = fixFFZUrl(urls["2"]);
                    maxScale = 2;
                }

                string urlX4 = null;
                if (urls.ContainsKey("4"))
                {
                    urlX4 = fixFFZUrl(urls["4"]);
                    maxScale = 4;
                }

                string url = getUrlFromScale(urlX1, urlX2, urlX4, AppSettings.EmoteScale, maxScale, out double scale);

                string margins = emote["margins"];
                string tooltipurl = getUrlFromScale(urlX1, urlX2, urlX4, 4, maxScale, out double temp);

                Margin margin = null;

                if (!string.IsNullOrEmpty(margins))
                {
                    var S = margins.Split(' ');

                    if (S.Length == 4)
                    {
                        try
                        {
                            var ints = S.Select(x =>
                            {
                                var index = x.Length;
                                for (var i = 0; i < x.Length; i++)
                                {
                                    if (x[i] != '-' && (x[i] < '0' || x[i] > '9'))
                                    {
                                        index = i - 1;
                                    }
                                }

                                return int.Parse(index == x.Length ? x : x.Remove(index));
                            }).ToArray();

                            margin = new Margin(ints[0], ints[1], ints[2], ints[3]);
                        }
                        catch
                        {
                        }
                    }
                }

                emotes.Add(new LazyLoadedImage { Name = name, Url = url, TooltipImageUrl = tooltipurl, Scale = scale, Margin = margin, Tooltip = name + $"\nFrankerFaceZ {(global ? "Global" : "Channel")} Emote", IsEmote = true });
            }

            return emotes;
        }

        public static string GetTwitchEmoteCodeReplacement(string emoteCode)
        {
            string code;

            return twitchEmotesCodeReplacements.TryGetValue(emoteCode, out code) ? code : emoteCode;
        }
        
        public static void EmoteAdded() {
            EmotesLoaded?.Invoke(null, EventArgs.Empty);
        }

        public static void LoadGlobalEmotes()
        {

            // bttv emotes
            Task.Run(() =>
            {
                try
                {
                    var parser = new System.Text.Json.JsonParser();

                    // better twitch tv emotes
                    //if (!File.Exists(bttvEmotesGlobalCache))
                    {
                        try
                        {

                            if (Util.IsLinux)
                            {
                                Util.LinuxDownloadFile("https://api.betterttv.net/3/cached/emotes/global", bttvEmotesGlobalCache);
                            }
                            else
                            {
                                using (var webClient = new WebClient()) {
                                    using (var readStream = webClient.OpenRead("https://api.betterttv.net/3/cached/emotes/global")) {
                                        if (File.Exists(bttvEmotesGlobalCache)) {
                                            File.Delete(bttvEmotesGlobalCache);
                                        }
                                        using (var writeStream = File.OpenWrite(bttvEmotesGlobalCache))
                                        {
                                            readStream.CopyTo(writeStream);
                                        }
                                        readStream.Close();
                                    }
                                    webClient.Dispose();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.Message.Log("emotes");
                        }
                    }

                    using (var stream = File.OpenRead(bttvEmotesGlobalCache))
                    {
                        dynamic json = parser.Parse(stream);
                        //var template = "https:" + json["urlTemplate"]; // urlTemplate is outdated, came from bttv v2 api, returned: //cdn.betterttv.net/emote/{{id}}/{{image}}
                        var template = "https://cdn.betterttv.net/emote/{{id}}/{{image}}";

                        foreach (var e in json)
                        {
                            string id = e["id"];
                            string code = e["code"];
                            string imageType = e["imageType"];
                            string url = template.Replace("{{id}}", id);
                            double fake;
                            string tooltipurl = GetBttvEmoteLink(url, true, out fake);

                            double scale;
                            url = GetBttvEmoteLink(url, false, out scale);

                            BttvGlobalEmotes[code] = new LazyLoadedImage { Name = code, Url = url, TooltipImageUrl = tooltipurl, IsHat = IsBttvEmoteAHat(code), Scale = scale, Tooltip = code + "\nBetterTTV Global Emote", IsEmote = true };
                        }
                    }
                    EmotesLoaded?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("error loading emotes: " + exc.Message);
                }
            });

            // ffz emotes
            Task.Run(() =>
            {
                try
                {
                    var parser = new System.Text.Json.JsonParser();

                    //if (!File.Exists(ffzEmotesGlobalCache))
                    {
                        try
                        {

                            if (Util.IsLinux)
                            {
                                Util.LinuxDownloadFile("https://api.frankerfacez.com/v1/set/global", ffzEmotesGlobalCache);
                            }
                            else
                            {
                                using (var webClient = new WebClient()) {
                                    using (var readStream = webClient.OpenRead("https://api.frankerfacez.com/v1/set/global")) {
                                        if (File.Exists(ffzEmotesGlobalCache)) {
                                            File.Delete(ffzEmotesGlobalCache);
                                        }
                                        using (var writeStream = File.OpenWrite(ffzEmotesGlobalCache))
                                        {
                                            readStream.CopyTo(writeStream);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.Message.Log("emotes");
                        }
                    }

                    using (var stream = File.OpenRead(ffzEmotesGlobalCache))
                    {
                        dynamic json = parser.Parse(stream);

                        foreach (var set in json["sets"])
                        {
                            var val = set.Value;

                            foreach (LazyLoadedImage emote in GetFfzEmoteFromDynamic(val["emoticons"], true))
                            {
                                FfzGlobalEmotes[emote.Name] = emote;
                            }
                        }
                    }
                    EmotesLoaded?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("error loading emotes: " + exc.Message);
                }
            });

            // ffz event emotes
            Task.Run(() =>
            {
                try
                {
                    var set = 0;

                    var parser = new System.Text.Json.JsonParser();
                    using (var webClient = new WebClient()) {
                        using (var readStream = webClient.OpenRead("https://cdn.frankerfacez.com/script/event.json"))
                        {
                            dynamic json = parser.Parse(readStream);

                            string _set = json["set"];

                            int.TryParse(_set, out set);
                            readStream.Close();
                        }
                        webClient.Dispose();
                    }

                    if (set != 0)
                    {
                        using (var webClient = new WebClient()) {
                            using (var readStream = webClient.OpenRead("https://api.frankerfacez.com/v1/set/" + set))
                            {
                                dynamic json = parser.Parse(readStream);
                                dynamic _set = json["set"];

                                dynamic emoticons = _set["emoticons"];

                                foreach (LazyLoadedImage emote in GetFfzEmoteFromDynamic(emoticons, true))
                                {
                                    FfzGlobalEmotes[emote.Name] = emote;
                                }
                                readStream.Close();
                            }
                            webClient.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    e.Message.Log("emotes");
                }
            });
            
            //7tv global emotes 
            Task.Run(() =>
            {
                try
                {
                    var parser = new System.Text.Json.JsonParser();

                    //if (!File.Exists(ffzEmotesGlobalCache))
                    {
                        try
                        {

                            if (Util.IsLinux)
                            {
                                Util.LinuxDownloadFile("https://7tv.io/v3/emote-sets/global", seventvEmotesGlobalCache);
                            }
                            else
                            {
                                using (var webClient = new WebClient()) {
                                    using (var readStream = webClient.OpenRead("https://7tv.io/v3/emote-sets/global")) {
                                        if (File.Exists(seventvEmotesGlobalCache)) {
                                            File.Delete(seventvEmotesGlobalCache);
                                        }
                                        using (var writeStream = File.OpenWrite(seventvEmotesGlobalCache))
                                        {
                                            readStream.CopyTo(writeStream);
                                        }
                                        readStream.Close();
                                    }
                                    webClient.Dispose();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.Message.Log("emotes");
                        }
                    }

                    using (var stream = File.OpenRead(seventvEmotesGlobalCache))
                    {
                        dynamic json = parser.Parse(stream);
                        string emotename;
                        string emoteid;
                        dynamic owner;
                        string ownername;
                        string template = "https://cdn.7tv.app/emote/{{id}}/{{image}}";
                        bool getemote;
                        double fake;
                        double scale;
                        string tooltipurl;
                        string url;
                        int visibilityFlags;
                        string visibility;
                        const int zeroWidthFlag = 0x100;
                        bool zeroWidth = false;
                        
                        LazyLoadedImage emote;
                        var emotes = json["emotes"];
                        foreach (var e in emotes)
                        {
                            var data = e["data"];
                            emotename = data["name"];
                            emoteid = data["id"];
                            var host = data["host"];
                            var baseUrl = fixFFZUrl(host["url"]);
                            var urls = host["files"];

                            int maxScale = 1;
                            string urlX1 = null;
                            string urlX2 = null;
                            string urlX4 = null;
                            foreach (var file in urls) {
                                if (file["name"] == "1x.webp") {
                                    urlX1 = $"{baseUrl}/{file["name"]}";
                                } else if (file["name"] == "2x.webp") {
                                    urlX2 = $"{baseUrl}/{file["name"]}";
                                    maxScale = Math.Max(maxScale, 2);
                                } else if (file["name"] == "4x.webp") {
                                    urlX4 = $"{baseUrl}/{file["name"]}";
                                    maxScale = Math.Max(maxScale, 4);
                                }
                            }

                            url = getUrlFromScale(urlX1, urlX2, urlX4, AppSettings.EmoteScale, maxScale, out scale);

                            tooltipurl = getUrlFromScale(urlX1, urlX2, urlX4, 4, maxScale, out fake);

                            owner = data.ContainsKey("owner") ? data["owner"] : null;
                            visibility = data["flags"];
                            if (!string.IsNullOrEmpty(visibility) && int.TryParse(visibility, out visibilityFlags)) {
                                zeroWidth = (visibilityFlags & zeroWidthFlag) > 0;
                            }
                            ownername = "";
                            if (owner != null) {
                                ownername = owner["display_name"];
                                if (string.IsNullOrEmpty(ownername)) {
                                    ownername = owner["username"];
                                } else if (!string.IsNullOrEmpty(owner["username"]) && string.Compare(ownername.ToUpper(), owner["username"].ToUpper()) != 0) {
                                    ownername = ownername + "(" + owner["username"] + ")";
                                }
                            }
                            emote = new LazyLoadedImage
                                {
                                    Name = emotename,
                                    Url = url,
                                    Tooltip = emotename + "\n7TV Channel Emote\nChannel: " + ownername,
                                    TooltipImageUrl = tooltipurl,
                                    Scale = scale,
                                    IsHat = zeroWidth,
                                    IsEmote = true
                                };
                            SeventvGlobalEmotes[emote.Name] = emote;
                        }
                    }
                    EmotesLoaded?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("error loading emotes: " + exc.Message);
                    GuiEngine.Current.log("error loading emotes: " + exc.Message);
                }
            });
        }

        private static ConcurrentDictionary<string, object> _bttvHatEmotes = new ConcurrentDictionary<string, object>();

        private static bool IsBttvEmoteAHat(string code)
        {
            return code.StartsWith("Hallo") ||
                   _bttvHatEmotes.ContainsKey(code);
        }

        internal static void TriggerEmotesLoaded()
        {
            EmotesLoaded?.Invoke(null, EventArgs.Empty);
        }

        public static void ClearTwitchEmoteCache() {
            TwitchEmotesByIDCache.Clear();
        }

        public static LazyLoadedImage GetTwitchEmoteById(string id, string name)
        {
            LazyLoadedImage e;
            double scale;
            double fake;
            string url = GetTwitchEmoteLink(id, false, out scale);
            
            if (!TwitchEmotesByIDCache.TryGetValue(id, out e) || Math.Abs(scale - e.Scale) > .01)
            {
                e = new LazyLoadedImage
                {
                    Name = name,
                    Url = url,
                    TooltipImageUrl = GetTwitchEmoteLink(id, true, out fake),
                    Scale = scale,
                    Tooltip = name + "\nTwitch Emote",
                    IsEmote = true
                };
                TwitchEmotesByIDCache[id] = e;
            }

            return e;
        }
    }
}
