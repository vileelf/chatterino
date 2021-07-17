using Chatterino.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chatterino.Controls
{
    public class MessageDisplayPopup : Form
    {
        MessageDisplay container;

        public MessageDisplayPopup()
        {
            Icon = App.Icon;

            Text = "Message display";

            //TopMost = true;

            container = new MessageDisplay();
            container.Dock = DockStyle.Fill;

            Controls.Add(container);

            Width = 500;
            Height = 600;

            TopMost = AppSettings.WindowTopMost;

            AppSettings.WindowTopMostChanged += (s, e) =>
            {
                TopMost = AppSettings.WindowTopMost;
            };
        }
        
        public HashSet<LazyLoadedImage> GetGifEmotes() {
            if (container!=null) {
                return container.GifEmotes;
            }
            return null;
        }
        
        public void ClearGifEmotes() {
            if (container!=null) {
                container.GifEmotes.Clear();
            }
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            container.GifEmotes.Clear();
            container.ClearBuffer();
            container = null;
            base.OnFormClosed(e);
        }

        public void LoadMessages(string title, MessageDisplay.MessageAdder msgfunc)
        {
            Text = title;

            container.LoadMessages(msgfunc);
        }
    }
}
