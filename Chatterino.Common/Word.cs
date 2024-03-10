using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Chatterino.Common
{
    public class Word
    {
        public SpanType Type { get; set; }
        public object Value { get; set; }
        public HSLColor? Color { get; set; }
        public Link Link { get; set; }
        public string Tooltip { get; set; }
        public string TooltipImageUrl { get; set; }
        public LazyLoadedImage TooltipImage { get; set; }
        public string CopyText { get; set; } = null;
        public bool Highlighted { get; set; } = false;
        public List<string> Modifiers { get; set; } = new List<string>();
        public bool IsModifier { get; set; } = false;
        public bool IsModifying { get; set; } = false;
        public bool DoneModifying { get; set; } = false;
        public FontType Font { get; set; }
        public int Height { get; set; } = 16;
        public int Width { get; set; } = 16;
        public int X { get; set; }
        public int Y { get; set; }
        public int XOffset { get; set; } = 0;
        public int WidthMultiplier { get; set; } = 1;

        public bool HasTrailingSpace { get; set; } = true;

        public Tuple<string, CommonRectangle>[] SplitSegments { get; set; } = null;
        public int[] CharacterWidths { get; set; } = null;

        public bool Intersects(Word word)
        {
            if (X < word.X)
            {
                if (word.X < X + Width)
                {
                    if (Y < word.Y)
                    {
                        if (word.Y < Y + Height)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (Y < word.Y + word.Height)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (X < word.X + word.Width)
                {
                    if (Y < word.Y)
                    {
                        if (word.Y < Y + Height)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (Y < word.Y + word.Height)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Replaces this word with the word given in replacementWord.
        /// </summary>
        /// <param name="replacementWord">The word that will replace this word.</param>
        public void Copy(Word replacementWord)
        {
            Type = replacementWord.Type;
            Value = replacementWord.Value;
            Color = replacementWord.Color;
            Link = replacementWord.Link;
            Tooltip = replacementWord.Tooltip;
            TooltipImageUrl = replacementWord.TooltipImageUrl;
            TooltipImage = replacementWord.TooltipImage;
            CopyText = replacementWord.CopyText;
            Highlighted = replacementWord.Highlighted;
        }

        public bool IsHat() {
            return Type == SpanType.LazyLoadedImage && ((LazyLoadedImage)Value).IsHat;
        }
    }
}

    public enum SpanType
    {
        Text,
        LazyLoadedImage,
        UnloadedBadge
    }
