using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json;

namespace Chatterino.Common
{
    public static class Emojis
    {
        static Regex findShortCodes = new Regex(":([-+\\w]+):", RegexOptions.Compiled);
        public static ConcurrentDictionary<string, string> ShortCodeToEmoji = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> EmojiToShortCode = new ConcurrentDictionary<string, string>();

        public static ConcurrentDictionary<char, ConcurrentDictionary<string, Emoji>> FirstEmojiChars = new ConcurrentDictionary<char, ConcurrentDictionary<string, Emoji>>();

        private static string EmojiGlobalCache = Path.Combine(Util.GetUserDataPath(), "Cache", "emoji_global.json");

        public class Emoji {
            public string unified;
            public string non_qualified;
            public string name;
            public string image;
            public string[] short_names;
            public bool has_img_twitter;
            public string url;
            public List<string> shortcodes;
            public LazyLoadedImage img;
            public Dictionary<string, Emoji> skin_variations;
        }
        public static string ReplaceShortCodes(string s)
        {
            return findShortCodes.Replace(s, m =>
            {
                string emoji;

                if (ShortCodeToEmoji.TryGetValue(m.Groups[1].Value, out emoji))
                    return emoji;

                return m.Value;
            });
        }

        public static int[] ToCodePoints(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            var codePoints = new List<int>(str.Length);
            for (var i = 0; i < str.Length; i++)
            {
                codePoints.Add(char.ConvertToUtf32(str, i));
                if (char.IsHighSurrogate(str[i]))
                    i += 1;
            }

            return codePoints.ToArray();
        }
        
        public static void LoadEmojis() {
            //https://raw.githubusercontent.com/iamcal/emoji-data/master/emoji_pretty.json
            
            //https://raw.githubusercontent.com/iamcal/emoji-data/master/img-twitter-72/0023-fe0f-20e3.png
            try {
                using (var webClient = new WebClient()) {
                    using (var readStream = webClient.OpenRead("https://raw.githubusercontent.com/iamcal/emoji-data/master/emoji.json")) {
                        if (File.Exists(EmojiGlobalCache)) {
                            File.Delete(EmojiGlobalCache);
                        }
                        using (var writeStream = File.OpenWrite(EmojiGlobalCache))
                        {
                            readStream.CopyTo(writeStream);
                        }
                    }
                }
                using (var stream = File.OpenRead(EmojiGlobalCache))
                {
                    using (var reader = new StreamReader(stream)) {
                        var jsonstring = reader.ReadToEnd();
                        var json = JsonConvert.DeserializeObject<Emoji[]>(jsonstring);
                    
                        
                        for(int i=0; i<json.Length; i++)
                        {
                            if (json[i].has_img_twitter) {
                                json[i].shortcodes = getEmojiShortcodes(json[i]);
                                
                                json[i].img = new LazyLoadedImage
                                {
                                    Url = $"https://raw.githubusercontent.com/iamcal/emoji-data/master/img-twitter-72/{json[i].image}",
                                    Tooltip = $":{json[i].short_names[0]}:\nemoji",
                                    TooltipImageUrl = $"https://raw.githubusercontent.com/iamcal/emoji-data/master/img-twitter-72/{json[i].image}",
                                    Name = json[i].name,
                                    Scale = 0.35,
                                    HasTrailingSpace = false,
                                    copyText = json[i].shortcodes[0],
                                    IsEmote = true
                                };
                                if (json[i].short_names[0]=="gun") {
                                    //specifically gun gets the old image so we dont have squirt guns
                                    json[i].img.Url = $"https://cdnjs.cloudflare.com/ajax/libs/emojione/2.2.6/assets/png/1f52b.png";
                                    json[i].img.TooltipImageUrl = json[i].img.Url;
                                }
                                for (int j=0; j < json[i].short_names.Length; j++) {
                                    ShortCodeToEmoji[json[i].short_names[j]] = json[i].shortcodes[0];
                                }
                                for (int j=0; j < json[i].shortcodes.Count; j++) {
                                    EmojiToShortCode[json[i].shortcodes[j]] = json[i].short_names[0];
                                    FirstEmojiChars.GetOrAdd(json[i].shortcodes[j][0], c => new ConcurrentDictionary<string, Emoji>())[json[i].shortcodes[j]] = json[i];
                                }
                                if (json[i].skin_variations != null) {
                                    Emoji skintone;
                                    foreach (var skintoneKeyValue in json[i].skin_variations) {
                                        skintone = skintoneKeyValue.Value;
                                        if (skintone.has_img_twitter) {
                                            skintone.shortcodes = getEmojiShortcodes(skintone);
                                            skintone.img = new LazyLoadedImage {
                                                Url = $"https://raw.githubusercontent.com/iamcal/emoji-data/master/img-twitter-72/{skintone.image}",
                                                Tooltip = $":{json[i].short_names[0]}:\nemoji",
                                                TooltipImageUrl = $"https://raw.githubusercontent.com/iamcal/emoji-data/master/img-twitter-72/{skintone.image}",
                                                Name = json[i].name,
                                                Scale = 0.35,
                                                HasTrailingSpace = false,
                                                copyText = skintone.shortcodes[0],
                                                IsEmote = true
                                            };
                                            for (int j=0; j < skintone.shortcodes.Count; j++) {
                                                FirstEmojiChars.GetOrAdd(skintone.shortcodes[j][0], c => new ConcurrentDictionary<string, Emoji>())[skintone.shortcodes[j]] = skintone;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception e) {
                GuiEngine.Current.log(e.ToString());
            }
            //(Emotes.EmotesLoaded)?.Invoke(null, EventArgs.Empty);
        }
        
        private static List<string> getEmojiShortcodes(Emoji emoji) {
            string shortcode;
            List<string> retshortcodes = new List<string>();
            
            if (!String.IsNullOrEmpty(emoji.unified)) {
                shortcode = emoji.unified;
                convertHexToStrings(shortcode, retshortcodes);
            }
            
            if (!String.IsNullOrEmpty(emoji.non_qualified) && emoji.non_qualified!=emoji.unified) {
                shortcode = emoji.non_qualified;
                convertHexToStrings(shortcode, retshortcodes);
            }
            return retshortcodes;
        }
        
        private static void convertHexToStrings(string hexstring, List<string> shortcodes) {
            string[] hexlist;
            string retshortcode1 = "";
            string retshortcode2 = "";
            hexlist = hexstring.Split('-');
            for (int j=0;j<hexlist.Length;j++) {
                retshortcode2 += Char.ConvertFromUtf32((int)System.Convert.ToUInt32(hexlist[j],16));
                if (hexlist[j].ToUpper() == "200D") {
                    //because of https://github.com/FrankerFaceZ/FrankerFaceZ/issues/1147
                    hexlist[j] = "E0002";
                }
                retshortcode1 += Char.ConvertFromUtf32((int)System.Convert.ToUInt32(hexlist[j],16));
            }
            shortcodes.Add(retshortcode1);
            if (retshortcode1!=retshortcode2) {
                shortcodes.Add(retshortcode2);
            }
        }
        
        public static object[] ParseEmojis(string text)
        {
            var objects = new List<object>();

            var lastSlice = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (!char.IsLowSurrogate(text, i))
                {
                    ConcurrentDictionary<string, Emoji> _emojis;
                    if (FirstEmojiChars.TryGetValue(text[i], out _emojis))
                    {
                        for (var j = Math.Min(8, text.Length - i); j > 0; j--)
                        {
                            var emoji = text.Substring(i, j);
                            Emoji emote;

                            if (_emojis.TryGetValue(emoji, out emote))
                            {
                                if (i - lastSlice != 0)
                                    objects.Add(text.Substring(lastSlice, i - lastSlice));

                                objects.Add(emote.img);

                                i += j - 1;

                                lastSlice = i + 1;
                                break;
                            }
                        }
                    }
                }
            }

            if (lastSlice == 0 && objects.Count == 0)
            {
                return new object[] { text };
            }

            if (lastSlice < text.Length)
                objects.Add(text.Substring(lastSlice));

            return objects.ToArray();
        }

        static Emojis()
        {
        }
    }
}
