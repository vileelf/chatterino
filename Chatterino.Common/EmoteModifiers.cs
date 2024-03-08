using System;
using System.Collections.Generic;
using System.Drawing;

namespace Chatterino.Common
{
    public class EmoteModifiers
    {
        public static readonly Dictionary<string, Func<Word, Word>> PreModifiers= new Dictionary<string, Func<Word, Word>>()
        {
            { "w!", (x) => { x.Width = (x.Width * 2); return x; } },
            { "h!",  (x) => { ((LazyLoadedImage)x.Value).Image?.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipX); return x; } },
            { "v!",  (x) => { ((LazyLoadedImage)x.Value).Image?.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipY); return x; } },
            { "z!",  (x) => { return x; } },
        };

        public static readonly Dictionary<string, Func<Word, Word>> PostModifiers = new Dictionary<string, Func<Word, Word>>()
        {
            { "ffzCursed", (x) => { return x; } },
            { "ffzX", (x) => {((LazyLoadedImage)x.Value).Image?.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipX); return x; } },
            { "ffzY", (x) => { ((LazyLoadedImage)x.Value).Image?.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipY); return x; } },
            { "ffzW", (x) => { x.Width = (x.Width * 2); return x; } },
        };
    }
}
