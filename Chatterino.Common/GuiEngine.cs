using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public static class GuiEngine
    {
        public static IGuiEngine Current { get; private set; } = null;

        public static void Initialize(IGuiEngine engine)
        {
            if (Current != null)
                throw new InvalidOperationException("There already is a GuiEngine loaded.");

            Current = engine;
        }
    }

    public interface IGuiEngine
    {
        bool IsDarkTheme { get; }
        void FreezeImage(object img);

        void HandleLink(Link link);
        void PlaySound(NotificationSound sound, bool forceCustom = false);
        object GetImage(ImageType type);
        LazyLoadedImage GetBadge(String badge);
        void HandleAnimatedTwitchEmote(LazyLoadedImage emote, object image);
        bool GetCheerEmote(string name,int cheer, bool light, out LazyLoadedImage outemote, out string outcolor);
        void FlashTaskbar();
        void LoadBadges();
        void log(string log);
        bool globalEmotesLoaded{ get; set;}
        void AddCheerEmote(string prefix, CheerEmote emote);
        void ClearCheerEmotes();
        object ReadImageFromStream(Stream stream);
        object ScaleImage(object image, double scale);
        object DrawImageBackground(object image, HSLColor color);
        HashSet<LazyLoadedImage> GifEmotesOnScreen{get;}
        object GifEmotesLock{get;}

        CommonSize MeasureStringSize(object graphics, FontType font, string text);
        //void DrawMessage(object graphics, Message message, int xOffset, int yOffset, Selection selection, int currentLineIndex);
        //void DrawGifEmotes(object graphics, Message message, Selection selection, int currentLineIndex);

        void DisposeMessageGraphicsBuffer(Message message);

        CommonSize GetImageSize(object image);

        void ExecuteHotkeyAction(HotkeyAction action);
        void TriggerEmoteLoaded();
    }
}
