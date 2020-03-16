using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Drawing;

namespace Chatterino.Common
{
    
    public static class EmoteCache
    {
        private struct _emotes_cache {
            public Image emote;
            public bool usedLastTime;
        }

        private static ConcurrentDictionary<string, _emotes_cache> CachedEmotes =
            new ConcurrentDictionary<string, _emotes_cache>();

        public static Image GetEmote(string url) {
            _emotes_cache ecache;
            
            if (CachedEmotes.TryGetValue(url, out ecache)) {
                ecache.usedLastTime = true;
                return ecache.emote;
            }
            return null;
        }
        
        public static bool AddEmote(string url, Image emote) {
            _emotes_cache ecache;
            if (CachedEmotes.TryGetValue(url, out ecache)) {
                return false;
            }
            ecache.usedLastTime = true;
            ecache.emote = emote;
            //save emote to a file
            
            return true;
        }
        
        public static void init () {
            //load list of emotes from file and delete unused ones
            var emotesCache = Path.Combine(Util.GetUserDataPath(), "Cache", $"emote_cache");
            var stream = File.OpenRead(emotesCache);
            var parser = new JsonParser();
            dynamic json = parser.Parse(stream);
            dynamic cachedemotes = json["CachedEmotes"];
            
            foreach (var url in cachedemotes.Values) {
                
            }
        }
    }
}
