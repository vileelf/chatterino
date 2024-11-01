using System;
using System.Collections.Generic;
using System.Drawing;

namespace Chatterino.Common
{
    public class EmoteModifiers
    {
        public static readonly Dictionary<string, Func<Word, bool>> PreModifiers= new Dictionary<string, Func<Word, bool>>()
        {
            { "w!", (x) => { x.WidthMultiplier = 2; x.Tooltip += "w!,"; return true; } },
            { "h!",  (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                image.Rotate(RotateFlipType.Rotate180FlipY);;
                x.Tooltip += "h!,";
                return true;
            } },
            { "v!",  (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                image.Rotate(RotateFlipType.Rotate180FlipX);
                x.Tooltip += "v!,";
                return true;
            } },
            { "c!",  (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                image.ConvertToGreyScale();
                x.Tooltip += "c!,";
                return true;
            } },
            { "z!",  (x) => { if (x.X != 0) {x.XOffset -= 12; } x.Tooltip += "z!,";  return true; } },
        };

        public static readonly Dictionary<string, Func<Word, bool>> PostModifiers = new Dictionary<string, Func<Word, bool>>()
        {
            { "ffzCursed", (x) => { 
                var image = ((LazyLoadedImage)x.Value).Image;
                if (image == null) {
                    return false;
                }
                image.ConvertToGreyScale();
                x.Tooltip += "ffzCursed,";
                return true; 
            } },
            { "ffzX", (x) => {
                var image = ((LazyLoadedImage)x.Value).Image;
                image.Rotate(RotateFlipType.Rotate180FlipY);
                x.Tooltip += "ffzX,";
                return true; 
            } },
            { "ffzY", (x) => { 
                var image = ((LazyLoadedImage)x.Value).Image;
                image.Rotate(RotateFlipType.Rotate180FlipX);
                x.Tooltip += "ffzY,";
                return true;
            } },
            { "ffzW", (x) => { x.WidthMultiplier = 2; x.Tooltip += "\nffzW"; return true; } },
        };

        public static bool IsEmoteModifier(string name) {
            return !string.IsNullOrWhiteSpace(name) && (PreModifiers.ContainsKey(name) || PostModifiers.ContainsKey(name));
        }
        public static bool IsPreEmoteModifier(string name) {
            return !string.IsNullOrWhiteSpace(name) && PreModifiers.ContainsKey(name);
        }
        public static bool IsPostEmoteModifier(string name) {
            return !string.IsNullOrWhiteSpace(name) && PostModifiers.ContainsKey(name);
        }
        private static bool ModifyWordPre(Word target, string modifier) {
            return PreModifiers.TryGetValue(modifier, out var func) && func(target);
        }
        private static bool ModifyWordPost(Word target, string modifier) {
            return PostModifiers.TryGetValue(modifier, out var func) && func(target);
        }
        public static bool ModifyWord(Word target) {
            var image = ((LazyLoadedImage)target.Value).Image;
            if (image == null) {
                return false;
            }
            image = image.Clone();
            var newImage = new LazyLoadedImage(image);
            target.Value = newImage;
            target.Tooltip += "\nModifiers: ";
            bool returnValue = false;
            foreach (var modifier in target.Modifiers) {
                returnValue = ModifyWordPre(target, modifier) || ModifyWordPost(target, modifier);
            }
            target.Tooltip = target.Tooltip.TrimEnd(',');
            return returnValue;
        }
    }
}
