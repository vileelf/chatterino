using System;
using System.Collections.Generic;
using System.Drawing;

namespace Chatterino.Common
{
    public class EmoteModifiers
    {
        public static readonly Dictionary<string, Func<ChatterinoImage, ChatterinoImage>> PreModifiers= new Dictionary<string, Func<ChatterinoImage, ChatterinoImage>>()
        {
            { "w!", (x) => { return x; } },
            { "h!",  (x) => { x.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipX); return x; } },
            { "v!",  (x) => { x.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipY); return x; } },
            { "z!",  (x) => { return x; } },
        };

        public static readonly Dictionary<string, Func<ChatterinoImage, ChatterinoImage>> PostModifiers = new Dictionary<string, Func<ChatterinoImage, ChatterinoImage>>()
        {
            { "ffzCursed", (x) => { return x; } },
            { "ffzX", (x) => {x.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipX); return x; } },
            { "ffzY", (x) => { x.ActiveImage.RotateFlip(RotateFlipType.RotateNoneFlipY); return x; } },
            { "ffzW", (x) => { return x; } },
        };
    }
}
