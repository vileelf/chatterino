using Chatterino.Common;
using Chatterino.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;

namespace Chatterino
{
    public class WinformsGuiEngine : IGuiEngine
    {
        #if DEBUG
        private bool debug = true;
        #else
        private bool debug = false;
        #endif

        public bool GlobalEmotesLoaded {get; set;} = false;
        public bool GlobalBadgesLoaded { get; set;} = false;
        public  HashSet<LazyLoadedImage> GifEmotesOnScreen{get;} =  new HashSet<LazyLoadedImage>();
        public object GifEmotesLock{get;} =  new object();
        
        public WinformsGuiEngine()
        {
            AppSettings.FontChanged += (s, e) =>
            {
                gdiSizeCaches.Clear();
                dwSizeCaches.Clear();
            };
        }
        
        // LINKS
        public void HandleLink(Link _link)
        {
            switch (_link.Type)
            {
                case LinkType.Url:
                    {
                        var link = _link.Value as string;
                        try
                        {
                            if (link.StartsWith("http://") || link.StartsWith("https://")
                                || MessageBox.Show($"The link \"{link}\" will be opened in an external application.", "open link", MessageBoxButtons.OKCancel) == DialogResult.OK)
                                Process.Start(link);
                        }
                        catch { }
                    }
                    break;
                case LinkType.CloseCurrentSplit:
                    App.MainForm.RemoveSelectedSplit();
                    break;
                case LinkType.InsertText:
                    (App.MainForm.Selected as ChatControl)?.Input.Logic.InsertText(_link.Value as string);
                    break;
                case LinkType.UserInfo:
                    {
                        var data = (UserInfoData)_link.Value;

                        var popup = new UserInfoPopup(data)
                        {
                            StartPosition = FormStartPosition.Manual,
                            Location = Cursor.Position
                        };

                        popup.Show();

                        var screen = Screen.FromPoint(Cursor.Position);

                        int x = popup.Location.X, y = popup.Location.Y;

                        if (popup.Location.X < screen.WorkingArea.X)
                        {
                            x = screen.WorkingArea.X;
                        }
                        else if (popup.Location.X + popup.Width > screen.WorkingArea.Right)
                        {
                            x = screen.WorkingArea.Right - popup.Width;
                        }

                        if (popup.Location.Y < screen.WorkingArea.Y)
                        {
                            y = screen.WorkingArea.Y;
                        }
                        else if (popup.Location.Y + popup.Height > screen.WorkingArea.Bottom)
                        {
                            y = screen.WorkingArea.Bottom - popup.Height;
                        }

                        popup.Location = new Point(x, y);
                    }
                    break;
                case LinkType.ShowChannel:
                    {
                        var channelName = (string)_link.Value;

                        var widget = App.MainForm.TabControl.TabPages
                            .Where(x => x is ColumnTabPage)
                            .SelectMany(x => ((ColumnTabPage)x).Columns.SelectMany(y => y.Widgets))
                            .FirstOrDefault(
                                c => c is ChatControl && string.Equals(((ChatControl)c).ChannelName, channelName));

                        if (widget != null)
                        {
                            App.MainForm.TabControl.Select(widget.Parent as Controls.TabPage);
                            widget.Select();
                        }
                    }
                    break;
                case LinkType.TimeoutUser:
                    {
                        var tuple = _link.Value as Tuple<string, string, int>;

                        var channel = TwitchChannel.GetChannel(tuple.Item2);

                        if (channel != null)
                        {
                            channel.SendMessage($"/timeout {tuple.Item1} {tuple.Item3}");
                        }
                    }
                    break;
                case LinkType.BanUser:
                    {
                        var tuple = _link.Value as Tuple<string, string>;

                        var channel = TwitchChannel.GetChannel(tuple.Item2);

                        if (channel != null)
                        {
                            channel.SendMessage($"/ban {tuple.Item1}");
                        }
                    }
                    break;
            }
        }


        // SOUNDS
        SoundPlayer defaultHighlightSound = new SoundPlayer(Properties.Resources.ping2);

        public bool IsDarkTheme
        {
            get
            {
                return !App.ColorScheme.IsLightTheme;
            }
        }
        
        public NotificationSoundPlayer HighlightSound = new NotificationSoundPlayer(Path.Combine(Util.GetUserDataPath(), "Custom", "Ping.wav"));
        public NotificationSoundPlayer GoLiveSound = new NotificationSoundPlayer();

        public void UpdateSoundPaths() {
            GoLiveSound.SetPath(AppSettings.ChatCustomGoLiveSoundPath);
        }

        public void PlaySound(NotificationSound sound, bool forceCustom = false)
        {
            var focused = false;

            App.MainForm.Invoke(() => focused = App.MainForm.ContainsFocus);

            if (!focused)
            {
                bool soundPlayed = false;
                switch (sound) {
                    case NotificationSound.Ping:
                        if (forceCustom || AppSettings.ChatCustomHighlightSound)
                        {
                            soundPlayed = HighlightSound.Play();
                        }
                        break;
                    case NotificationSound.GoLive:
                        if (forceCustom || AppSettings.ChatCustomGoLiveSound)
                        {
                            soundPlayed = GoLiveSound.Play();
                        }
                        break;
                }
                if (!soundPlayed) {
                    defaultHighlightSound.Play();
                }
            }
        }


        // IMAGES
        public ChatterinoImage ReadImageFromStream(Stream stream)
        {
            try
            {
                return ChatterinoImage.FromStream(stream);
            }
            catch (Exception e){ 
                log(e.ToString());
            }

            return null;
        }
        
        public ChatterinoImage ReadImageFromStream(MemoryStream stream)
        {
            try
            {
                return ChatterinoImage.FromStream(stream);
            }
            catch (Exception e){ 
                log(e.ToString());
            }

            return null;
        }

        public void HandleAnimatedTwitchEmote(LazyLoadedImage emote, ChatterinoImage image)
        {
            if (image != null)
            {
                ChatterinoImage img = image;
                bool animated = false;
                lock (img) {
                    animated = img.IsAnimated;
                }

                if (animated)
                {
                    try
                    {
                        int frameCount = img.GetFrameCount();
                        int[] frameDuration = new int[frameCount];
                        int totaldelay = 0;
                        int currentFrame = 0;
                        int currentFrameOffset = 0;
                        int num = 0;
                        num = img.GetFrameDuration(0);
                        if (num <= 1)
                        {
                            num = 10;
                        }
                        frameDuration[0] = num-1;
                        totaldelay = num;
                        for (int i = 1; i < frameCount; i++)
                        {
                            num = img.GetFrameDuration(i);

                            if (num <= 1)
                            {
                                num = 10;
                            }
                            frameDuration[i] = frameDuration[i-1] + num;
                            totaldelay += num;
                        }
                        emote.IsAnimated = true;
                        emote.HandleAnimation += (int offset) =>
                        {
                            currentFrameOffset = offset % totaldelay;

                            int oldCurrentFrame = currentFrame;

                            currentFrame = 0;
                            
                            int index = Array.BinarySearch(frameDuration, currentFrameOffset);
                            if (index>=0) {
                                currentFrame = index;
                            } else {
                                currentFrame = ~index;
                                if (currentFrame >= frameCount)
                                {
                                    currentFrame = 0;
                                }
                            }

                            if (oldCurrentFrame != currentFrame)
                            {
                                lock (img)
                                {
                                    img.SelectActiveFrame(currentFrame);
                                }
                            }
                        };
                    }
                    catch (Exception e){ 
                        this.log(e.ToString());
                    }
                }
            }
        }

        Dictionary<ImageType, Image> images = new Dictionary<ImageType, Image>
        {
            [ImageType.BadgeAdmin] = Properties.Resources.admin_bg,
            [ImageType.BadgeBroadcaster] = Properties.Resources.broadcaster_bg,
            [ImageType.BadgeDev] = Properties.Resources.dev_bg,
            [ImageType.BadgeGlobalmod] = Properties.Resources.globalmod_bg,
            [ImageType.BadgeModerator] = Properties.Resources.moderator_bg,
            [ImageType.BadgeStaff] = Properties.Resources.staff_bg,
            [ImageType.BadgeTurbo] = Properties.Resources.turbo_bg,
            [ImageType.BadgeTwitchPrime] = Properties.Resources.twitchprime_bg,
            [ImageType.BadgeVerified] = Properties.Resources.partner,

            [ImageType.Cheer1] = Properties.Resources.cheer1,
            [ImageType.Cheer100] = Properties.Resources.cheer100,
            [ImageType.Cheer1000] = Properties.Resources.cheer1000,
            [ImageType.Cheer5000] = Properties.Resources.cheer5000,
            [ImageType.Cheer10000] = Properties.Resources.cheer10000,
            [ImageType.Cheer25000] = Properties.Resources._25000,
            [ImageType.Cheer50000] = Properties.Resources._50000,
            [ImageType.Cheer75000] = Properties.Resources._75000,
            [ImageType.Cheer100000] = Properties.Resources.cheer100000,
            [ImageType.Cheer200000] = Properties.Resources._200000,
            [ImageType.Cheer300000] = Properties.Resources._300000,
            [ImageType.Cheer400000] = Properties.Resources._400000,
            [ImageType.Cheer500000] = Properties.Resources._500000,
            [ImageType.Cheer600000] = Properties.Resources._600000,
            [ImageType.Cheer700000] = Properties.Resources._700000,
            [ImageType.Cheer800000] = Properties.Resources._800000,
            [ImageType.Cheer900000] = Properties.Resources._900000,
            [ImageType.Cheer1000000] = Properties.Resources._1000000,

            [ImageType.Ban] = Properties.Resources.ban,
            [ImageType.Timeout] = Properties.Resources.timeout,
            [ImageType.TimeoutAlt] = Properties.Resources.timeoutalt,
        };

        ConcurrentDictionary<string, LazyLoadedImage> badges = new ConcurrentDictionary<string, LazyLoadedImage>();
        private ConcurrentDictionary<string, CheerEmote> CheerEmotes = new ConcurrentDictionary<string, CheerEmote>();

        protected object logLock = new object();
        
        public void log(string text)
        {
            if (debug)
            {
                lock (logLock) {
                    string folder = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                    StreamWriter file = new StreamWriter(folder + @"\log.txt", true);
                    file.WriteLine(text);
                    file.Close();
                }
            }
        }


        public void LoadBadges()
        {
            try
            {
                var request =
                    WebRequest.Create($"https://badges.twitch.tv/v1/badges/global/display?language=en");
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    var parser = new JsonParser();
                    dynamic json = parser.Parse(stream);
                    dynamic badgeSets = json["badge_sets"];
                    foreach (var badge in badgeSets)
                    {
                        string name = badge.Key;
                        dynamic versions = badge.Value["versions"];

                        foreach (var version in versions)
                        {
                            string key = version.Key;

                            dynamic value = version.Value;

                            string imageUrl = value["image_url_1x"];
                            string title = value["title"];
                            string description = value["description"];
                            string clickUrl = value["click_url"];
                            string tooltipimageurl = value["image_url_4x"];

                            badges.TryAdd(name + "/" + key,
                            new LazyLoadedImage
                            {
                                Name = title,
                                Url = imageUrl,
                                TooltipImageUrl = tooltipimageurl,
                                Tooltip = title
                            });
                        }
                    }
                    GlobalBadgesLoaded = true;
                }
            }
            catch
            {
            }
        }

        public void NewLoadBadges()
        {
            try
            {
                dynamic json = TwitchApiHandler.Get("chat/badges/global", "");
                if (json is HttpStatusCode) { return; }
                dynamic data = json["data"];
                foreach (var badge in data)
                {
                    string name = badge["set_id"];
                    dynamic versions = badge["versions"];

                    foreach (var version in versions)
                    {
                        string key = version["id"];

                        string imageUrl = version["image_url_1x"];
                        string title = version["title"];
                        string description = version["description"];
                        string clickUrl = version["click_url"];
                        string tooltipimageurl = version["image_url_4x"];

                        var newBadge = new LazyLoadedImage
                        {
                            Name = title,
                            Url = imageUrl,
                            TooltipImageUrl = tooltipimageurl,
                            Tooltip = title
                        };

                        badges.AddOrUpdate(name + "/" + key, newBadge, (x, y) => newBadge);
                    }
                }
                GlobalBadgesLoaded = true;
            } catch (Exception e)
            {

            }
            
        }
        
        public ChatterinoImage GetImage(ImageType type)
        {
            lock (images)
            {
                Image img;
                return images.TryGetValue(type, out img) ? new ChatterinoImage(img) : null;
            }
        }

        public LazyLoadedImage GetBadge(String badge)
        {
            lock (badges)
            {
                LazyLoadedImage img;
                return badges.TryGetValue(badge, out img) ? img : null;
            }
        }

        public bool GetCheerEmote(string name,int cheer, bool light, out LazyLoadedImage outemote, out string outcolor)
        {
            CheerEmote emote;
            LazyLoadedImage emoteimage;
            string color;
            outemote = null;
            outcolor = null;

            if (CheerEmotes.TryGetValue(name.ToUpper(), out emote))
            {
                bool ret = emote.GetCheerEmote(cheer,light,out emoteimage,out color);
                outemote = emoteimage;
                outcolor = color;
                return ret;
            }
            return false;
        }

        public void AddCheerEmote(string prefix, CheerEmote emote)
        {
            CheerEmotes.AddOrUpdate(prefix, emote, (x, y) => emote);
        }

        public void ClearCheerEmotes(){
            CheerEmotes.Clear();
        }

        public CommonSize GetImageSize(ChatterinoImage image)
        {
            if (image == null)
            {
                return new CommonSize();
            }
            else
            {
                try
                {
                    return new CommonSize(image.Width, image.Height);
                }
                catch (Exception e) {
                    log("error getting image size: " + e.ToString());
                }

                return new CommonSize(16, 16);
            }
        }


        // MESSAGES
        static int sizeCacheStackLimit = 2048;

        ConcurrentDictionary<FontType, Tuple<ConcurrentDictionary<string, CommonSize>, ConcurrentStack<string>, int>> gdiSizeCaches = new ConcurrentDictionary<FontType, Tuple<ConcurrentDictionary<string, CommonSize>, ConcurrentStack<string>, int>>();
        ConcurrentDictionary<FontType, Tuple<ConcurrentDictionary<string, CommonSize>, ConcurrentStack<string>, int>> dwSizeCaches = new ConcurrentDictionary<FontType, Tuple<ConcurrentDictionary<string, CommonSize>, ConcurrentStack<string>, int>>();

        public CommonSize MeasureStringSize(object graphics, FontType font, string text)
        {
            var isGdi = graphics is Graphics;

            var sizeCache = (isGdi ? gdiSizeCaches : dwSizeCaches).GetOrAdd(font, f =>
            {
                int lineHeight;

                if (isGdi)
                {
                    lineHeight = TextRenderer.MeasureText((Graphics)graphics, "X", Fonts.GetFont(font), Size.Empty, App.DefaultTextFormatFlags).Height;
                }
                else
                {
                    using (var layout = new SharpDX.DirectWrite.TextLayout(Fonts.Factory, "X", Fonts.GetTextFormat(font), 1000000, 1000000))
                    {
                        var metrics = layout.Metrics;
                        lineHeight = (int)metrics.Height;
                        //lineHeight = (int)Math.Ceiling(Fonts.GetTextFormat(font).FontSize);
                    }
                }

                return Tuple.Create(new ConcurrentDictionary<string, CommonSize>(), new ConcurrentStack<string>(), lineHeight);
            });

            return sizeCache.Item1.GetOrAdd(text, s =>
            {
                if (sizeCache.Item2.Count >= sizeCacheStackLimit)
                {
                    string value;
                    if (sizeCache.Item2.TryPop(out value))
                    {
                        CommonSize _s;
                        sizeCache.Item1.TryRemove(value, out _s);
                    }
                }

                sizeCache.Item2.Push(s);

                if (isGdi)
                {
                    var size = TextRenderer.MeasureText((IDeviceContext)graphics, text, Fonts.GetFont(font), Size.Empty, App.DefaultTextFormatFlags);
                    return new CommonSize(size.Width, sizeCache.Item3);
                }
                else
                {
                    try
                    {
                        using (var layout = new SharpDX.DirectWrite.TextLayout(Fonts.Factory, text, Fonts.GetTextFormat(font), 1000000, 1000000))
                        {
                            var metrics = layout.Metrics;

                            return new CommonSize((int)metrics.WidthIncludingTrailingWhitespace, sizeCache.Item3);
                        }
                    }
                    catch { }
                    return new CommonSize(100, 10);
                }
            });
        }

        public void DisposeMessageGraphicsBuffer(Common.Message message)
        {
            if (message.buffer != null)
            {
                ((IDisposable)message.buffer).Dispose();
                message.buffer = null;
            }
        }

        public void FlashTaskbar()
        {
            if (!Util.IsLinux && App.MainForm != null)
            {

                App.MainForm.Invoke(() => Win32.FlashWindow.Flash(App.MainForm, 1));

                //App.MainForm.Invoke(() => Win32.FlashWindowEx(App.MainForm));
            }
        }

        public ChatterinoImage ScaleImage(ChatterinoImage image, double scale)
        {
            var img = image;

            int w = (int)(img.Width * scale), h = (int)(img.Height * scale);

            var newImage = new Bitmap(w, h);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                img.DrawImage(graphics, 0, 0, w, h);
            }

            return new ChatterinoImage(newImage);
        }

        public void FreezeImage(ChatterinoImage img)
        {

        }

        public ChatterinoImage DrawImageBackground(ChatterinoImage image, HSLColor color)
        {
            var img = image;

            var bitmap = new Bitmap(img.Width, img.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(color.ToColor());
                img.DrawImage(g, 0, 0, img.Width, img.Height);
            }

            return new ChatterinoImage(bitmap);
        }

        public void ExecuteHotkeyAction(HotkeyAction action)
        {

        }

        static ConcurrentDictionary<string, Image> timeoutImages = new ConcurrentDictionary<string, Image>();
        static Dictionary<string, int> values = new Dictionary<string, int>
        {
            ["s"] = 1,
            ["m"] = 60,
            ["h"] = 60 * 60,
            ["d"] = 60 * 60 * 24,
        };

        static Font timeoutFont = new Font("Arial", 7f);

        public ChatterinoImage GetImageForTimeout(int value)
        {
            string text1 = "";
            string text2 = "";

            if (value > 60 * 60 * 24 * 99 || value <= 0)
            {
                return new ChatterinoImage(Properties.Resources.timeout);
            }

            foreach (var v in values)
            {
                if (value >= v.Value)
                {
                    text1 = (value / v.Value).ToString();
                    text2 = v.Key;

                    if (text1.Length == 1)
                    {
                        text2 = text1 + text2;
                        text1 = "";
                    }
                }
            }

            return new ChatterinoImage (timeoutImages.GetOrAdd(text1 + text2, x =>
            {
                Bitmap bitmap = new Bitmap(16, 16);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                    if (text1 != "")
                    {
                        TextRenderer.DrawText(g, text1, timeoutFont, new Rectangle(0, -4, 16, 16), Color.Gray, flags);
                        TextRenderer.DrawText(g, text2, timeoutFont, new Rectangle(0, 4, 16, 16), Color.Gray, flags);
                    }
                    else
                    {
                        TextRenderer.DrawText(g, text2, timeoutFont, new Rectangle(0, 0, 16, 16), Color.Gray, flags);
                    }
                }

                return bitmap;
            }));
        }

        public void TriggerEmoteLoaded()
        {
            App.TriggerEmoteLoaded();
        }
    }
}
