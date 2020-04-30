using Chatterino.Common;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Chatterino.Common
{
    public class CheerEmote
    {
        private class _CheerEmote {
            public LazyLoadedImage light;
            public LazyLoadedImage dark;
            public int min_bits;
            public string color;
            public _CheerEmote(LazyLoadedImage light, LazyLoadedImage dark, int min_bits, string color) {
                this.light = light;
                this.dark = dark;
                this.min_bits = min_bits;
                this.color = color;
            }
            public _CheerEmote(int min_bits) {
                this.min_bits = min_bits;
            }
        }
        private class _MinBitsCompare : IComparer<_CheerEmote> {
            public int Compare(_CheerEmote x, _CheerEmote y) {
                return x.min_bits - y.min_bits;
            }
        }

        private List<_CheerEmote> _CheerEmotes;

        public bool GetCheerEmote(int cheer, bool light, out LazyLoadedImage outemote, out string color)
        {
            _CheerEmote emote;
            outemote = null;
            color = null;
            
            if ((emote = _findMaxBits(_CheerEmotes, cheer))!= null)
            {
                outemote = light?emote.light:emote.dark;
                color = emote.color;
                return true;
            }

            return false;
        }

        public void Add(LazyLoadedImage light, LazyLoadedImage dark, int min_bits, string color) {
            _CheerEmote emote = new _CheerEmote(light,dark,min_bits,color);
            int i = _CheerEmotes.BinarySearch(emote, new _MinBitsCompare());
            if(i>=0) {
                _CheerEmotes[i]=emote;
            } else {
                _CheerEmotes.Insert(~i, emote);
            }
        }

        private _CheerEmote _findMaxBits(List<_CheerEmote> cheerlist, int cheer) {
            _CheerEmote emote = new _CheerEmote(cheer);
            int i = cheerlist.BinarySearch(emote, new _MinBitsCompare());
            if (cheerlist.Count>0) {
                if(i>=0) {
                    return cheerlist[i];
                } else {
                    if (~i > 0 && (~i-1)<cheerlist.Count) {
                        return cheerlist[~i-1];
                    } else {
                        return cheerlist[0];
                    }
                }
            }
            return null;
        }

        public CheerEmote() {
            _CheerEmotes = new List<_CheerEmote>();
        }

        public CheerEmote(int size) {
            _CheerEmotes = new List<_CheerEmote>(size);
        }
    }
    
}