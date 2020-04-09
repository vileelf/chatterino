using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chatterino.Common;

namespace Chatterino.Controls
{
    public class EmoteList : MessageContainerControl
    {
        private List<Message> _messages = new List<Message>();

        protected override Message[] Messages
        {
            get
            {
                return _messages.ToArray();
            }
        }

        public EmoteList()
        {
            mouseScrollMultiplyer = 0.2;

            AllowMessageSeperator = false;
            EnableHatEmotes = false;
        }

        public void LoadChannel(TwitchChannel channel)
        {
            lock (MessageLock)
            {
                var messages = _messages;
                LazyLoadedImage twitchemote;
                messages.Clear();

                // recently used emotes
                if (AppSettings.RecentlyUsedEmoteList) {
                    var words = new List<Word>();

                    foreach (var emote in Emotes.RecentlyUsedEmotes.Values)
                    {
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
                    foreach (var emotes in Emotes.TwitchEmotes.GroupBy(x => x.Value.Set))
                    {
                        var words = new List<Word>();

                        foreach (var emote in emotes.OrderBy(x => x.Key))
                        {
                            twitchemote = Emotes.GetTwitchEmoteById(emote.Value.ID, emote.Key);
                            words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = twitchemote, Tooltip = twitchemote.Tooltip, TooltipImageUrl = twitchemote.TooltipImageUrl, CopyText = twitchemote.Name, Link = new Link(LinkType.InsertText, emote.Key + " ") });
                        }

                        if (words.Count != 0)
                        {
                            if (emotes.Key == 0)
                            {
                                messages.Add(new Message("Twitch Emotes"));
                            }
                            else
                            {
                                messages.Add(new Message("Twitch Subscriber Emotes"));
                            }

                            messages.Add(new Message(words));
                        }
                    }
                }

                // bttv channel emotes
                if (channel != null)
                {
                    var words = new List<Word>();

                    foreach (var emote in channel.BttvChannelEmotes.Values)
                    {
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
                        words.Add(new Word { Type = SpanType.LazyLoadedImage, Value = emote, Tooltip = emote.Tooltip, TooltipImageUrl = emote.TooltipImageUrl, CopyText = emote.Name, Link = new Link(LinkType.InsertText, emote.Name + " ") });
                    }

                    if (words.Count != 0)
                    {
                        messages.Add(new Message("FrankerFaceZ Global Emotes"));
                        messages.Add(new Message(words));
                    }
                }

                scrollAtBottom = false;
                _scroll.Value = 0;
                updateMessageBounds();
                Invalidate();
            }
        }
    }
}
