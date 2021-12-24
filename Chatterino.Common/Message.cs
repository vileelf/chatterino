using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing;
using TwitchIrc;

namespace Chatterino.Common
{
    public class Message
    {
        public static bool EnablePings { get; set; } = true;

        public bool HighlightTab { get; set; } = true;
        

        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int TotalY { get; set; }
        public int Height { get; private set; }
        public int Width { get; set; } = 0;

        private static long _currentId = long.MinValue;
        // when replacing messages, the id should always stay the same
        public long Id { get; set; } = Interlocked.Increment(ref _currentId);

        public bool Disabled { get; set; } = false;
        public HighlightType HighlightType { get; set; } = HighlightType.None;
        public bool EmoteBoundsChanged { get; set; } = true;

        public string Username { get; set; }
        public string UserId { get; set; }
        public string Params { get; set; }
        public string MessageId { get; set; } = null;
        public Word UsernameWord { get; set; } = null;

        public string DisplayName { get; set; }
        public HSLColor UsernameColor { get; set; }

        public string TimeoutUser { get; set; } = null;
        public int TimeoutCount { get; set; } = 0;

        public MessageBadges Badges { get; set; }

        //private bool isVisible = false;

        //public bool IsVisible
        //{
        //    get { return isVisible; }
        //    set
        //    {
        //        isVisible = value;
        //        if (!value && buffer != null)
        //        {
        //            var b = buffer;
        //            buffer = null;
        //            GuiEngine.Current.DisposeMessageGraphicsBuffer(this);
        //        }
        //    }
        //}

        public object buffer = null;

        public string RawMessage { get; private set; }
        public List<Word> Words { get; set; }
        public TwitchChannel Channel { get; set; }

        private static Regex _linkRegex = new Regex(@"^((?<Protocol>\w+):\/\/)?(?<Domain>[\w%@-][\w.%-:@]+\w)\/?[\w\.?=#%&=\+\-@/$,\(\)]*$", RegexOptions.Compiled);
        private static char[] _linkIdentifiers = { '.', ':' };

        public DateTime ParseTime { get; set; }

        private Message()
        {

        }

        static CultureInfo enUS = new CultureInfo("en-US");
        
        //builds an array of indexes of the elements in string after being converted to utf32
        private int [] ParseUtf32index (string s) {
            List<int> ret = new List<int>(s.Length);
            int curchar;
            for (int i = 0; i<s.Length; i++) {
                curchar = char.ConvertToUtf32(s, i);
                ret.Add(i);
                if ( curchar > 0xFFFF ) { //curchar is bigger than a utf 16 char
                    i++; //skip the second part of curchar
                }
            }
            return ret.ToArray();
        }

        public Message(IrcMessage data, TwitchChannel channel, bool enableTimestamp = true, bool enablePingSound = true,
            bool isReceivedWhisper = false, bool isSentWhisper = false, bool includeChannel = false, bool isPastMessage = false)
        {
            //var w = Stopwatch.StartNew();

            ParseTime = DateTime.Now;
            Channel = channel;

            List<Word> words = new List<Word>();
            string value;

            string text = data.Params ?? "";
            Params = text;
            Username = data.PrefixNickname ?? "";

            if (string.IsNullOrWhiteSpace(Username))
            {
                string login;

                if (data.Tags.TryGetValue("login", out login))
                {
                    Username = login;
                }
            }
            
            if (data.Tags.TryGetValue("user-id", out value))
            {
                UserId = value;
            }
            
            if (data.Tags.TryGetValue("id", out value))
            {
                MessageId = value;
            }
            
            var slashMe = false;

            // Handle /me messages
            if (text.Length > 8 && text.StartsWith("\u0001ACTION "))
            {
                text = text.Substring("\u0001ACTION ".Length, text.Length - "\u0001ACTION ".Length - 1);
                slashMe = true;
            }

            if (data.Tags.TryGetValue("display-name", out value))
            {
                DisplayName = value;
            }

            // Username
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = Username;
            }
            // Highlights
            if (!IrcManager.Account.IsAnon)
            {
                if ((AppSettings.ChatEnableHighlight || AppSettings.ChatEnableHighlightSound ||
                     AppSettings.ChatEnableHighlightTaskbar) && Username != IrcManager.Account.Username.ToLower())
                {
                    if (!AppSettings.HighlightIgnoredUsers.ContainsKey(Username))
                    {
                        if (!IrcManager.IgnoredUsers.Contains(Username))
                        {
                            if (AppSettings.CustomHighlightRegex != null &&
                                AppSettings.CustomHighlightRegex.IsMatch(text))
                            {
                                if (AppSettings.ChatEnableHighlight)
                                {
                                    HighlightType = HighlightType.Highlighted;
                                }

                                if (EnablePings && enablePingSound)
                                {
                                    if (AppSettings.ChatEnableHighlightSound)
                                        GuiEngine.Current.PlaySound(NotificationSound.Ping);
                                    if (AppSettings.ChatEnableHighlightTaskbar)
                                        GuiEngine.Current.FlashTaskbar();
                                }
                            } else if (AppSettings.HighlightUserNames != null &&
                            (AppSettings.HighlightUserNames.ContainsKey(Username)
                            || AppSettings.HighlightUserNames.ContainsKey(DisplayName))) {
                                if (AppSettings.ChatEnableHighlight)
                                {
                                    HighlightType = HighlightType.UsernameHighlighted;
                                }
                            }
                        }
                    }
                }
            }

            // Tags
            if (data.Tags.TryGetValue("color", out value))
            {
                try
                {
                    if (value.Length == 7 && value[0] == '#')
                    {
                        UsernameColor = HSLColor.FromRGB(Convert.ToInt32(value.Substring(1), 16));
                    }
                }
                catch { }
            }

            // Bits
            string bits = null;

            data.Tags.TryGetValue("bits", out bits);

            // Add timestamp
            string timestampTag;
            string timestamp = null;

            string tmiTimestamp;
            long tmiTimestampInt;

            DateTime timestampTime = DateTime.Now;

            if (data.Tags.TryGetValue("tmi-sent-ts", out tmiTimestamp))
            {

                if (long.TryParse(tmiTimestamp, out tmiTimestampInt))
                {
                    var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

                    var time = dtDateTime.AddSeconds(tmiTimestampInt / 1000).ToLocalTime();

                    timestampTime = time;

                    //timestamp = time.ToString(AppSettings.ChatShowTimestampSeconds ? "HH:mm:ss" : "HH:mm");
                    //enableTimestamp = true;
                }
            }
            else if (data.Tags.TryGetValue("timestamp-utc", out timestampTag))
            {
                DateTime time;
                if (DateTime.TryParseExact(timestampTag, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time))
                {
                    timestampTime = time;
                    //timestamp = time.ToString(AppSettings.ChatShowTimestampSeconds ? "HH:mm:ss" : "HH:mm");
                    //enableTimestamp = true;
                }
            }

            if (enableTimestamp && AppSettings.ChatShowTimestamps)
            {
                string timestampFormat;

                if (AppSettings.ChatShowTimestampSeconds)
                {
                    timestampFormat = AppSettings.TimestampsAmPm ? "hh:mm:ss tt" : "HH:mm:ss";
                }
                else
                {
                    timestampFormat = AppSettings.TimestampsAmPm ? "hh:mm tt" : "HH:mm";
                }

                timestamp = timestampTime.ToString(timestampFormat, enUS);

                words.Add(new Word
                {
                    Type = SpanType.Text,
                    Value = timestamp,
                    Color = HSLColor.FromRGB(-8355712),
                    Font = FontType.Small,
                    CopyText = timestamp
                });
            }

            // add channel name
            if (includeChannel)
            {
                words.Add(new Word
                {
                    Type = SpanType.Text,
                    Link = new Link(LinkType.ShowChannel, channel.Name),
                    Value = "#" + channel.Name,
                    Color = HSLColor.FromRGB(-8355712),
                    Font = FontType.Medium,
                    CopyText = "#" + channel.Name
                });
            }

            // add moderation buttons
            if (channel.IsModOrBroadcaster && !string.Equals(IrcManager.Account.Username, Username))
            {
                string badges;
                if (data.Tags.TryGetValue("badges", out badges))
                {
                    if (badges.Contains("broadcaster"))
                        goto aftermod;

                    if (badges.Contains("moderator") && !channel.IsBroadcaster)
                        goto aftermod;
                }

                if (AppSettings.EnableBanButton)
                {
                    words.Add(new Word
                    {
                        Type = SpanType.LazyLoadedImage,
                        Value = new LazyLoadedImage(GuiEngine.Current.GetImage(ImageType.Ban)),
                        Link = new Link(LinkType.BanUser, Tuple.Create(Username, channel.Name)),
                        Tooltip = "Ban User"
                    });
                }

                if (AppSettings.EnableTimeoutButton)
                {
                    foreach (var amount in AppSettings.TimeoutButtons)
                    {
                        int tmpAmount = amount;
                        string tooltip = "";

                        if (tmpAmount > 60 * 60 * 24)
                        {
                            tooltip += tmpAmount / (60 * 60 * 24) + " days ";
                            tmpAmount %= 60 * 60 * 24;
                        }
                        if (tmpAmount > 60 * 60)
                        {
                            tooltip += tmpAmount / (60 * 60) + " hours ";
                            tmpAmount %= 60 * 60;
                        }
                        if (tmpAmount > 60)
                        {
                            tooltip += tmpAmount / (60) + " mins ";
                            tmpAmount %= 60;
                        }
                        if (tmpAmount > 0)
                        {
                            tooltip += tmpAmount + " secs ";
                        }

                        ChatterinoImage image;
                        if (AppSettings.TimeoutButtons.Count > 1)
                        {
                            image = ((dynamic)GuiEngine.Current).GetImageForTimeout(amount);
                        }
                        else
                        {
                            image = GuiEngine.Current.GetImage(ImageType.Timeout);
                        }

                        words.Add(new Word
                        {
                            Type = SpanType.LazyLoadedImage,
                            Value = new LazyLoadedImage(image),
                            Link = new Link(LinkType.TimeoutUser, Tuple.Create(Username, channel.Name, amount)),
                            Tooltip = $"Timeout User ({tooltip.Trim()})"
                        });
                    }
                }
            }
            aftermod:

            // get badges from tags
            if (data.Tags.TryGetValue("badges", out value))
            {
                var badges = value.Split(',');
                LazyLoadedImage image;
                foreach (var badge in badges)
                {
                    if (badge.StartsWith("subscriber/"))
                    {
                        try
                        {
                            int n = int.Parse(badge.Substring("subscriber/".Length));

                            Badges |= MessageBadges.Sub;
                            image = channel.GetSubscriberBadge(n);
                            if (image!=null) {
                                words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = image, Link = new Link(LinkType.Url, Channel.SubLink), Tooltip = image.Tooltip, TooltipImageUrl = image.TooltipImageUrl, TooltipImage = image.TooltipImage });
                            } else {
                                image = GuiEngine.Current.GetBadge(badge);
                                if (image != null) {
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = image, Link = new Link(LinkType.Url, image.click_url), Tooltip = image.Tooltip, TooltipImageUrl = image.TooltipImageUrl, TooltipImage = image.TooltipImage });
                                }
                            }
                        }
                        catch { }
                    } else if (badge.Equals("moderator/1") && channel.ModeratorBadge != null) {
                        words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = channel.ModeratorBadge, Tooltip = channel.ModeratorBadge.Tooltip, TooltipImageUrl = channel.ModeratorBadge.TooltipImageUrl, TooltipImage =  channel.ModeratorBadge.TooltipImage});
                    } else if (badge.Equals("vip/1") && channel.VipBadge != null) {
                        words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = channel.VipBadge, Tooltip = channel.VipBadge.Tooltip, TooltipImageUrl = channel.VipBadge.TooltipImageUrl, TooltipImage =  channel.VipBadge.TooltipImage});
                    } else if (badge.StartsWith("bits/")) {
                        try {
                            int n = int.Parse(badge.Substring("bits/".Length));

                            image = channel.GetCheerBadge(n);
                            if (image!=null) {
                                words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = image, Link = new Link(LinkType.Url, image.click_url), Tooltip = image.Tooltip, TooltipImageUrl = image.TooltipImageUrl, TooltipImage = image.TooltipImage });
                            } else {
                                image = GuiEngine.Current.GetBadge(badge);
                                if (image != null) {
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = image, Link = new Link(LinkType.Url, image.click_url), Tooltip = image.Tooltip, TooltipImageUrl = image.TooltipImageUrl, TooltipImage = image.TooltipImage });
                                }
                            }
                        } catch {}
                    } else {
                        image = GuiEngine.Current.GetBadge(badge);
                        if (image != null) {
                            words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = image, Link = new Link(LinkType.Url, image.click_url), Tooltip = image.Tooltip, TooltipImageUrl = image.TooltipImageUrl, TooltipImage = image.TooltipImage });
                        }
                    }
                }
            }

            var messageUser = (isSentWhisper ? IrcManager.Account.Username + " -> " : "");

            messageUser += DisplayName;

            if (!isReceivedWhisper && !isSentWhisper)
            {
                messageUser += (DisplayName.ToLower() != Username ? $" ({Username})" : "");
            }

            if (isReceivedWhisper)
            {
                messageUser += " -> " + IrcManager.Account.Username;
            }

            if (!slashMe)
            {
                messageUser += ":";
            }

            words.Add(new Word
            {
                Type = SpanType.Text,
                Value = messageUser,
                Color = UsernameColor,
                Font = FontType.MediumBold,
                Link = new Link(LinkType.UserInfo, new UserInfoData { UserName = Username, UserId = UserId, Channel = channel }),
                CopyText = messageUser
            });

            var twitchEmotes = new List<Tuple<int, LazyLoadedImage>>();
            
            if (data.Tags.TryGetValue("msg-id", out value)) {
                if (value.Contains("highlighted-message")) {
                    if (!isPastMessage) {
                        Message message;
                        if (AppSettings.HighlightHighlightedMessages) {
                            message = new Message("Highlighted Message", HSLColor.Gray, true)
                            {
                                HighlightType = HighlightType.HighlightedMessage
                            };
                        } else {
                            message = new Message("Highlighted Message", HSLColor.Gray, true);
                        }
                        channel.AddMessage(message);
                    }
                    if (AppSettings.HighlightHighlightedMessages) {
                        HighlightType = HighlightType.HighlightedMessage;
                    }
                }
            }
            // Twitch Emotes
            if (AppSettings.ChatEnableTwitchEmotes && data.Tags.TryGetValue("emotes", out value))
            {
                //  93064:0-6,8-14/80481:16-20,22-26
                value.Split('/').Do(emote =>
                {
                    if (emote != "")
                    {
                        string[] x = emote.Split(':');
                        try {
                            string id = x[0];
                            foreach (string y in x[1].Split(','))
                            {
                                string[] coords = y.Split('-');
                                int[] textElemIndex = ParseUtf32index(text);
                                int index = int.Parse(coords[0]);
                                if (textElemIndex.Length>0) {
                                    string name = text.Substring(textElemIndex[index], int.Parse(coords[1]) - index + 1);

                                    // ignore ignored emotes
                                    if (!AppSettings.ChatIgnoredEmotes.ContainsKey(name))
                                    {
                                        var e = Emotes.GetTwitchEmoteById(id, name);

                                        twitchEmotes.Add(Tuple.Create(index, e));
                                        
                                        if (AppSettings.RecentlyUsedEmoteList && Username.Equals(IrcManager.Account.Username.ToLower()) && !Emotes.TwitchEmotes.ContainsKey(name) && !Emotes.RecentlyUsedEmotes.ContainsKey(name)) {
                                            Emotes.RecentlyUsedEmotes.TryAdd(name, e);
                                            Emotes.EmoteAdded();
                                        }
                                    }
                                }
                            };
                        } catch(Exception e){
                            GuiEngine.Current.log("Generic Exception Handler: " + " " + emote + " " + x[0] + " " + e.ToString());
                        }
                    }
                });
                twitchEmotes.Sort((e1, e2) => e1.Item1.CompareTo(e2.Item1));
            }

            var i = 0;
            var currentTwitchEmoteIndex = 0;
            var currentTwitchEmote = twitchEmotes.FirstOrDefault();

            foreach (var split in text.Split(' '))
            {
                if (currentTwitchEmote != null)
                {
                    if (currentTwitchEmote.Item1 == i)
                    {
                        words.Add(new Word
                        {
                            Type = SpanType.LazyLoadedImage,
                            Value = currentTwitchEmote.Item2,
                            Link = new Link(LinkType.Url, currentTwitchEmote.Item2.Url),
                            Tooltip = currentTwitchEmote.Item2.Tooltip,
                            TooltipImageUrl = currentTwitchEmote.Item2.TooltipImageUrl,
                            TooltipImage = currentTwitchEmote.Item2.TooltipImage,
                            CopyText = currentTwitchEmote.Item2.Name
                        });
                        i += split.Length + 1;
                        currentTwitchEmoteIndex++;
                        currentTwitchEmote = currentTwitchEmoteIndex == twitchEmotes.Count ? null : twitchEmotes[currentTwitchEmoteIndex];
                        continue;
                    }
                }

                foreach (var o in Emojis.ParseEmojis(split))
                {
                    var s = o as string;

                    if (s != null)
                    {
                        Match m = Regex.Match(s, "([A-Za-z]+)([1-9][0-9]*)");
                        if (bits != null && m.Success)
                        {
                            try{
                                int cheer;
                                string prefix = m.Groups[1].Value;
                                string getcheer = m.Groups[2].Value;
                                if (int.TryParse(getcheer, out cheer))
                                {
                                    string color;
                                    string bitsLink;
                                    bool found = false;
                                    HSLColor bitsColor;
                                    LazyLoadedImage emote;
                                    if (!(found = channel.GetCheerEmote(prefix, cheer, !GuiEngine.Current.IsDarkTheme, out emote, out color))) {
                                        found = GuiEngine.Current.GetCheerEmote(prefix, cheer, !GuiEngine.Current.IsDarkTheme, out emote, out color);
                                    }
                                    if (found) {
                                        bitsColor = HSLColor.FromRGBHex(color);
                                        bitsLink = emote.Url;

                                        words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = Emotes.MiscEmotesByUrl.GetOrAdd(bitsLink, url => new LazyLoadedImage { Name = "cheer", Url = url, Tooltip = "Twitch Bits Badge" }), Tooltip = "Twitch Bits Donation", TooltipImageUrl = emote.TooltipImageUrl, TooltipImage = emote.TooltipImage, CopyText = s, Link = new Link(LinkType.Url, "https://blog.twitch.tv/introducing-cheering-celebrate-together-da62af41fac6") });
                                        words.Add(new Word { Type = SpanType.Text, Value = getcheer, Font = FontType.Small, Color = bitsColor });
                                    }
                                    continue;
                                }
                            } catch (Exception e) {
                                GuiEngine.Current.log("Generic Exception Handler: " + e.ToString());
                            }
                        }

                        LazyLoadedImage bttvEmote;
                        if (!AppSettings.ChatIgnoredEmotes.ContainsKey(s) && (AppSettings.ChatEnableBttvEmotes && (Emotes.BttvGlobalEmotes.TryGetValue(s, out bttvEmote) || channel.BttvChannelEmotes.TryGetValue(s, out bttvEmote))
                            || (AppSettings.ChatEnableFfzEmotes && (Emotes.FfzGlobalEmotes.TryGetValue(s, out bttvEmote) || channel.FfzChannelEmotes.TryGetValue(s, out bttvEmote)))
                            || (AppSettings.ChatEnable7tvEmotes && (Emotes.SeventvGlobalEmotes.TryGetValue(s, out bttvEmote) || channel.SeventvChannelEmotes.TryGetValue(s, out bttvEmote)))))
                        {
                            words.Add(new Word
                            {
                                Type = SpanType.LazyLoadedImage,
                                Value = bttvEmote,
                                Color = slashMe ? UsernameColor : new HSLColor?(),
                                Tooltip = bttvEmote.Tooltip,
                                TooltipImageUrl = bttvEmote.TooltipImageUrl,
                                TooltipImage = bttvEmote.TooltipImage,
                                Link = new Link(LinkType.Url, bttvEmote.TooltipImageUrl),
                                CopyText = bttvEmote.Name
                            });
                        }
                        else
                        {
                            var link = _matchLink(split);

                            words.Add(new Word
                            {
                                Type = SpanType.Text,
                                Value = s.Replace('﷽', '?'),
                                Color = slashMe ? UsernameColor : (link == null ? new HSLColor?() : HSLColor.FromRGB(-8355712)),
                                Link = link == null ? null : new Link(LinkType.Url, link),
                                CopyText = s
                            });
                        }
                    }
                    else
                    {
                        var e = o as LazyLoadedImage;

                        if (e != null)
                        {
                            words.Add(new Word
                            {
                                Type = SpanType.LazyLoadedImage,
                                Value = e,
                                Link = new Link(LinkType.Url, e.Url),
                                Tooltip = e.Tooltip,
                                TooltipImageUrl = e.TooltipImageUrl,
                                TooltipImage = e.TooltipImage,
                                CopyText = e.Name,
                                HasTrailingSpace = e.HasTrailingSpace
                            });
                        }
                    }
                }

                var splitLength = 0;
                for (var j = 0; j < split.Length; j++)
                {
                    splitLength++;

                    if (char.IsHighSurrogate(split[j]))
                        j += 1;
                }

                i += splitLength + 1;
            }

            Words = words;

            RawMessage = text;

            if (!isReceivedWhisper && AppSettings.HighlightIgnoredUsers.ContainsKey(Username))
            {
                HighlightTab = false;
            }

            //w.Stop();
            //Console.WriteLine("Message parsed in " + w.Elapsed.TotalSeconds.ToString("0.000000") + " seconds");
        }

        public Message(string text)
                : this(text, null, false)
        {

        }

        public Message(string text, HSLColor? color, bool addTimeStamp)
        {
            ParseTime = DateTime.Now;

            RawMessage = text;
            Words = new List<Word>();

            // Add timestamp
            if (addTimeStamp && AppSettings.ChatShowTimestamps)
            {
                //var timestamp = DateTime.Now.ToString(AppSettings.ChatShowTimestampSeconds ? "HH:mm:ss" : "HH:mm");
                string timestampFormat;

                if (AppSettings.ChatShowTimestampSeconds)
                {
                    timestampFormat = AppSettings.TimestampsAmPm ? "hh:mm:ss tt" : "HH:mm:ss";
                }
                else
                {
                    timestampFormat = AppSettings.TimestampsAmPm ? "hh:mm tt" : "HH:mm";
                }

                string timestamp = ParseTime.ToString(timestampFormat, enUS);

                Words.Add(new Word
                {
                    Type = SpanType.Text,
                    Value = timestamp,
                    Color = color ?? HSLColor.FromRGB(-8355712),
                    Font = FontType.Small,
                    CopyText = timestamp
                });
            }

            Words.AddRange(text.Split(' ').Select(x => _createWord(SpanType.Text, x.Replace('﷽', '?'), color)));
        }

        public Message(List<Word> words)
        {
            ParseTime = DateTime.Now;

            RawMessage = "";
            Words = words;
        }

        public override string ToString()
        {
            return Username + ": " + RawMessage;
        }

        public static Message[] ParseMarkdown(string md)
        {
            var list = new List<Message>();

            using (var reader = new StringReader(md))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    var msg = new Message();

                    var font = FontType.Medium;

                    // Heading
                    var headerMatch = Regex.Match(line, "^(#{1,6}) ");
                    if (headerMatch.Success)
                    {
                        if (headerMatch.Length == 2)
                            font = FontType.VeryLarge;
                        else
                            font = FontType.Large;

                        line = line.Substring(headerMatch.Length);
                    }

                    // Lists
                    if (line.Length > 2 && line[0] == '-' && line[1] == ' ')
                    {
                        line = " • " + line.Substring(2);
                    }

                    // Add words
                    msg.Words = line.Split(' ').Select(x =>
                    {
                        var link = _matchLink(x);

                        return new Word { Type = SpanType.Text, Font = font, Value = x, CopyText = x, Link = (link == null ? null : new Link(LinkType.Url, link)) };
                    }).ToList();

                    list.Add(msg);
                }
            }

            return list.ToArray();
        }

        public void InvalidateTextMeasurements()
        {
            measureText = true;
        }

        private bool measureText = true;
        private bool _measureImages = true;

        public bool HasAnyHighlightType(HighlightType type)
        {
            return (HighlightType & type) != HighlightType.None;
        }

        // return true if control needs to be redrawn
        public bool CalculateBounds(object graphics, int width, bool enableHatEmotes)
        {
            var emotesChanged = EmoteBoundsChanged;
            var redraw = false;

            var mediumTextLineHeight = GuiEngine.Current.MeasureStringSize(null, FontType.Medium, "X").Height;

            if (Width != width)
            {
                Width = width;
                redraw = true;
            }

            // check if any words need to be recalculated
            if (emotesChanged || measureText || _measureImages)
            {
                foreach (var word in Words)
                {
                    if (word.Type == SpanType.Text)
                    {
                        if (measureText)
                        {
                            var size = GuiEngine.Current.MeasureStringSize(graphics, word.Font, (string)word.Value);
                            word.Width = size.Width;
                            word.Height = size.Height;
                        }
                    }
                    else if (word.Type == SpanType.LazyLoadedImage)
                    {
                        if (emotesChanged || _measureImages)
                        {
                            var emote = word.Value as LazyLoadedImage;
                            var image = emote?.Image;
                            if (image == null)
                            {
                                word.Width = word.Height = 16;
                            }
                            else
                            {
                                var size = GuiEngine.Current.GetImageSize(image);

                                double w = size.Width, h = size.Height;

                                if (emote.IsEmote)
                                {
                                    if (AppSettings.EmoteScaleByLineHeight)
                                    {
                                        w = size.Width * ((float)mediumTextLineHeight / size.Height);
                                        h = mediumTextLineHeight;
                                    }
                                    else
                                    {
                                        w *= emote.Scale;
                                        h *= emote.Scale;
                                    }

                                    w = w * AppSettings.EmoteScale;
                                    h = h * AppSettings.EmoteScale;
                                }

                                word.Width = (int)w;
                                word.Height = (int)h;
                            }
                        }
                    }
                }

                measureText = false;
                _measureImages = false;

                redraw = true;
            }

            if (redraw)
            {
                var x = 0;
                var y = 4;
                var lineStartIndex = 0;

                var spaceWidth = GuiEngine.Current.MeasureStringSize(graphics, FontType.Medium, " ").Width;

                var i = 0;

                Func<int> fixCurrentLineHeight = () =>
                {
                    var lineHeight = 0;

                    for (var j = lineStartIndex; j < i; j++)
                    {
                        var h = Words[j].Height;
                        lineHeight = h > lineHeight ? h : lineHeight;
                    }

                    for (var j = lineStartIndex; j < i; j++)
                    {
                        var word = Words[j];
                        if (j == lineStartIndex && word.Type == SpanType.Text && word.SplitSegments != null)
                        {
                            var segment = word.SplitSegments[word.SplitSegments.Length - 1];
                            var rec = segment.Item2;
                            word.SplitSegments[word.SplitSegments.Length - 1] = Tuple.Create(segment.Item1, new CommonRectangle(rec.X, rec.Y + lineHeight - word.Height, rec.Width, rec.Height));
                        }
                        else
                        {
                            word.Y += lineHeight - word.Height;
                        }
                    }

                    return lineHeight;
                };

                for (; i < Words.Count; i++)
                {
                    var word = Words[i];

                    word.SplitSegments = null;

                    Margin margin = null;

                    if (word.Type == SpanType.LazyLoadedImage && enableHatEmotes && (i > 0 && Words[i - 1].Type != SpanType.Text))
                    {
                        var img = (LazyLoadedImage)word.Value;

                        if (img.IsHat)
                        {
#warning emote size
                            x -= word.Width + 2;
                        }
                        else if (img.Margin != null)
                        {
                            margin = img.Margin;
                        }
                    }

                    // word wrapped text
                    if (word.Width > width && word.Type == SpanType.Text && ((string)word.Value).Length > 2)
                    {
                        y += fixCurrentLineHeight();

                        lineStartIndex = i;

                        word.X = 0;
                        word.Y = y;

                        var text = (string)word.Value;
                        var startIndex = 0;
                        var items = new List<Tuple<string, CommonRectangle>>();

                        string s;

                        var widths = word.CharacterWidths;

                        // calculate word widths
                        if (widths == null)
                        {
                            widths = new int[text.Length];

                            var lastW = 0;
                            for (var j = 0; j < text.Length; j++)
                            {
                                var w = GuiEngine.Current.MeasureStringSize(graphics, word.Font, text.Remove(j)).Width;
                                widths[j] = w - lastW;
                                lastW = w;
                            }

                            word.CharacterWidths = widths;
                        }

                        // create word splits
                        {
                            var w = widths[0];

                            for (var j = 1; j < text.Length; j++)
                            {
                                w += widths[j];
                                if (w > width - spaceWidth - spaceWidth - spaceWidth)
                                {
                                    items.Add(Tuple.Create(text.Substring(startIndex, j - startIndex), new CommonRectangle(0, y, w, word.Height)));
                                    startIndex = j;
                                    y += word.Height;
                                    w = 0;
                                }
                            }

                            s = text.Substring(startIndex);

                            for (var j = startIndex; j < text.Length; j++)
                            {
                                w += widths[j];
                            }

                            items.Add(Tuple.Create(s, new CommonRectangle(0, y, w, word.Height)));
                        }

                        x = GuiEngine.Current.MeasureStringSize(graphics, word.Font, s).Width + spaceWidth;

                        if (items.Count > 1)
                            word.SplitSegments = items.ToArray();
                    }
                    // word in new line
                    else if (word.Width > width - x)
                    {
                        y += fixCurrentLineHeight();

                        word.X = 0 + (margin?.Left ?? 0);
                        word.Y = y + (margin?.Top ?? 0);

                        x = word.Width + spaceWidth + (margin == null ? 0 : (margin.Left + margin.Right));

                        lineStartIndex = i;
                    }
                    // word fits in current line
                    else
                    {
                        word.X = x + (margin?.Left ?? 0);
                        word.Y = y + (margin?.Top ?? 0);

                        x += word.Width + spaceWidth + (margin == null ? 0 : (margin.Left + margin.Right));
                    }
                }

                y += fixCurrentLineHeight();
                Height = y + 4;
            }

            if (redraw)
                buffer = null;

            return redraw;
        }

        public Word WordAtPoint(CommonPoint point)
        {
            for (var i = 0; i < Words.Count; i++)
            {
                var word = Words[i];
                if (word.Type == SpanType.Text && word.SplitSegments != null)
                {
                    if (word.SplitSegments.Any(x => x.Item2.Contains(point)))
                        return word;
                }
                else if (word.X < point.X && word.Y < point.Y && word.X + word.Width > point.X && word.Y + word.Height > point.Y)
                {
                    return word;
                }
            }

            return null;
        }

        public MessagePosition MessagePositionAtPoint(object graphics, CommonPoint point, int messageIndex)
        {
            var currentWord = 0;
            var currentChar = 0;
            var currentSplit = 0;

            var mediumTextLineHeight = GuiEngine.Current.MeasureStringSize(null, FontType.Medium, "X").Height;

            for (var i = 0; i < Words.Count; i++)
            {
                var word = Words[i];

                if (word.Type == SpanType.Text && word.SplitSegments != null)
                {
                    var splits = word.SplitSegments;

                    for (var j = 0; j < splits.Length; j++)
                    {
                        var split = splits[j];
                        if (point.Y > split.Item2.Y)
                        {
                            if (point.X > split.Item2.X)
                            {
                                currentSplit = j;
                                currentWord = i;
                            }
                        }
                    }
                }
                else
                {
                    if (point.Y > word.Y)
                    {
                        if (point.X > word.X)
                            currentWord = i;
                    }
                }
            }

            var w = Words[currentWord];
            var _currentSplit = 0;

            if (w.Type == SpanType.LazyLoadedImage)
            {
                var emote = (LazyLoadedImage)w.Value;

                var size = GuiEngine.Current.GetImageSize(emote.Image);

                double width = size.Width, h = size.Height;

                if (emote.IsEmote)
                {
                    if (AppSettings.EmoteScaleByLineHeight)
                    {
                        width = size.Width * ((float)mediumTextLineHeight / size.Height);
                        h = mediumTextLineHeight;
                    }
                    else
                    {
                        width *= emote.Scale;
                        h *= emote.Scale;
                    }

                    width = width * AppSettings.EmoteScale;
                    h = h * AppSettings.EmoteScale;
                }

                size = new CommonSize((int)width, (int)h);

                if (point.X - w.X > size.Width)
                    currentChar = 1;
            }
            else if (w.Type == SpanType.Text)
            {
                string text;
                CommonRectangle bounds;

                if (w.SplitSegments == null)
                {
                    text = (string)w.Value;
                    bounds = new CommonRectangle(w.X, w.Y, w.Width, w.Height);
                }
                else
                {
                    text = w.SplitSegments[currentSplit].Item1;
                    _currentSplit = currentSplit;
                    bounds = w.SplitSegments[currentSplit].Item2;
                }

                if (point.X > bounds.X + bounds.Width)
                {
                    currentChar = text.Length;
                }
                else
                {
                    for (var i = text.Length - 1; i >= 1; i--)
                    {
                        var s = text.Remove(i);

                        var size = GuiEngine.Current.MeasureStringSize(graphics, w.Font, s);

                        if (point.X - bounds.X > size.Width)
                        {
                            currentChar = s.Length;
                            break;
                        }
                    }
                }
            }

            return new MessagePosition(messageIndex, currentWord, _currentSplit, currentChar);
        }

        public MessagePosition PositionFromIndex(int index)
        {
            if (index == 0)
                return new MessagePosition(0, 0, 0, 0);

            var isFirstWord = true;

            var _index = 0;

            var i = 0;
            var j = 0;
            var k = 0;

            for (; i < Words.Count; i++)
            {
                var word = Words[i];

                j = 0;
                for (; j < (word.SplitSegments?.Length ?? 1); j++)
                {
                    var text = word.SplitSegments?[j].Item1 ?? (string)word.Value;

                    if (j == 0)
                        if (isFirstWord)
                            isFirstWord = false;
                        else
                            _index++;

                    k = 0;
                    for (; k < text.Length; k++)
                    {
                        if (_index == index)
                            return new MessagePosition(0, i, j, k);
                        _index++;
                    }
                }

                if (_index == index)
                    return new MessagePosition(0, i, j, k);
            }

            return new MessagePosition(0, i, j, k);
        }

        public int IndexFromPosition(MessagePosition pos)
        {
            var isFirstWord = true;

            var index = 0;

            for (var i = 0; i < pos.WordIndex; i++)
            {
                var word = Words[i];

                for (var j = 0; j < (word.SplitSegments?.Length ?? 1); j++)
                {
                    var text = word.SplitSegments?[j].Item1 ?? (string)word.Value;

                    if (j == 0)
                        if (isFirstWord)
                            isFirstWord = false;
                        else
                            index++;

                    for (var k = 0; k < text.Length; k++)
                    {
                        if (pos.WordIndex == i && pos.SplitIndex == j && pos.CharIndex == k)
                            return index;
                    }
                }
            }
            return 0;
        }

        // private
        private static Word _createWord(SpanType type, string text, HSLColor? color, string differentCopyText = null)
        {
            var link = _createLink(text);

            return new Word
            {
                Type = type,
                Value = text,
                Color = (link == null ? color : HSLColor.FromRGB(-8355712)),
                CopyText = differentCopyText ?? text,
                Link = link,
            };
        }

        // Try to create Link object from text, return null if no link was found, otherwise return new LinkType.Url Link
        private static Link _createLink(string text)
        {
            var url = _matchLink(text);
            if (url == null)
            {
                return null;
            }
            return new Link(LinkType.Url, url);
        }

        // Try to parse link from text, return null if no link was found, otherwise return the url parsed as a string
        private static string _matchLink(string text)
        {
            string link = null;

            if (text.IndexOfAny(_linkIdentifiers) != -1)
            {
                var m = _linkRegex.Match(text);

                if (m.Success)
                {
                    link = m.Value;

                    if (!m.Groups["Protocol"].Success)
                        link = "http://" + link;

                    if (!m.Groups["Protocol"].Success || m.Groups["Protocol"].Value.ToUpper() == "HTTP" || m.Groups["Protocol"].Value.ToUpper() == "HTTPS")
                    {
                        if (m.Groups["Domain"].Value.IndexOf('.') == -1)
                            link = null;
                    }
                }
            }

            return link;
        }
    }
}
