﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TwitchIrc;

namespace Chatterino.Common
{
    public class TwitchChannel
    {
       int maxMessages = AppSettings.ChatMessageLimit;
        
        private System.Timers.Timer reconnectTryAgainTimer;

        // properties
        public string Name { get; private set; }

        public int RoomID { get; private set; } = -1;

        public string SubLink { get; private set; }
        public string ChannelLink { get; private set; }
        public string PopoutPlayerLink { get; private set; }
        public static TwitchChannel SelectedChannel;

        protected int Uses { get; set; } = 0;

        public bool IsLive { get; private set; }
        public bool IsFollowing { get; private set; }

        public int StreamViewerCount { get; private set; }
        public string StreamStatus { get; private set; }
        public string StreamGame { get; private set; }
        public DateTime StreamStart { get; set; }
        public static Random rand = new Random();

        public event EventHandler<LiveStatusEventArgs> LiveStatusUpdated;

        // Channel Emotes
        public ConcurrentDictionary<string, LazyLoadedImage> BttvChannelEmotes { get; private set; }
        = new ConcurrentDictionary<string, LazyLoadedImage>();

        public ConcurrentDictionary<string, LazyLoadedImage> FfzChannelEmotes { get; private set; }
        = new ConcurrentDictionary<string, LazyLoadedImage>();
        
        public ConcurrentDictionary<string, LazyLoadedImage> SeventvChannelEmotes { get; private set; }
        = new ConcurrentDictionary<string, LazyLoadedImage>();
        
        public ConcurrentDictionary<string, LazyLoadedImage> ChannelEmotes { get; private set; }
        = new ConcurrentDictionary<string, LazyLoadedImage>();
        
        public ConcurrentDictionary<string, LazyLoadedImage> FollowerEmotes { get; private set; }
        = new ConcurrentDictionary<string, LazyLoadedImage>();

        public ConcurrentDictionary<int, LazyLoadedImage> SubscriberBadges = new ConcurrentDictionary<int, LazyLoadedImage>();
        public ConcurrentDictionary<int, LazyLoadedImage> CheerBadges = new ConcurrentDictionary<int, LazyLoadedImage>();
        private ConcurrentDictionary<string, CheerEmote> ChannelCheerEmotes = new ConcurrentDictionary<string, CheerEmote>();
        private bool connected = false;

        public LazyLoadedImage GetSubscriberBadge(int months)
        {
            LazyLoadedImage emote;

            if (SubscriberBadges.TryGetValue(months, out emote))
            {
                return emote;
            }
            //GuiEngine.Current.log("sub "+months);
            return null;
        }

        public LazyLoadedImage GetCheerBadge(int cheer)
        {
            LazyLoadedImage emote;

            if (CheerBadges.TryGetValue(cheer, out emote))
            {
                return emote;
            }

            return null;
        }
        
        public bool GetCheerEmote(string name,int cheer, bool light, out LazyLoadedImage outemote, out string outcolor)
        {
            CheerEmote emote;
            LazyLoadedImage emoteimage;
            string color;
            outemote = null;
            outcolor = null;

            if (ChannelCheerEmotes.TryGetValue(name.ToUpper(), out emote))
            {
                bool ret = emote.GetCheerEmote(cheer,light,out emoteimage,out color);
                outemote = emoteimage;
                outcolor = color;
                return ret;
            }
            return false;
        }

        // Moderator Badge
        public LazyLoadedImage ModeratorBadge { get; private set; } = null;
        public LazyLoadedImage VipBadge { get; private set; } = null;


        // Roomstate
        public event EventHandler RoomStateChanged;

        private RoomState roomState;

        public RoomState RoomState
        {
            get { return roomState; }
            set
            {
                if (roomState != value)
                {
                    roomState = value;
                    RoomStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private int slowModeTime;

        public int SlowModeTime
        {
            get { return slowModeTime; }
            set
            {
                if (slowModeTime != value)
                {
                    slowModeTime = value;
                    RoomStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        
        private int followModeTime;
        
        public int FollowModeTime
        {
            get { return followModeTime; }
            set
            {
                if (followModeTime != value)
                {
                    followModeTime = value;
                    RoomStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        // Userstate
        public event EventHandler UserStateChanged;

        private bool isMod;

        public bool IsMod
        {
            get { return isMod; }
            set
            {
                if (isMod != value)
                {
                    isMod = value;

                    UserStateChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        private bool isVip;

        public bool IsVip
        {
            get { return isVip; }
            set
            {
                if (isVip != value)
                {
                    isVip = value;

                    UserStateChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public bool IsModOrBroadcaster
        {
            get
            {
                if (IrcManager.Account.IsAnon)
                    return false;

                return IsMod ||
                       string.Equals(Name, IrcManager.Account.Username, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private CheerEmote JsonLoadCheerEmote(dynamic emote) {
            CheerEmote customCheer = new CheerEmote();
            dynamic tiers = emote["tiers"];
            for(int j=0;j<tiers.Count;j++){
                int min_bits = int.Parse(tiers[j]["min_bits"]);
                bool can_use = tiers[j]["can_cheer"];
                string bitcolor = tiers[j]["color"];
                dynamic images = tiers[j]["images"];
                string dark = images["dark"]["animated"]["1"];
                string light = images["light"]["animated"]["1"];
                string darkbig = images["dark"]["animated"]["3"];
                string lightbig = images["light"]["animated"]["3"];
                LazyLoadedImage lightemote = new LazyLoadedImage
                {
                    Name = "cheer",
                    Url = light,
                    Tooltip = "Twitch Bits Donation",
                    TooltipImageUrl = lightbig,
                    click_url = lightbig
                };
                LazyLoadedImage darkemote = new LazyLoadedImage
                {
                    Name = "cheer",
                    Url = dark,
                    Tooltip = "Twitch Bits Donation",
                    TooltipImageUrl = darkbig,
                    click_url = darkbig
                };
                customCheer.Add(lightemote, darkemote, min_bits, bitcolor);
            }
            return customCheer;
        }
        
        public void LoadChannelEmotes(int RoomID)
        {
            try {
                ChannelEmotes.Clear();
                FollowerEmotes.Clear();
                var request =
                    WebRequest.Create(
                        $"https://api.twitch.tv/helix/chat/emotes?broadcaster_id={RoomID}");
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                request.Method = "GET";
                request.Headers.Add("Client-ID", IrcManager.Account.ClientId);
                request.Headers["Authorization"]=$"Bearer {IrcManager.Account.OauthToken}";
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream())
                    {
                        JsonParser parser = new JsonParser();

                        dynamic json = parser.Parse(stream);
                        dynamic data = json["data"];
                        string id;
                        string name;
                        string type;
                        string tier;
                        string set_id;
                        string url;
                        double scale;
                        double fake;
                        LazyLoadedImage emote;
                        foreach (var emotedata in data){
                            id = emotedata["id"];
                            name = emotedata["name"];
                            type = emotedata["emote_type"];
                            tier = emotedata["tier"];
                            set_id = emotedata["emote_set_id"];
                            
                            url = Emotes.GetTwitchEmoteLink(id, false, out scale);
                            emote = new LazyLoadedImage{
                                Name = name,
                                Scale = scale,
                                Url = url,
                                TooltipImageUrl = Emotes.GetTwitchEmoteLink(id, true, out fake),
                                Tooltip = name + "\n " + 
                                ((type.Equals("bitstier"))?"Bit":(type.Equals("follower"))?"Follower":(tier.Equals("1000"))?"Tier 1 Sub":(tier.Equals("2000"))?"Tier 2 Sub":"Tier 3 Sub")
                                + " Emote",
                                IsEmote = true
                            };
                            
                            emote.EmoteInfo.id = id;
                            emote.EmoteInfo.type = type;
                            emote.EmoteInfo.tier = tier;
                            emote.EmoteInfo.setid = set_id;
                            emote.EmoteInfo.ownerid = RoomID.ToString();
                            
                            if (type.Equals("follower")) {
                                Emotes.RecentlyUsedEmotes.TryRemove(name, out LazyLoadedImage image);
                                FollowerEmotes[name] = emote;
                            } else {
                                ChannelEmotes[id] = emote;
                            }
                        }
                    }
                    response.Close();
                }
            } catch(Exception e){
                GuiEngine.Current.log("Generic Exception Handler: " + "room " + RoomID + " " + e.ToString());
            }
        }
        
        public void LoadChannelBits(int RoomID)
        {
            try {
                var request =
                    WebRequest.Create(
                        $"https://api.twitch.tv/helix/bits/cheermotes?broadcaster_id={RoomID}");
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                request.Headers.Add("Client-ID", IrcManager.DefaultClientID);
                request.Headers["Authorization"]=$"Bearer {IrcManager.Account.OauthToken}";
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream())
                    {
                        JsonParser parser = new JsonParser();

                        dynamic json = parser.Parse(stream);

                        dynamic actions = json["data"];
                        bool cheerexists = false;
                        string type;
                        string prefix;
                        for(int i=0;i<actions.Count;i++){
                            type = actions[i]["type"];
                            prefix = actions[i]["prefix"];
                            if (prefix.ToUpper().Equals("CHEER")) {
                                cheerexists = true;
                            }
                            if(type.Equals("channel_custom")){
                                //this channels custom bit emote
                                CheerEmote customCheer = JsonLoadCheerEmote(actions[i]);
                                ChannelCheerEmotes.AddOrUpdate(prefix.ToUpper(), customCheer, (x, y) => customCheer);
                            } else if(!GuiEngine.Current.GlobalEmotesLoaded){
                                //global bit emote
                                CheerEmote customCheer = JsonLoadCheerEmote(actions[i]);
                                GuiEngine.Current.AddCheerEmote(prefix.ToUpper(), customCheer);
                            }
                        }
                        if (!cheerexists) {
                            //This is a shortcut for special channels like owl. I'm assuming all emotes are custom.
                            for(int i=0;i<actions.Count;i++){
                                type = actions[i]["type"];
                                prefix = actions[i]["prefix"];
                                //this channels custom bit emote
                                CheerEmote customCheer = JsonLoadCheerEmote(actions[i]);
                                ChannelCheerEmotes.AddOrUpdate(prefix.ToUpper(), customCheer, (x, y) => customCheer);
                                if (!GuiEngine.Current.GlobalEmotesLoaded) {
                                    //unload these from global since they aren't actually global Jebaited thx twitch
                                    GuiEngine.Current.ClearCheerEmotes();
                                }
                            }
                        } else {
                            GuiEngine.Current.GlobalEmotesLoaded = true;
                        }
                        stream.Close();
                    }
                    response.Close();
                }
            } catch(Exception e){
                GuiEngine.Current.log("Generic Exception Handler: " + "room " + RoomID + " " + e.ToString());
            }
        }

        public void LoadSubBadges(int RoomID)
        {
            try
            {
                dynamic json = TwitchApiHandler.Get("chat/badges", $"broadcaster_id={RoomID}");
                if (json is HttpStatusCode) { return; }
                dynamic badgeSets = json["data"];
                
                foreach (var badgeSet in badgeSets)
                {
                    string set_id = badgeSet["set_id"];
                    if (set_id == "subscriber")
                    {
                        dynamic versions = badgeSet["versions"];

                        foreach (var version in versions)
                        {
                            int months = int.Parse(version["id"]);

                            string imageUrl = version["image_url_1x"];
                            string title = version["title"];
                            string description = version["description"];
                            string clickUrl = version["click_url"];
                            string tooltipurl = version["image_url_4x"];

                            LazyLoadedImage subBadge = new LazyLoadedImage
                            {
                                Name = title,
                                Url = imageUrl,
                                TooltipImageUrl = tooltipurl,
                                Tooltip = title
                            };

                            SubscriberBadges.AddOrUpdate(months, subBadge, (key, oldvalue) => subBadge);
                        }
                    }
                    else if (set_id == "bits")
                    {
                        dynamic bitversions = badgeSet["versions"];
                        foreach (var version in bitversions)
                        {
                            int cheer = int.Parse(version["id"]);
                            string imageUrl = version["image_url_1x"];
                            string title = version["title"];
                            string description = version["description"];
                            string clickUrl = version["click_url"];
                            string tooltipurl = version["image_url_4x"];
                            LazyLoadedImage cheerbadge = new LazyLoadedImage
                            {
                                Name = title,
                                Url = imageUrl,
                                Tooltip = title,
                                TooltipImageUrl = tooltipurl,
                                click_url = clickUrl
                            };

                            CheerBadges.AddOrUpdate(cheer, cheerbadge, (key, oldvalue) => cheerbadge);
                        }
                    }
                }
            }
            catch ( Exception e)
            {
                GuiEngine.Current.log("Generic Exception Handler: " + "room " + RoomID + " " + e.ToString());
            }
        }

        public void PubSub ()
        {
            
        }

        public bool IsBroadcaster
        {
            get
            {
                if (IrcManager.Account.IsAnon)
                    return false;

                return string.Equals(Name, IrcManager.Account.Username, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        protected void loadData()
        {
            loadRoomID();
        }

        protected void loadRoomID()
        {
            // Try to load from cache
            RoomID = Cache.RoomIdCache.Get(Name);

            if (RoomID == -1)
            {
                // No room ID was saved in the cache

                if (loadRoomIDFromTwitch())
                {
                    // Successfully got a room ID from twitch
                    Cache.RoomIdCache.Set(Name, RoomID);
                }
            }
        }

        protected bool loadRoomIDFromTwitch()
        {
            // call twitch api
            try
            {
                var request =
                    WebRequest.Create(
                        $"https://api.twitch.tv/helix/users?&login={Name}");
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                request.Headers["Authorization"]=$"Bearer {IrcManager.Account.OauthToken}";
                request.Headers["Client-ID"]=$"{IrcManager.DefaultClientID}";
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream())
                    {
                        var parser = new JsonParser();

                        dynamic json = parser.Parse(stream);
                        dynamic users = json["data"];
                        int roomID = -1;

                        if (users.Count>0 && int.TryParse(users[0]["id"], out roomID))
                        {
                            RoomID = roomID;
                            return true;
                        }
                    }
                    response.Close();
                }
            }
            catch
            {
            }

            return false;
        }
        private void OnRefreshTimerElapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer sourcetmr = (System.Timers.Timer)source;
            try {
                Join();
                connected = true;
                sourcetmr.Elapsed -= this.OnRefreshTimerElapsed;
                sourcetmr.Close();
            } catch (Exception ex) {
                sourcetmr.Interval = sourcetmr.Interval*2;
            }
        }
        
        private void loadFFZEmotesFromFile() {
            try {
                var ffzChannelEmotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"ffz_channel_{RoomID}");
                var parser = new JsonParser();
                if (File.Exists(ffzChannelEmotesCache)) {
                    using (var stream = File.OpenRead(ffzChannelEmotesCache))
                    {
                        dynamic json = parser.Parse(stream);

                        dynamic room = json["room"];

                        try
                        {
                            if (room.ContainsKey("vip_badge")) {
                                dynamic vip_badge = room["vip_badge"];
                                if (vip_badge != null) {
                                    string url = vip_badge["1"];
                                    string tooltipurl = vip_badge["4"];
                                    if (!string.IsNullOrWhiteSpace(url)) {
                                        url = Emotes.fixFFZUrl(url);
                                        if (string.IsNullOrWhiteSpace(tooltipurl)) {
                                            tooltipurl = url;
                                        } else {
                                            tooltipurl = Emotes.fixFFZUrl(tooltipurl);
                                        }
                                        VipBadge = new LazyLoadedImage {
                                            Url = url,
                                            TooltipImageUrl = tooltipurl,
                                            Tooltip = "custom vip badge\nFFZ"
                                        };
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.Message.Log("emotes");
                            GuiEngine.Current.log(e.ToString());
                        }
                        try
                        {
                            object moderator;
                            
                            if (room.TryGetValue("moderator_badge", out moderator))
                            {
                                if (moderator != null && !string.IsNullOrWhiteSpace((string)moderator))
                                {
                                    var url = Emotes.fixFFZUrl((moderator as string));
                                    string tooltipurl = "";
                                    LazyLoadedImage tooltipImage = null;
                                    if (room.ContainsKey("mod_urls")) {
                                        dynamic mod_urls = room["mod_urls"];
                                        if (mod_urls != null) {
                                            if (mod_urls.ContainsKey("4"))
                                            {
                                                tooltipurl = mod_urls["4"];
                                            } else if (mod_urls.ContainsKey("2"))
                                            {
                                                tooltipurl = mod_urls["2"];
                                            }
                                            if (!string.IsNullOrWhiteSpace(tooltipurl)) {
                                                tooltipurl = Emotes.fixFFZUrl(tooltipurl);
                                            }
                                        }
                                    }
                                    if (string.IsNullOrWhiteSpace(tooltipurl)){
                                        tooltipurl = url;
                                    }
                                    
                                    tooltipImage = new LazyLoadedImage {
                                        Url = tooltipurl,
                                        LoadAction = () =>
                                        {
                                            try
                                            {
                                                ChatterinoImage img;

                                                var request = WebRequest.Create(tooltipurl);
                                                if (AppSettings.IgnoreSystemProxy)
                                                {
                                                    request.Proxy = null;
                                                }
                                                using (var response = request.GetResponse()){
                                                    using (var s = response.GetResponseStream())
                                                    {
                                                        MemoryStream mem = new MemoryStream();
                                                        s.CopyTo(mem);
                                                        img = GuiEngine.Current.ReadImageFromStream(mem);
                                                    }
                                                    response.Close();
                                                }

                                                GuiEngine.Current.FreezeImage(img);

                                                return GuiEngine.Current.DrawImageBackground(img, HSLColor.FromRGB(0x45A41E));
                                            }
                                            catch (Exception e)
                                            {
                                                e.Message.Log("emotes");
                                                return null;
                                            }
                                        }
                                    };
                                    ModeratorBadge = new LazyLoadedImage
                                    {
                                        Url = url,
                                        Tooltip = "custom moderator badge\nFFZ",
                                        TooltipImageUrl = tooltipurl,
                                        TooltipImage = tooltipImage,
                                        LoadAction = () =>
                                        {
                                            try
                                            {
                                                ChatterinoImage img;

                                                var request = WebRequest.Create(url);
                                                if (AppSettings.IgnoreSystemProxy)
                                                {
                                                    request.Proxy = null;
                                                }
                                                using (var response = request.GetResponse()){
                                                    using (var s = response.GetResponseStream())
                                                    {
                                                        MemoryStream mem = new MemoryStream();
                                                        s.CopyTo(mem);
                                                        img = GuiEngine.Current.ReadImageFromStream(mem);
                                                    }
                                                    response.Close();
                                                }

                                                GuiEngine.Current.FreezeImage(img);

                                                return GuiEngine.Current.DrawImageBackground(img, HSLColor.FromRGB(0x45A41E));
                                            }
                                            catch (Exception e)
                                            {
                                                e.Message.Log("emotes");
                                                return null;
                                            }
                                        }
                                    };
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            e.Message.Log("emotes");
                            GuiEngine.Current.log(e.ToString());
                        }

                        dynamic sets = json["sets"];

                        FfzChannelEmotes.Clear();

                        foreach (var set in sets.Values)
                        {
                            string title = set["title"];

                            dynamic emoticons = set["emoticons"];

                            foreach (LazyLoadedImage emote in Emotes.GetFfzEmoteFromDynamic(emoticons, false))
                            {
                                FfzChannelEmotes[emote.Name] = emote;
                            }
                        }
                    }
                }
            } catch (Exception e) {
                
            }
        }
        
        private void load7tvEmotesFromFile() {
            try {
                var SeventvChannelEmotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"7tv_channel_{RoomID}");
                var parser = new JsonParser();
                if (File.Exists(SeventvChannelEmotesCache)) {
                    using (var stream = File.OpenRead(SeventvChannelEmotesCache))
                    {
                        if (stream == null || stream.Length == 0) {
                            return;
                        }
                        dynamic json = parser.Parse(stream);

                        SeventvChannelEmotes.Clear();
                        
                        string emotename;
                        string emoteid;
                        dynamic owner;
                        string ownername;
                        bool getemote;
                        double fake;
                        double scale;
                        string tooltipurl;
                        string url;
                        bool zeroWidth = false;
                        string visibility;
                        int visibilityFlags;
                        const int zeroWidthFlag = 0x100;
                        LazyLoadedImage emote;

                        var emote_set = json["emote_set"];
                        if (emote_set == null || emote_set.ContainsKey("emotes") != true) {
                            return;
                        }
                        var emotes = emote_set["emotes"];
                        foreach (var e in emotes)
                        {
                            var data = e["data"];
                            var strflags = e["flags"];
                            emotename = e["name"];
                            emoteid = data["id"];
                            var host = data["host"];
                            var files = host["files"];
                            var urls = files;

                            int maxScale = 1;
                            string urlX1 = null;
                            string urlX2 = null;
                            string urlX4 = null;
                            string baseUrl = Emotes.fixFFZUrl(host["url"]);
                            foreach (var file in files) {
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

                            url = Emotes.getUrlFromScale(urlX1, urlX2, urlX4, AppSettings.EmoteScale, maxScale, out scale);

                            tooltipurl = Emotes.getUrlFromScale(urlX1, urlX2, urlX4, 4, maxScale, out fake);
                            owner = data.ContainsKey("owner") ? data["owner"] : null;
                            visibility = data["flags"];
                            int flags = 0;
                            visibilityFlags = 0;
                            if (!string.IsNullOrEmpty(strflags)) {
                                int.TryParse(strflags, out flags);
                            }
                            if (!string.IsNullOrEmpty(visibility)) {
                                int.TryParse(visibility, out visibilityFlags);
                            }
                            //fuck you 7tv.
                            zeroWidth = (visibilityFlags & zeroWidthFlag) > 0 || (flags & 1) > 0;
                            ownername = "";
                            if (owner != null) {
                                ownername = owner["display_name"];
                                if (string.IsNullOrEmpty(ownername)) {
                                    ownername = owner["username"];
                                } else if (!string.IsNullOrEmpty(owner["username"]) && string.Compare(ownername.ToUpper(), owner["username"].ToUpper())!=0) {
                                    ownername = ownername + "(" + owner["username"] + ")";
                                }
                            }
                            getemote = Emotes.SeventvChannelEmotesCache.TryGetValue(emoteid, out emote);
                            if (getemote && Math.Abs(emote.Scale - scale) < .01 && emote.Name == emotename)
                            {
                                SeventvChannelEmotes[emotename] = emote;
                            }
                            else
                            {
                                
                                SeventvChannelEmotes[emotename] =
                                    new LazyLoadedImage
                                    {
                                        Name = emotename,
                                        Url = url,
                                        Tooltip = emotename + "\n7TV Channel Emote\nChannel: " + ownername,
                                        TooltipImageUrl = tooltipurl,
                                        Scale = scale,
                                        IsHat = zeroWidth,
                                        IsEmote = true
                                        
                                    };
                                if (!getemote || emote.Name == emotename) {
                                    Emotes.SeventvChannelEmotesCache[emoteid] = SeventvChannelEmotes[emotename];
                                }
                                emote = SeventvChannelEmotes[emotename];
                            }
                        }
                    } 
                }
            } catch (Exception e) {
                GuiEngine.Current.log(e.ToString());
            }
        }
        
        private void loadBTTVEmotesFromFile() {
            try {
                var bttvChannelEmotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"bttv_channel_{RoomID}");
                var parser = new JsonParser();
                
                if (File.Exists(bttvChannelEmotesCache)) {
                    using (var stream = File.OpenRead(bttvChannelEmotesCache))
                    {
                        dynamic json = parser.Parse(stream);
                        //var template = "https:" + json["urlTemplate"]; // urlTemplate is outdated, came from bttv v2 api, returned: //cdn.betterttv.net/emote/{{id}}/{{image}}
                        string template = "https://cdn.betterttv.net/emote/{{id}}/{{image}}";

                        BttvChannelEmotes.Clear();

                        foreach (var e in json["channelEmotes"])
                        {
                            string channel = Name;

                            AddBttvEmotes(template, e, channel);
                        }

                        foreach (var e in json["sharedEmotes"])
                        {
                            string channel = e["user"]["displayName"];

                            AddBttvEmotes(template, e, channel);
                        }
                    } 
                }
            } catch (Exception e) {
                
            }
        }
        
        //loads bttv and ffz emotes from cached file
        private void loadEmotesFromFile() {
            loadFFZEmotesFromFile();
            loadBTTVEmotesFromFile();
            load7tvEmotesFromFile();
            updateEmoteNameList();
        }

        // ctor
        private TwitchChannel(string channelName)
        {
            if (!channelName.StartsWith("/"))
            {
                
                Name = channelName.Trim('#');
                SubLink = $"https://www.twitch.tv/{Name}/subscribe?ref=in_chat_subscriber_link";
                ChannelLink = $"https://twitch.tv/{Name}";
                PopoutPlayerLink = $"https://player.twitch.tv/?channel={Name}&parent=chatterinoclassic";
                
                Task.Run(() =>
                {
                    loadData();
                    // recent chat
                    Task.Run(() =>
                    {
                        

                        if (RoomID != -1)
                        {
                            loadEmotesFromFile();
                            Task.Run(() =>
                            {
                                ReloadEmotes();
                            });
                            List<Message> message = LoadRecentMessages();
                            if (message != null) {
                                AddMessagesAtStart(message.ToArray());
                            }
                        }
                    });

                    checkIfIsLive();
                    
                    try {
                        Join();
                        connected = true;           
                    } catch (Exception e) {
                        GuiEngine.Current.log("error connecting to twitch " + channelName + " " + e.Message);
                        reconnectTryAgainTimer = new System.Timers.Timer(1000);
                        reconnectTryAgainTimer.Elapsed += this.OnRefreshTimerElapsed;
                    }
                });
            }

            Emotes.EmotesLoaded += Emotes_EmotesLoaded;
            IrcManager.Connected += IrcManager_Connected;
            IrcManager.Disconnected += IrcManager_Disconnected;
            IrcManager.NoticeAdded += IrcManager_NoticeAdded;
            AppSettings.MessageLimitChanged += AppSettings_MessageLimitChanged;
            AppSettings.FontChanged += AppSettings_FontChanged;
        }
        
        private List<Message> LoadRecentMessages() {
            if (RoomID != -1)
            {
                try
                {
                    LoadSubBadges(RoomID);
                    var messages = new List<Message>();

                    var request =
                        WebRequest.Create(
                            $"https://recent-messages.robotty.de/api/v2/recent-messages/{Name}");
                    if (AppSettings.IgnoreSystemProxy)
                    {
                        request.Proxy = null;
                    }
                    using (var response = request.GetResponse()) {
                        using (var stream = response.GetResponseStream())
                        {
                            var parser = new JsonParser();

                            dynamic json = parser.Parse(stream);

                            dynamic _messages = json["messages"];
                            
                            IrcMessage msg;
                            string sysMsg;
                            string login;
                            string displayname;
                            string giftlogin;
                            string giftdisplayname;
                            Message message;
                            string reason;
                            string duration;
                            int iduration;
                            HSLColor syscolor;
                            
                            foreach (string s in _messages)
                            {
                                
                                if (IrcMessage.TryParse(s, out msg))
                                {
                                    if (msg.Command == "ROOMSTATE" || msg.Command == "USERSTATE") {
                                        continue; //skip these messages
                                    } else if (msg.Command == "CLEARCHAT" && !string.IsNullOrWhiteSpace(msg.Params)) {
                                        msg.Tags.TryGetValue("ban-reason", out reason);

                                        iduration = 0;
                                        if (msg.Tags.TryGetValue("ban-duration", out duration))
                                        {
                                            int.TryParse(duration, out iduration);
                                        }
                                        message = new Message(
                                            $"{msg.Params} was timed out for {iduration} second{(iduration != 1 ? "s" : "")}",
                                            HSLColor.Gray, true);
                                        messages.Add(message);
                                    } else if (msg.Command == "CLEARMSG" && !string.IsNullOrWhiteSpace(msg.Params)) {
                                        msg.Tags.TryGetValue("target-msg-id", out string msgId);
                                        foreach(Message targetmsg in messages) {
                                            if (targetmsg.MessageId == msgId) {
                                                targetmsg.Disabled = true;
                                                messages.Add(new Message($"{targetmsg.Username}'s message has been deleted by a moderator.", HSLColor.Gray, true));
                                                break;
                                            }
                                        }
                                    } else if (msg.Command == "USERNOTICE") {
                                        msg.Tags.TryGetValue("system-msg", out sysMsg);
                                        msg.Tags.TryGetValue("msg-param-recipient-display-name", out giftdisplayname);
                                        msg.Tags.TryGetValue("display-name", out displayname);
                                        msg.Tags.TryGetValue("msg-param-recipient-user-name", out giftlogin);
                                        msg.Tags.TryGetValue("login", out login);
                                        msg.Tags.TryGetValue("msg-id", out string msgid);
                                        msg.Tags.TryGetValue("msg-param-color", out string msgcolor);
                                        syscolor = HSLColor.Gray;
                                        
                                        if (!string.IsNullOrEmpty(msgid) && msgid.Equals("announcement")) {
                                            sysMsg = "Announcement";
                                            if (!string.IsNullOrEmpty(msgcolor)){
                                                if (msgcolor.Equals("BLUE")) {
                                                    syscolor = HSLColor.Blue;
                                                } else if (msgcolor.Equals("ORANGE")) {
                                                    syscolor = HSLColor.Orange;
                                                } else if (msgcolor.Equals("PURPLE")) {
                                                    syscolor = HSLColor.Purple;
                                                } else if (msgcolor.Equals("GREEN")) {
                                                    syscolor = HSLColor.Green;
                                                }
                                            }
                                        } else {
                                            if (!string.IsNullOrEmpty(displayname)&&!string.IsNullOrEmpty(login)&&
                                                !string.Equals(displayname,login,StringComparison.OrdinalIgnoreCase)) {
                                                int index = sysMsg.IndexOf(displayname, StringComparison.OrdinalIgnoreCase);
                                                if (index != -1) {
                                                    index += displayname.Length;
                                                    sysMsg = sysMsg.Insert(index, " ("+login+")");
                                                }
                                            }
                                            if (!string.IsNullOrEmpty(giftdisplayname)&&!string.IsNullOrEmpty(giftlogin)&&
                                                !string.Equals(giftdisplayname,giftlogin,StringComparison.OrdinalIgnoreCase)) {
                                                int index = sysMsg.IndexOf(giftdisplayname, StringComparison.OrdinalIgnoreCase);
                                                if (index != -1) {
                                                    index += giftdisplayname.Length;
                                                    sysMsg = sysMsg.Insert(index, " ("+giftlogin+")");
                                                }
                                            }
                                        }
                                        message = new Message(sysMsg, syscolor, true)
                                        {
                                            HighlightType = HighlightType.Resub
                                        };
                                        messages.Add(message);
                                        if (!string.IsNullOrEmpty(msg.Params))
                                        {
                                            message = new Message(msg, this)
                                            {
                                                HighlightType = HighlightType.Resub
                                            };
                                            messages.Add(message);
                                        }
                                    } else if (msg.Tags.TryGetValue("pinned-chat-paid-canonical-amount", out string paidamount)) {
                                        msg.Tags.TryGetValue("pinned-chat-paid-currency", out string currency);
                                        msg.Tags.TryGetValue("login", out login);
                                        msg.Tags.TryGetValue("display-name", out displayname);
                                        string name = displayname ?? login;
                                        double paid = double.Parse(paidamount);
                                        if (!string.IsNullOrEmpty(displayname) && !string.IsNullOrEmpty(login) &&
                                                !string.Equals(displayname, login, StringComparison.OrdinalIgnoreCase)) {
                                                name = displayname + "(" + login + ")";
                                        }
                                        var sysMessage = new Message($"{name} elevated their chat message for {paid/100:F2}$ ({currency.ToLower()})", HSLColor.Gray, true)
                                        {
                                            HighlightType = HighlightType.Resub
                                        };
                                        messages.Add(sysMessage);
                                        if (!string.IsNullOrEmpty(msg.Params))
                                        {
                                            message = new Message(msg, this)
                                            {
                                                HighlightType = HighlightType.Resub
                                            };
                                            messages.Add(message);
                                        }
                                    } else {
                                        message = new Message(msg, this, isPastMessage: true) { HighlightTab = false };
                                        Users[message.Username.ToUpper()] = message.DisplayName;
                                        if (IrcManager.IsMessageIgnored(message, this) != true ) {
                                            if (message.ReplyBody != null && AppSettings.EnableReplys) {
                                                var sysMessage = new Message($"⤵️ Replying to @{message.ReplyUser}: {message.ReplyBody}", HSLColor.Gray, false, FontType.SmallItalic) {
                                                    HighlightType = HighlightType.None
                                                };
                                                messages.Add(sysMessage);
                                            }
                                            messages.Add(message);
                                        }
                                    }
                                    
                                    
                                }
                            }
                        }
                        response.Close();
                    }
                    return messages;
                }
                catch (Exception e)
                {
                    GuiEngine.Current.log(e.ToString());
                }
            }
            return null;
        }

        private void AppSettings_FontChanged(object sender, EventArgs e)
        {
            lock (MessageLock)
            {
                foreach (var msg in Messages)
                {
                    msg.InvalidateTextMeasurements();
                }
            }
        }

        private void IrcManager_Connected(object sender, EventArgs e)
        {
            lock (MessageLock)
            {
                if (Messages.Count != 0 && Messages.Last.Value.HighlightType.HasFlag(HighlightType.Disconnected))
                {
                    
                    Messages.Last.Value = new Message("reconnected to chat",
                        HSLColor.Gray, true)
                    {
                        HighlightTab = false,
                        HighlightType = HighlightType.Connected,
                    };
                    Task.Run(() =>
                    {
                        Rejoin();
                    });
                    Task.Run(() => ChatCleared?.Invoke(this, new ChatClearedEventArgs("", "", 0)));

                    return;
                }
            }
            Task.Run(() =>
            {
                Rejoin();
            });
            AddMessage(new Message("connected to chat", HSLColor.Gray, true)
            {
                HighlightTab = false,
                HighlightType = HighlightType.Disconnected,
            });
        }

        private void IrcManager_Disconnected(object sender, EventArgs e)
        {
            AddMessage(new Message("disconnected from chat", HSLColor.Gray, true)
            {
                HighlightTab = false,
                HighlightType = HighlightType.Disconnected
            });
        }

        private void IrcManager_NoticeAdded(object sender, ValueEventArgs<string> e)
        {
            AddMessage(new Message(e.Value, HSLColor.Gray, true) { HighlightTab = false });
        }

        private void AppSettings_MessageLimitChanged(object sender, EventArgs e)
        {
            List<Message> _messages = new List<Message>();

            lock (MessageLock)
            {
                maxMessages = AppSettings.ChatMessageLimit;
                while(Messages.Count > AppSettings.ChatMessageLimit)
                {
                    _messages.Add(Messages.First.Value);
                    Messages.RemoveFirst();
                }
            }

            if (_messages != null && _messages.Count > 0)
                MessagesRemovedAtStart?.Invoke(this, new ValueEventArgs<Message[]>(_messages.ToArray()));
        }

        private void Emotes_EmotesLoaded(object sender, EventArgs e)
        {
            updateEmoteNameList();
        }


        // Channels
        static TwitchChannel()
        {
            WhisperChannel?.AddMessage(
                new Message("Please note that chatterino can only read whispers while it is running!", null, true));
            WhisperChannel?.AddMessage(new Message("You can send whispers using the \"/w user message\" command!", null,
                true));
            MentionsChannel?.AddMessage(
                new Message("Please note that chatterino can only read mentions while it is running!", null, true));

            UpdateIsLiveTimer.Elapsed += UpdateIsLiveTimerElapsed;
            UpdateIsLiveTimer.Start();
            UpdateIsLiveTimerElapsed(null, null);
        }

        private void checkIfIsLive()
        {
            if (IrcManager.Account.IsAnon) { return; }
            if (RoomID != -1) {
                Task.Run(() =>
                {
                    try
                    {
                        var req =
                            WebRequest.Create(
                                $"https://api.twitch.tv/helix/streams?user_id={RoomID}");
                        if (AppSettings.IgnoreSystemProxy)
                        {
                            req.Proxy = null;
                        }
                        req.Headers["Authorization"]=$"Bearer {IrcManager.Account.OauthToken}";
                        req.Headers["Client-ID"]=$"{IrcManager.DefaultClientID}";
                        using (var res = req.GetResponse()) {
                            using (var resStream = res.GetResponseStream())
                            {
                                
                                var parser = new JsonParser();
                                dynamic json = parser.Parse(resStream);
                                dynamic data = json["data"];
                                var tmpIsLive = IsLive;
                                IsLive = data != null && data.Count > 0 && data[0]["type"]!="";
                                if (!IsLive)
                                {
                                    StreamViewerCount = 0;
                                    StreamStatus = null;
                                    StreamGame = null;

                                    if (tmpIsLive)
                                    {
                                        LiveStatusUpdated?.Invoke(this, new LiveStatusEventArgs(false));
                                    }
                                }
                                else
                                {
                                    dynamic stream = data[0];

                                    StreamViewerCount = int.Parse(stream["viewer_count"]);
                                    StreamStatus = stream["title"];
                                    StreamGame = stream["game_name"];
                                    StreamStart = DateTime.Parse(stream["started_at"]);
                                    LiveStatusUpdated?.Invoke(this, new LiveStatusEventArgs(tmpIsLive!=IsLive));
                                }
                            }
                            res.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        GuiEngine.Current.log("Generic Exception Handler: " + "room " + RoomID + " " + e.ToString());
                    }
                });
            }
        }

        private static void UpdateIsLiveTimerElapsed(object sender, ElapsedEventArgs args)
        {
            foreach (var channel in Channels)
            {
                channel.checkIfIsLive();
            }
        }


        private static readonly System.Timers.Timer UpdateIsLiveTimer = new System.Timers.Timer(1000 * 60);

        private static readonly ConcurrentDictionary<string, TwitchChannel> channels = new ConcurrentDictionary<string, TwitchChannel>();
        public static IEnumerable<TwitchChannel> Channels { get { return channels.Values; } }

        public static TwitchChannel WhisperChannel { get; private set; } = new TwitchChannel("/whispers");
        public static TwitchChannel MentionsChannel { get; private set; } = new TwitchChannel("/mentions");

        public static TwitchChannel AddChannel(string channelName)
        {
            if (channelName.StartsWith("/"))
            {
                switch (channelName)
                {
                    case "/whispers":
                        return WhisperChannel;

                    case "/mentions":
                        return MentionsChannel;
                }
            }

            return channels.AddOrUpdate((channelName ?? "").ToLower(), cname => new TwitchChannel(cname) { Uses = 1 }, (cname, c) => { c.Uses++; return c; });
        }

        public static void RemoveChannel(string channelName)
        {
            if (channelName == null)
                return;

            channelName = channelName.ToLower();

            TwitchChannel data;
            if (channels.TryGetValue(channelName ?? "", out data))
            {
                data.Uses--;
                if (data.Uses <= 0)
                {
                    data.Part();
                    data.Dispose();
                    channels.TryRemove(channelName ?? "", out data);
                }
            }
        }

        public static TwitchChannel GetChannel(string channelName)
        {
            channelName = channelName.ToLower();

            TwitchChannel data;
            if (channels.TryGetValue(channelName ?? "", out data))
                return data;

            return null;
        }


        // LazyLoadedImage + Name Autocompletion
        public ConcurrentDictionary<string, string> Users = new ConcurrentDictionary<string, string>();

        List<KeyValuePair<string, string>> emoteNames = new List<KeyValuePair<string, string>>();

        void updateEmoteNameList()
        {
            var names = new HashSet<KeyValuePair<string, string>>();
            if (AppSettings.ChatEnableTwitchEmotes) {
                names.UnionWith(Emotes.TwitchEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            }
            if (AppSettings.RecentlyUsedEmoteList) {
                names.UnionWith(Emotes.RecentlyUsedEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            }
            if (AppSettings.ChatEnableBttvEmotes) {
                names.UnionWith(Emotes.BttvGlobalEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
                names.UnionWith(BttvChannelEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            }
            if (AppSettings.ChatEnableFfzEmotes) {
                names.UnionWith(Emotes.FfzGlobalEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
                names.UnionWith(FfzChannelEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            }
            if (AppSettings.ChatEnable7tvEmotes) {
                names.UnionWith(Emotes.SeventvGlobalEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
                names.UnionWith(SeventvChannelEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            }
            if (AppSettings.ChatEnableEmojis) {
                names.UnionWith(Emojis.ShortCodeToEmoji.Keys.Select(x => new KeyValuePair<string, string>(":" + x.ToUpper() + ":", ":" + x + ":")));
            }
            
            if (IsFollowing) {
               names.UnionWith(FollowerEmotes.Keys.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            }

            emoteNames = new List<KeyValuePair<string, string>>(names);
            
        }

        public enum UsernameOrEmotes {
            Usernames = 0,
            Emotes = 1,
            Both = 2
        };
        public IEnumerable<KeyValuePair<string, string>> GetCompletionItems(bool firstWord, bool allowAt, UsernameOrEmotes usernamesoremotes)
        {
            var usernames = new List<KeyValuePair<string, string>>();

            if (AppSettings.ChatMentionUsersWithAt)
            {
                usernames.AddRange(Users.Select(x => new KeyValuePair<string, string>(x.Key, (allowAt ? "@" : "") + (!AppSettings.ChatTabLocalizedNames && !string.Equals(x.Value, x.Key, StringComparison.OrdinalIgnoreCase) ? x.Key.ToLower() : x.Value))));
                usernames.AddRange(Users.Select(x => new KeyValuePair<string, string>("@" + x.Key, "@" + (!AppSettings.ChatTabLocalizedNames && !string.Equals(x.Value, x.Key, StringComparison.OrdinalIgnoreCase) ? x.Key.ToLower() : x.Value))));
            }
            else
            {
                usernames.AddRange(Users.Select(x => new KeyValuePair<string, string>(x.Key, (!AppSettings.ChatTabLocalizedNames && !string.Equals(x.Value, x.Key, StringComparison.OrdinalIgnoreCase) ? x.Key.ToLower() : x.Value))));
                usernames.AddRange(Users.Select(x => new KeyValuePair<string, string>("@" + x.Key, "@" + (!AppSettings.ChatTabLocalizedNames && !string.Equals(x.Value, x.Key, StringComparison.OrdinalIgnoreCase) ? x.Key.ToLower() : x.Value))));
            }

            lock (Commands.CustomCommandsLock)
            {
                usernames.AddRange(Commands.CustomCommands.Select(x => new KeyValuePair<string, string>("/" + x.Name.ToUpper(), "/" + x.Name)));
            }

            usernames.AddRange(Commands.ChatCommands.Select(x => new KeyValuePair<string, string>("/" + x.Key.ToUpper(), "/" + x.Key)));

            usernames.AddRange(Commands.TwitchChatCommandNames.Select(x => new KeyValuePair<string, string>(x.ToUpper(), x)));
            
            if (usernamesoremotes == UsernameOrEmotes.Both) {
                if (AppSettings.PreferEmotesOverUsernames)
                {
                    usernames.Sort((x1, x2) => string.Compare(x1.Value, x2.Value));

                    var emotes = new List<KeyValuePair<string, string>>(emoteNames);

                    emotes.Sort((x1, x2) => string.Compare(x1.Value, x2.Value));

                    emotes.AddRange(usernames);

                    return emotes;
                }
                else
                {
                    usernames.AddRange(emoteNames);

                    usernames.Sort((x1, x2) => string.Compare(x1.Value, x2.Value));

                    return usernames;
                }
            } else if (usernamesoremotes == UsernameOrEmotes.Usernames) {
                usernames = new List<KeyValuePair<string, string>>(Users);
                usernames.Sort((x1, x2) => string.Compare(x1.Value, x2.Value));
                return usernames;
            } else if (usernamesoremotes == UsernameOrEmotes.Emotes) {
                var emotes = new List<KeyValuePair<string, string>>(emoteNames);
                emotes.Sort((x1, x2) => string.Compare(x1.Value, x2.Value));
                return emotes;
            }
            return null;
        }
        
        private bool isrejoining = false;
        public void Rejoin()
        {
            try
            {
                if (!isrejoining) {
                    isrejoining = true;
                    Part();
                    List<Message> messages = LoadRecentMessages();
                    if (messages != null) {
                        JoinMessagesAtEnd(messages.ToArray());
                    }
                    Join();
                    isrejoining = false;
                }
            } catch (Exception e) {
                GuiEngine.Current.log(e.ToString());
                isrejoining = false;
            }
        }
        public void Join()
        {
            AddMessage(new Message("Joining Channel", HSLColor.Gray, true) {
                HighlightTab = false,
            });
            if (SelectedChannel != this) {
                TwitchChannelJoiner.queueJoinChannel(Name, channelJoinCallback, null); 
            } else {
                TwitchChannelJoiner.queueJoinChannelFront(Name, channelJoinCallback, null); 
            }
        }
        
        private void channelJoinCallback(bool success, object callbackData) {
            if (success) {
                AddMessage(new Message("Channel Joined", HSLColor.Gray, true){
                    HighlightTab = false,
                });
            } else {
                AddMessage(new Message("Channel Failed To Join", HSLColor.Gray, true){
                    HighlightTab = false,
                });
            }
        }

        public void Part()
        {
            IrcManager.Client?.Part("#" + Name);
        }

        //static readonly Random ColorRandom = new Random();
        private static float usernameHue = 0;
        public void SendMessage(string text)
        {
            if (Name == null)
            {
                if (text.StartsWith("/w "))
                {
                    var channel = AllChannels.FirstOrDefault();

                    if (channel != null)
                    {
                        IrcManager.SendMessage(channel, text, IsModOrBroadcaster || IsVip);
                    }
                }

                return;
            }

            IrcManager.SendMessage(this, text, IsModOrBroadcaster || IsVip);
            
            if (AppSettings.Rainbow)
            {
                usernameHue += 0.05f;

                var color = new HSLColor(usernameHue % 1, 0.5f, 0.5f);

                IrcManager.SendMessage(this, $"/color {color.ToRGBHex()}", IsModOrBroadcaster || IsVip);
            }
        }

        [Obsolete("Twitch has killed this functionality. Hopefully some third party website will be made to replace it", true)]
        public void fetchUsernames()
        {
            try
            {
                var request = WebRequest.Create($"https://tmi.twitch.tv/group/user/{Name}/chatters");
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream())
                    {
                        var parser = new JsonParser();
                        dynamic json = parser.Parse(stream);
                        dynamic chatters = json["chatters"];

                        Users.Clear();

                        foreach (var group in chatters)
                        {
                            foreach (string user in group.Value)
                            {
                                Users[user.ToUpper()] = user;
                            }
                        }
                    }
                    response.Close();
                }
            }
            catch { }
        }

        // Messages
        public event EventHandler<ChatClearedEventArgs> ChatCleared;
        public event EventHandler<MessageAddedEventArgs> MessageAdded;
        public event EventHandler<ValueEventArgs<Message[]>> MessagesAddedAtStart;
        public event EventHandler<ValueEventArgs<Message[]>> MessagesRemovedAtStart;
        public event EventHandler<ValueEventArgs<Message[]>> MessagesAddedAtEnd;

        public int MessageCount { get; private set; } = 0;

        //private Message[] _messages = new Message[0];
        
        private LinkedList<Message> _messages = new LinkedList<Message>();

        public LinkedList<Message> Messages
        {
            get { return _messages; }
            set { _messages = value; }
        }

        public Message[] CloneMessages()
        {
            Message[] M;
            lock (MessageLock)
            {
                M = _messages.ToArray();
            }
            return M;
        }

        public object MessageLock { get; private set; } = new object();

        public static IEnumerable<TwitchChannel> AllChannels => Channels.Concat(new[] { WhisperChannel, MentionsChannel });

        public void ClearChat(bool removeMessages)
        {
            if (removeMessages)
            {
                var _messages = Messages;

                lock (MessageLock)
                {
                    Messages = new LinkedList<Message>();
                }

                MessagesRemovedAtStart?.Invoke(this, new ValueEventArgs<Message[]>(_messages.ToArray()));
            }
            else
            {
                lock (MessageLock)
                {
                    foreach (Message message in Messages)
                    {
                        message.Disabled = true;
                    }
                }

                AddMessage(new Message("Chat has been cleared by a moderator.", HSLColor.Gray, true));
            }
        }

        public void ClearMsg(string msgId) {
            Monitor.Enter(MessageLock);
            foreach (var msg in Messages)
            {
                if (msg.MessageId == msgId)
                {
                    msg.Disabled = true;
                    AddMessage(new Message($"{msg.Username}'s message has been deleted by a moderator.", HSLColor.Gray, true));
                    break;
                }
            }
            Monitor.Exit(MessageLock);
        }

        public void ClearChat(string user, string reason, int duration)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                ClearChat(false);
                return;
            }

            Monitor.Enter(MessageLock);

            foreach (var msg in Messages)
            {
                if (!msg.HasAnyHighlightType(HighlightType.Whisper) && msg.Username == user)
                {
                    msg.Disabled = true;
                }
            }
            if (Messages.Count > 0) {
                for (var i = Messages.Last; i != null; i = i.Previous)
                {
                    var m = i.Value;

                    if (m.ParseTime > DateTime.Now - TimeSpan.FromSeconds(30))
                    {
                        if (m.TimeoutUser != user) continue;

                        i.Value =
                            new Message(
                                $"{user} was timed out for {duration} second{(duration != 1 ? "s" : "")} (multiple times)",
                                HSLColor.Gray, true)
                            { TimeoutUser = user, Id = m.Id };
                        Monitor.Exit(MessageLock);

                        ChatCleared?.Invoke(this, new ChatClearedEventArgs(user, reason, duration));
                        return;
                    }

                    break;
                }
            }

            Monitor.Exit(MessageLock);

            AddMessage(
                new Message($"{user} was timed out for {duration} second{(duration != 1 ? "s" : "")}",
                    HSLColor.Gray, true)
                { TimeoutUser = user });

            ChatCleared?.Invoke(this, new ChatClearedEventArgs(user, reason, duration));
        }

        public void AddMessage(Message message)
        {
            Message removedMessage = null;

            lock (MessageLock)
            {
                if (Messages.Count == maxMessages)
                {
                    removedMessage = Messages.First.Value;
                    Messages.RemoveFirst();
                }
                
                Messages.AddLast(message);
                MessageCount = Messages.Count;
            }

            MessageAdded?.Invoke(this, new MessageAddedEventArgs(message, removedMessage));
        }

        public void AddMessagesAtStart(Message[] messages)
        {

            lock (MessageLock)
            {
                if (Messages.Count == maxMessages)
                    return;

                if (messages.Length + Messages.Count > maxMessages)
                {
                    var _messages = new Message[maxMessages - Messages.Count];
                    Array.Copy(messages, messages.Length - maxMessages + Messages.Count, _messages, 0, maxMessages - Messages.Count);
                    messages = _messages;
                }
                
                for (var i = messages.Length-1; i >=0 ; i--) {
                    Messages.AddFirst(messages[i]);
                }
                
                MessageCount = Messages.Count;
            }

            MessagesAddedAtStart?.Invoke(this, new ValueEventArgs<Message[]>(messages));
        }
        
        //joins new messages to the end of the list. it will move past the duplicates in the array to the new messages.
        public void JoinMessagesAtEnd(Message[] newmessages)
        {
            List<Message> messages_added = new List<Message>(newmessages.Length);
            List<Message> messages_removed = new List<Message>(newmessages.Length);
            lock (MessageLock)
            {
                Message m;
                bool messages_are_added = false;
                for (var i = Messages.Last; i != null && !messages_are_added; i = i.Previous) {
                    m = i.Value;
                    if (m.MessageId != null) {
                        for (int j = newmessages.Length - 1; j >= 0; j--) {
                            if (newmessages[j].MessageId != null && newmessages[j].MessageId.Equals(m.MessageId)) {
                                messages_are_added = true;
                                for (int k = j+1; k < newmessages.Length; k++) {
                                    Messages.AddLast(newmessages[k]);
                                    messages_added.Add(newmessages[k]);
                                }
                                break;
                            }
                        }
                    }
                }
                if (!messages_are_added) {
                    for (int i = 0; i < newmessages.Length; i++) {
                        Messages.AddLast(newmessages[i]);
                        messages_added.Add(newmessages[i]);
                    }
                }
                while (Messages.Count > maxMessages) {
                    messages_removed.Add(Messages.First.Value);
                    Messages.RemoveFirst();
                }
                MessageCount = Messages.Count;
            }
            MessagesRemovedAtStart?.Invoke(this, new ValueEventArgs<Message[]>(messages_removed.ToArray()));
            MessagesAddedAtEnd?.Invoke(this, new ValueEventArgs<Message[]>(messages_added.ToArray()));
        }

        public void Dispose()
        {
            Emotes.EmotesLoaded -= Emotes_EmotesLoaded;
            IrcManager.Connected -= IrcManager_Connected;
            IrcManager.Disconnected -= IrcManager_Disconnected;
            IrcManager.NoticeAdded -= IrcManager_NoticeAdded;
            AppSettings.MessageLimitChanged -= AppSettings_MessageLimitChanged;
        }

        public void ReloadEmotes()
        {
            if (RoomID != -1)
            {
                var channelName = Name;

                var bttvChannelEmotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"bttv_channel_{RoomID}");
                var ffzChannelEmotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"ffz_channel_{RoomID}");
                var seventvChannelEmotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"7tv_channel_{RoomID}");
                Task.Run(() =>
                {
                    LoadSubBadges(RoomID);
                });
                Task.Run(() =>
                {
                    LoadChannelBits(RoomID);
                });
                
                Task.Run(() =>
                {
                    LoadChannelEmotes(RoomID);
                });
                
                //Emotes.ClearTwitchEmoteCache();
                // bttv channel emotes
                Task.Run(() =>
                {
                    try {
                        var parser = new JsonParser();
                        if (Util.IsLinux) {
                            if (File.Exists(bttvChannelEmotesCache)) {
                                File.Delete(bttvChannelEmotesCache);
                            }
                            Util.LinuxDownloadFile("https://api.betterttv.net/3/cached/users/twitch/" + RoomID, bttvChannelEmotesCache);
                        } else {
                            using (var webClient = new WebClient())
                            using (var readStream = webClient.OpenRead("https://api.betterttv.net/3/cached/users/twitch/" + RoomID)) {
                                if (File.Exists(bttvChannelEmotesCache)) {
                                    File.Delete(bttvChannelEmotesCache);
                                }
                                using (var writeStream = File.OpenWrite(bttvChannelEmotesCache)) {
                                    readStream.CopyTo(writeStream);
                                }
                                readStream.Close();
                            }
                        }
                        loadBTTVEmotesFromFile();
                        updateEmoteNameList();
                    } catch (Exception e) {
                        e.Message.Log("emotes");
                    }
                });

                // ffz channel emotes
                Task.Run(() =>
                {
                    try
                    {
                        var parser = new JsonParser();

                        if (Util.IsLinux) {
                            if (File.Exists(ffzChannelEmotesCache)) {
                                File.Delete(ffzChannelEmotesCache);
                            }
                            Util.LinuxDownloadFile("https://api.frankerfacez.com/v1/room/id/" + RoomID, ffzChannelEmotesCache);
                        } else {
                            using (var webClient = new WebClient()) {
                                using (var readStream = webClient.OpenRead("https://api.frankerfacez.com/v1/room/id/" + RoomID)) {
                                    if (File.Exists(ffzChannelEmotesCache)) {
                                        File.Delete(ffzChannelEmotesCache);
                                    }
                                    using (var writeStream = File.OpenWrite(ffzChannelEmotesCache)) {
                                        readStream.CopyTo(writeStream);
                                    }
                                    readStream.Close();
                                }
                            }
                        }
                        loadFFZEmotesFromFile();
                        updateEmoteNameList();
                    }
                    catch (Exception e)
                    {
                        e.Message.Log("emotes");
                    }
                });
                
                //7tv channel emotes https://api.7tv.app/v3/users/twitch/{RoomID}
                Task.Run(() =>
                {
                    try
                    {
                        var parser = new JsonParser();
                        if (Util.IsLinux)
                        {
                            if (File.Exists(seventvChannelEmotesCache)) {
                                File.Delete(seventvChannelEmotesCache);
                            }
                            Util.LinuxDownloadFile($"https://7tv.io/v3/users/twitch/{RoomID}", seventvChannelEmotesCache);
                        }
                        else
                        {
                            using (var webClient = new WebClient()) {
                                using (var readStream = webClient.OpenRead($"https://7tv.io/v3/users/twitch/{RoomID}")) {
                                    if (File.Exists(seventvChannelEmotesCache)) {
                                        File.Delete(seventvChannelEmotesCache);
                                    }
                                    using (var writeStream = File.OpenWrite(seventvChannelEmotesCache))
                                    {
                                        readStream.CopyTo(writeStream);
                                    }
                                    readStream.Close();
                                }
                            }
                        }
                        load7tvEmotesFromFile();
                        updateEmoteNameList();
                    }
                    catch (Exception e)
                    {
                        e.Message.Log("emotes");
                    }
                });
            }
        }

        // separate method to prevent redundant code after moving to bttv v3 api
        // call once for "channel emotes", and a separate time for "shared emotes"
        private void AddBttvEmotes(string template, dynamic e, string channel)
        {
            string id = e["id"];
            string code = e["code"];
            double scale;
            bool getemote;
            double fake;
            string tooltipurl;
            string url = template.Replace("{{id}}", id);     
            tooltipurl = Emotes.GetBttvEmoteLink(url, true, out fake);
            url = Emotes.GetBttvEmoteLink(url, false, out scale);
            LazyLoadedImage emote;
            
            getemote = Emotes.BttvChannelEmotesCache.TryGetValue(id, out emote);
            if (getemote && Math.Abs(emote.Scale - scale) < .01)
            {
                BttvChannelEmotes[code] = emote;
                if (emote.Name != code) {
                    emote.Name = code;
                    emote.Tooltip =  code + "\nBetterTTV Channel Emote\nChannel: " + channel;
                }
            }
            else
            {
                string imageType = e["imageType"];
                
                Emotes.BttvChannelEmotesCache[id] =
                    BttvChannelEmotes[code] =
                        new LazyLoadedImage
                        {
                            Name = code,
                            Url = url,
                            Tooltip = code + "\nBetterTTV Channel Emote\nChannel: " + channel,
                            TooltipImageUrl = tooltipurl,
                            Scale = scale,
                            IsEmote = true
                        };
            }
        }
    }
}
