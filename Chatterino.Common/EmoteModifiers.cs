using System;
using System.Collections.Generic;
using System.Drawing;

namespace Chatterino.Common
{
    public class EmoteModifiers
    {
        public static readonly Dictionary<string, Func<Word, bool>> PreModifiers= new Dictionary<string, Func<Word, bool>>()
        {
            { "w!", (x) => { x.WidthMultiplier = 2; x.Tooltip += "\nw!"; return true; } },
            { "h!",  (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                var newImage = image.Clone();
                newImage.Rotate(RotateFlipType.Rotate180FlipY);
                var newLazyImage = new LazyLoadedImage(newImage);
                x.Value = newLazyImage;
                x.Tooltip += "\nh!";
                return true;
            } },
            { "v!",  (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                var newImage = image.Clone();
                newImage.Rotate(RotateFlipType.Rotate180FlipX);
                var newLazyImage = new LazyLoadedImage(newImage);
                x.Value = newLazyImage;
                x.Tooltip += "\nv!";
                return true;
            } },
            { "z!",  (x) => { if (x.X != 0) {x.XOffset -= 12; } x.Tooltip += "\nz!";  return true; } },
        };

        public static readonly Dictionary<string, Func<Word, bool>> PostModifiers = new Dictionary<string, Func<Word, bool>>()
        {
            { "ffzCursed", (x) => { 
                var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                var newImage = image.Clone();
                newImage.ConvertToGreyScale();
                var newLazyImage = new LazyLoadedImage(newImage);
                x.Value = newLazyImage;
                x.Tooltip += "\nffzCursed";
                return true; 
            } },
            { "ffzX", (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                var newImage = image.Clone();
                newImage.Rotate(RotateFlipType.Rotate180FlipY);
                var newLazyImage = new LazyLoadedImage(newImage);
                x.Value = newLazyImage;
                x.Tooltip += "\nffzX";
                return true; 
            } },
            { "ffzY", (x) => { var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                var newImage = image.Clone();
                newImage.Rotate(RotateFlipType.Rotate180FlipX);
                var newLazyImage = new LazyLoadedImage(newImage);
                x.Value = newLazyImage;
                x.Tooltip += "\nffzY";
                return true;
            } },
            { "ffzW", (x) => { x.WidthMultiplier = 2; x.Tooltip += "\nffzW"; return true; } },
        };

        public static bool IsEmoteModifier(string name) {
            return PreModifiers.ContainsKey(name) || PostModifiers.ContainsKey(name);
        }
        public static bool IsPreEmoteModifier(string name) {
            return PreModifiers.ContainsKey(name);
        }
        public static bool IsPostEmoteModifier(string name) {
            return PostModifiers.ContainsKey(name);
        }
        public static bool ModifyWordPre(Word target, string modifier) {
            return PreModifiers.TryGetValue(modifier, out var func) && func(target);
        }
        public static bool ModifyWordPost(Word target, string modifier) {
            return PostModifiers.TryGetValue(modifier, out var func) && func(target);
        }
        public static bool ModifyWord(Word target, string modifier) {
            return ModifyWordPre(target, modifier) || ModifyWordPost(target, modifier);
        }
    }
}
