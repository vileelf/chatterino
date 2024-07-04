using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chatterino.Common;

namespace Chatterino.Controls
{
    public class MessageDisplay : MessageContainerControl
    {
        private List<Message> _messages = new List<Message>();
        
        public HashSet<LazyLoadedImage> GifEmotes = new HashSet<LazyLoadedImage>();
        

        protected override Message[] Messages
        {
            get
            {
                return _messages.ToArray();
            }
        }

        public MessageDisplay()
        {
            mouseScrollMultiplier = 0.2;

            AllowMessageSeparator = false;
            EnableHatEmotes = false;
        }

        public delegate void MessageAdder(List<Message> msgs, HashSet<LazyLoadedImage> gifemotes);
        
        public void LoadMessages(MessageAdder msgfunc)
        {
            lock (MessageLock)
            {
                var messages = _messages;
                messages.Clear();
                GifEmotes.Clear();
                msgfunc(messages, GifEmotes);
            }
            scrollAtBottom = false;
            _scroll.Value = 0;
            updateMessageBounds();
            Invalidate();
        }
    }
}
