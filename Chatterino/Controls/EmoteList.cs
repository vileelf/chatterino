using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using Chatterino.Common;

namespace Chatterino.Controls
{
    public class EmoteList : MessageContainerControl
    {
        private List<Message> _messages = new List<Message>();
        
        public HashSet<LazyLoadedImage> GifEmotes = new HashSet<LazyLoadedImage>();
        
        public bool show_only_channel_emotes;

        public TwitchChannel Channel { get; set; }

        protected override Message[] Messages
        {
            get
            {
                return _messages.ToArray();
            }
        }

        public EmoteList()
        {
            mouseScrollMultiplier = 0.2;

            AllowMessageSeparator = false;
            EnableHatEmotes = false;
            Emotes.EmotesReLoaded += LoadChannel;
        }
        
        private void addGifEmote(LazyLoadedImage emote) {
            lock (GifEmotes) {
                GifEmotes.Add(emote);
            }
        }
        
        private void clearGifEmotes() {
            lock (GifEmotes) {
                GifEmotes.Clear();
            }
        }

        public void LoadChannel(object sender, EventArgs e) {
            if (Channel != null) {
                LoadChannel(Channel);
            }
        }

        public void LoadChannel(TwitchChannel channel)
        {
            Channel = channel;
            Task.Run(() =>
            {
                try {
                    lock (MessageLock)
                    {
                        var messages = _messages;
                        LazyLoadedImage twitchemote;
                        messages.Clear();
                        clearGifEmotes();
                        
                        if (!show_only_channel_emotes) {
                            // recently used emotes
                            if (AppSettings.RecentlyUsedEmoteList) {
                                var words = new List<Word>();

                                foreach (var emote in Emotes.RecentlyUsedEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("Recently Used Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }

                            // twitch emotes
                            {
                                foreach (var emotes in Emotes.TwitchEmotes.GroupBy(x => x.Value.OwnerID))
                                {
                                    var words = new List<Word>();

                                    foreach (var emote in emotes.OrderBy(x => x.Key))
                                    {
                                        twitchemote = Emotes.GetTwitchEmoteById(emote.Value.ID, emote.Key);
                                        if (twitchemote.IsAnimated) {
                                            addGifEmote(twitchemote);
                                        } else if (twitchemote.Image==null) {
                                            twitchemote.ImageLoaded += (s, e) => {
                                                if (twitchemote.IsAnimated) {
                                                    addGifEmote(twitchemote);
                                                }
                                            };
                                        }
                                        words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = twitchemote, Tooltip = twitchemote.Tooltip, TooltipImageUrl = twitchemote.TooltipImageUrl, CopyText = twitchemote.Name, Link = new Link(LinkType.InsertText, emote.Key + " ") });
                                    }
                                    var firstEmote = emotes.FirstOrDefault();

                                    if (words.Count != 0)
                                    {
                                        if (emotes.Key == "0" || emotes.Key == "")
                                        {
                                            messages.Add(new Message("Twitch Emotes"));
                                        }
                                        else
                                        {
                                            var channelName = firstEmote.Value.ChannelName;
                                            messages.Add(new Message($"{channelName ?? "Twitch"} Subscriber Emotes"));
                                        }

                                        messages.Add(new Message(words));
                                    }
                                }
                            }

                            // Follower emotes
                            if (channel != null && channel.IsFollowing) {
                                var words = new List<Word>();

                                foreach (var emote in channel.FollowerEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("Follower Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }
                            
                            // bttv channel emotes
                            if (channel != null)
                            {
                                var words = new List<Word>();

                                foreach (var emote in channel.BttvChannelEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("BetterTTV Channel Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }

                            // bttv global emotes
                            {
                                var words = new List<Word>();

                                foreach (var emote in Emotes.BttvGlobalEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("BetterTTV Global Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }

                            // ffz channel emotes
                            if (channel != null)
                            {
                                var words = new List<Word>();

                                foreach (var emote in channel.FfzChannelEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("FrankerFaceZ Channel Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }

                            // ffz global emotes
                            {
                                var words = new List<Word>();

                                foreach (var emote in Emotes.FfzGlobalEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("FrankerFaceZ Global Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }
                            
                             // 7tv channel emotes
                            {
                                var words = new List<Word>();

                                foreach (var emote in channel.SeventvChannelEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("7tv Channel Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }
                            
                            // 7tv global emotes
                            {
                                var words = new List<Word>();

                                foreach (var emote in Emotes.SeventvGlobalEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("7tv Global Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }
                        } else {
                            // Follower emotes
                            if (channel != null) {
                                var words = new List<Word>();

                                foreach (var emote in channel.FollowerEmotes.Values)
                                {
                                    if (emote.IsAnimated) {
                                        addGifEmote(emote);
                                    } else if (emote.Image==null) {
                                        emote.ImageLoaded += (s, e) => {
                                            if (emote.IsAnimated) {
                                                addGifEmote(emote);
                                            }
                                        };
                                    }
                                    words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                }

                                if (words.Count != 0)
                                {
                                    messages.Add(new Message("Follower Emotes"));
                                    messages.Add(new Message(words));
                                }
                            }
                            
                            // Channel emotes
                            if (channel != null) {
                                LazyLoadedImage emote;
                                foreach (var emotes in channel.ChannelEmotes.OrderBy(x => x.Value.EmoteInfo.type).ThenBy(x => x.Value.EmoteInfo.tier).GroupBy(x => new {x.Value.EmoteInfo.type, x.Value.EmoteInfo.tier}))
                                {
                                    var words = new List<Word>();

                                    foreach (var emotepair in emotes.OrderBy(x => x.Value.Name))
                                    {
                                        emote = emotepair.Value;
                                        if (emote.IsAnimated) {
                                            addGifEmote(emote);
                                        } else if (emote.Image==null) {
                                            emote.ImageLoaded += (s, e) => {
                                                if (emote.IsAnimated) {
                                                    addGifEmote(emote);
                                                }
                                            };
                                        }
                                        words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                                    }

                                    if (words.Count != 0)
                                    {
                                        if (((LazyLoadedImage)(words[0].Value)).EmoteInfo.type.Equals("bitstier")) {
                                            messages.Add(new Message("Bit Emotes"));
                                        } else if (((LazyLoadedImage)(words[0].Value)).EmoteInfo.type.Equals("subscriptions") && ((LazyLoadedImage)(words[0].Value)).EmoteInfo.tier.Equals("1000")){
                                            messages.Add(new Message("Tier 1 Emotes"));
                                        } else if (((LazyLoadedImage)(words[0].Value)).EmoteInfo.type.Equals("subscriptions") && ((LazyLoadedImage)(words[0].Value)).EmoteInfo.tier.Equals("2000")){
                                            messages.Add(new Message("Tier 2 Emotes"));
                                        } else if (((LazyLoadedImage)(words[0].Value)).EmoteInfo.type.Equals("subscriptions") && ((LazyLoadedImage)(words[0].Value)).EmoteInfo.tier.Equals("3000")){
                                            messages.Add(new Message("Tier 3 Emotes"));
                                        }
                                        messages.Add(new Message(words));
                                    }
                                }
                            }
                            
                        }
                    }
                    scrollAtBottom = false;
                    _scroll.Value = 0;
                    updateMessageBounds();
                    Invalidate();
                } catch (Exception e) {
                    GuiEngine.Current.log("Error loading emotelist " +e.ToString());
                }
            });
        }
    }
}
