using Chatterino.Common;
using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing;

namespace Chatterino.Common
{
    public class LazyLoadedImage
    {
        public string Url { get; set; } = null;
        public string Name { get; set; } = null;
        public bool IsAnimated { get; set; } = false;
        public Func<ChatterinoImage> LoadAction { get; set; } = null;
        public bool IsHat { get; set; } = false;
        public bool HasTrailingSpace { get; set; } = true;
        public bool IsEmote { get; set; }

        public double Scale { get; set; } = 1;
        public Margin Margin { get; set; } = null;

        public string Tooltip { get; set; } = null;
        public string TooltipImageUrl { get; set; } = null;
        public LazyLoadedImage TooltipImage = null;
        public string click_url {get; set; } = null;
        public bool IsDanke = false;
        
        public struct EmoteInfoStruct {
            public string type;
            public string tier;
            public string ownerid;
            public string id;
            public string setid;
        }
        public EmoteInfoStruct EmoteInfo = new EmoteInfoStruct();
        public delegate void HA(int offset);
        public HA HandleAnimation = null;
        public event EventHandler ImageLoaded;

        bool loading = false;
        private ChatterinoImage image = null;

        public ChatterinoImage Image
        {
            get
            {
                if (image != null)
                    return image;
                if (loading) return null;

                loading = true;
                
                try
                {
                    Task.Run(() =>
                    {
                        if (EmoteCache.CheckEmote(Url)) {
                            try
                            {
                                EmoteCache.GetEmote(Url, (emote) => {
                                    if (emote != null) {
                                        GuiEngine.Current.HandleAnimatedTwitchEmote(this, emote);
                                        image = emote;
                                        GuiEngine.Current.TriggerEmoteLoaded();
                                        ImageLoaded?.Invoke(null, null);
                                        loading = false;
                                    } else {
                                        getEmote();
                                    }
                                });
                            } catch (Exception e) {
                                GuiEngine.Current.log("Error loading emote from cache" + Name + " " + Url+ " " +e.ToString());
                            }
                        } else {
                            getEmote();
                        }
                    });
                } catch (Exception e) {
                    GuiEngine.Current.log("Error loading emote" + Name + " " + Url+ " " +e.ToString());
                }
                
                return null;
            }
        }
        
        private void getEmote() {
            try
            {
                ChatterinoImage img;
                if (LoadAction != null)
                {
                    img = LoadAction();
                }
                else
                {
                    try
                    {
                        var request = WebRequest.Create(Url);
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
                    catch (Exception e)
                    {
                        GuiEngine.Current.log("emote faild to load " + Name + " " + Url+ " " +e.ToString());
                        img = null;
                    }
                }
                if (img != null)
                {
                    GuiEngine.Current.HandleAnimatedTwitchEmote(this, img);
                    EmoteCache.AddEmote(Url, img);
                    image = img;
                    GuiEngine.Current.TriggerEmoteLoaded();
                    ImageLoaded?.Invoke(null, null);
                }
                loading = false;
            } catch (Exception e) {
                GuiEngine.Current.log("Error loading emote " + Name + " " + Url+ " " +e.ToString());
            }
        }

        public LazyLoadedImage()
        {
            
        }

        public LazyLoadedImage(ChatterinoImage image)
        {
            this.image = image;
            loading = false;
        }
    }
}
