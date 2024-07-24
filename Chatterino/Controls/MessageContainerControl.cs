﻿using Chatterino.Common;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Message = Chatterino.Common.Message;
using SCB = SharpDX.Direct2D1.SolidColorBrush;

namespace Chatterino.Controls
{
    public class MessageContainerControl : ColumnLayoutItem
    {
        public Padding MessagePadding { get; protected set; } = new Padding(8, 8, SystemInformation.VerticalScrollBarWidth + 8, 8);

        protected CustomScrollBar _scroll = new CustomScrollBar
        {
            Enabled = false,
            SmallChange = 4,
        };

        public bool AllowMessageSeparator { get; set; } = true;

        public bool EnableHatEmotes { get; protected set; } = true;

        private ContextMenu urlContextMenu = new ContextMenu();
        private ContextMenu normalContextMenu = new ContextMenu();
        private Link urlContextMenuLink;
        private MenuItem replyMenuItem;
        private MenuItem replyMenuItemLink;

        protected bool scrollAtBottom = true;

        private object messageLock = new object();

        protected virtual object MessageLock
        {
            get { return messageLock; }
        }

        Message[] messages = new Message[0];

        protected virtual Message[] Messages
        {
            get { return messages; }
        }

        protected virtual Message[] GetMessagesClone()
        {
            Message[] M;
            lock (MessageLock)
            {
                M = new Message[Messages.Length];
                Array.Copy(Messages, M, M.Length);
            }
            return M;
        }

        protected Message LastReadMessage { get; set; } = null;

        protected List<GifEmoteState> GifEmotesOnScreen = new List<GifEmoteState>();

        // mouse
        protected double mouseScrollMultiplier = 1;

        protected Link mouseDownLink = null;
        protected Word mouseDownWord = null;
        protected Selection mouseDownSelection = null;
        protected Selection selection = null;
        protected bool mouseDown = false;
        protected bool leftClick = false;

        // buffer
        protected BufferedGraphicsContext context = BufferedGraphicsManager.Current;
        protected BufferedGraphics buffer = null;

        protected object bufferLock = new object();

        private Message clickedMessage = null;

        // Constructor
        public MessageContainerControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            Width = 600;
            Height = 500;

            _scroll.Location = new Point(Width - SystemInformation.VerticalScrollBarWidth - 1, 1);
            _scroll.Size = new Size(SystemInformation.VerticalScrollBarWidth, Height - 1);
            _scroll.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

            _scroll.Scroll += (s, e) =>
            {
                checkScrollBarPosition();
                updateMessageBounds();

                ProposeInvalidation();
            };

            urlContextMenu.MenuItems.Add(new MenuItem("Open in Browser", (s, e) => GuiEngine.Current.HandleLink(urlContextMenuLink)));
            urlContextMenu.MenuItems.Add(new MenuItem("Copy link", (s, e) => Clipboard.SetText(urlContextMenuLink.Value as string ?? "")));
            replyMenuItemLink = urlContextMenu.MenuItems.Add("Reply to message", (s, e) => {
                if (clickedMessage != null)
                    (App.MainForm.Selected as ChatControl)?.Input.Logic.SetText("/reply " + clickedMessage?.MessageId + " ");
            });

            normalContextMenu.MenuItems.Add("Copy Selection ", (s, e) => { CopySelection(false);});
            normalContextMenu.MenuItems.Add("Append selection to message", (s, e) => {(App.MainForm.Selected as ChatControl)?.PasteText(GetSelectedText(false));});
            replyMenuItem = normalContextMenu.MenuItems.Add("Reply to message", (s, e) => {
                if (clickedMessage != null)
                    (App.MainForm.Selected as ChatControl)?.Input.Logic.SetText("/reply " + clickedMessage?.MessageId + " ");
            });
            Controls.Add(_scroll);

            App.GifEmoteFramesUpdated += App_GifEmoteFramesUpdated;
            App.EmoteLoaded += App_EmoteLoaded;

            Disposed += (s, e) =>
            {
                App.GifEmoteFramesUpdated -= App_GifEmoteFramesUpdated;
                App.EmoteLoaded -= App_EmoteLoaded;
            };
        }

        public void ClearBuffer()
        {
            lock (bufferLock) {
                if (buffer != null) {
                    buffer.Dispose();
                    buffer = null;
                }
            }
        }
        private void App_GifEmoteFramesUpdated(object s, EventArgs e)
        {
            
            lock (bufferLock)
            {
                try
                {
                    if (buffer != null)
                    {
                        var hasUpdated = false;

                        if (MessageLock != null)
                        {
                            lock (MessageLock)
                            {

                                hasUpdated = true;

                                MessageRenderer.DrawGifEmotes(buffer.Graphics, GifEmotesOnScreen, selection);
                            }
                        }

                        if (hasUpdated)
                        {
                            var borderPen = Selected ? App.ColorScheme.ChatBorderFocused : App.ColorScheme.ChatBorder;
                            buffer.Graphics.DrawRectangle(borderPen, 0, Height - 1, Width - 1, 1);

                            var g = CreateGraphics();

                            buffer.Render(g);

                            g.Dispose();
                        }
                    }
                }
                catch { }
            }
            
        }

        private void App_EmoteLoaded(object s, EventArgs e)
        {
            updateMessageBounds(true);

            ProposeInvalidation();
        }

        // overrides
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            try {
                if (_scroll.Enabled)
                {
                    var scrollDistance = (int)(e.Delta * AppSettings.ScrollMultiplyer);

                    if (MessageLock != null)
                    {
                        var graphics = App.UseDirectX ? null : CreateGraphics();

                        lock (MessageLock)
                        {
                            if (e.Delta > 0)
                            {
                                var i = (int)_scroll.Value;
                                var val = _scroll.Value;

                                var scrollFactor = _scroll.Value % 1;
                                var currentScrollLeft = (int)(scrollFactor * Messages[i].Height);

                                for (; i >= 0; i--)
                                {
                                    if (scrollDistance < currentScrollLeft)
                                    {
                                        val -= scrollFactor * ((double)scrollDistance / currentScrollLeft);
                                        _scroll.Value = val;
                                        break;
                                    }
                                    else
                                    {
                                        scrollDistance -= currentScrollLeft;
                                        val -= scrollFactor;
                                    }

                                    if (i == 0)
                                    {
                                        _scroll.Value = 0;
                                    }
                                    else
                                    {
                                        Messages[i - 1].CalculateBounds(graphics,
                                            Width - MessagePadding.Left - MessagePadding.Right, EnableHatEmotes);

                                        scrollFactor = 1;
                                        currentScrollLeft = Messages[i - 1].Height;
                                    }
                                }
                            }
                            else
                            {
                                scrollDistance = -scrollDistance;

                                var i = (int)_scroll.Value;
                                var val = _scroll.Value;

                                var scrollFactor = 1 - (_scroll.Value % 1);
                                var currentScrollLeft = (int)(scrollFactor * Messages[i].Height);

                                for (; i < Messages.Length; i++)
                                {
                                    if (scrollDistance < currentScrollLeft)
                                    {
                                        val += scrollFactor * ((double)scrollDistance / currentScrollLeft);
                                        _scroll.Value = val;
                                        break;
                                    }
                                    else
                                    {
                                        scrollDistance -= currentScrollLeft;
                                        val += scrollFactor;
                                    }

                                    if (i == Messages.Length - 1)
                                    {
                                        //_scroll.Value = 0;
                                    }
                                    else
                                    {
                                        Messages[i + 1].CalculateBounds(graphics, Width - MessagePadding.Left - MessagePadding.Right, EnableHatEmotes);

                                        scrollFactor = 1;
                                        currentScrollLeft = Messages[i + 1].Height;
                                    }
                                }

                            }
                        }

                        graphics?.Dispose();
                    }

                    if (e.Delta > 0)
                        scrollAtBottom = false;
                    else
                        checkScrollBarPosition();

                    updateMessageBounds();

                    ProposeInvalidation();
                }
            } catch (Exception ex) {
                GuiEngine.Current.log(ex.ToString());
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index;

            var graphics = App.UseDirectX ? null : CreateGraphics();

            var msg = MessageAtPoint(e.Location, out index);
            if (msg != null)
            {
                var word = msg.WordAtPoint(new CommonPoint(e.X - MessagePadding.Left, e.Y - msg.Y));

                MessagePosition pos = msg.MessagePositionAtPoint(graphics, new CommonPoint(e.X - MessagePadding.Left, e.Y - msg.Y), index);
                //Console.WriteLine($"pos: {pos.MessageIndex} : {pos.WordIndex} : {pos.SplitIndex} : {pos.CharIndex}");

                if (selection != null && mouseDown && leftClick)
                {
                    var newSelection = new Selection(selection.Start, pos);
                    if (!newSelection.Equals(selection))
                    {
                        selection = newSelection;
                        clearOtherSelections();
                        Invalidate();
                    }
                }

                if (word != null)
                {
                    if (word.Link != null)
                    {
                        Cursor = Cursors.Hand;
                    }
                    else if (word.Type == SpanType.Text)
                    {
                        Cursor = Cursors.IBeam;
                    }
                    else
                    {
                        Cursor = Cursors.Default;
                    }

                    if (word.Tooltip != null)
                    {
                        App.ShowToolTip(PointToScreen(new Point(e.Location.X + 16, e.Location.Y + 16)), word.Tooltip, word.TooltipImageUrl, word.TooltipImage);
                    }
                    else
                    {
                        App.HideToolTip();
                    }
                }
                else
                {
                    Cursor = Cursors.Default;
                    App.HideToolTip();
                }
            }

            graphics?.Dispose();

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Cursor = Cursors.Default;

            App.HideToolTip();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            
            mouseDown = true;

            leftClick = (e.Button == MouseButtons.Left);
            int index;

            var msg = MessageAtPoint(e.Location, out index);
            if (msg != null)
            {
                var graphics = App.UseDirectX ? null : CreateGraphics();
                MessagePosition position;
                if (e.Button == MouseButtons.Left) {
                    position = msg.MessagePositionAtPoint(graphics, new CommonPoint(e.X - MessagePadding.Left, e.Y - msg.Y), index);
                    selection = new Selection(position, position);
                }

                var word = msg.WordAtPoint(new CommonPoint(e.X - MessagePadding.Left, e.Y - msg.Y));
                mouseDownWord = word;
                if (word != null)
                {
                    if (e.Button == MouseButtons.Left) {
                        position = msg.MessagePositionAtPoint(graphics, new CommonPoint(word.X + 1, e.Y - msg.Y), index);
                        MessagePosition position2 = msg.MessagePositionAtPoint(graphics, new CommonPoint((word.X + 1 + word.Width), e.Y - msg.Y), index);
                        mouseDownSelection = new Selection(position, position2);
                    }
                    if (word.Link != null)
                    {
                        mouseDownLink = word.Link;
                    }
                }
                graphics?.Dispose();
            }
            else if (e.Button == MouseButtons.Left) {
                selection = null;
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseDown = false;

            int index;
            if (e.Button == MouseButtons.Right) {
                (App.MainForm.Selected as ChatControl)?.CloseAutocomplete();
            }
            var msg = MessageAtPoint(e.Location, out index);
            clickedMessage = msg;

            if (clickedMessage != null && clickedMessage.MessageId != null) {
                replyMenuItem.Visible = true;
                replyMenuItemLink.Visible = true;
            } else {
                replyMenuItem.Visible = false;
                replyMenuItemLink.Visible = false;
            }

            if (msg != null)
            {
                var word = msg.WordAtPoint(new CommonPoint(e.X - MessagePadding.Left, e.Y - msg.Y));
                if (word != null)
                {
                    if (mouseDownLink != null && mouseDownWord == word)
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            if (!AppSettings.ChatLinksDoubleClickOnly)
                            {
                                GuiEngine.Current.HandleLink(mouseDownLink);
                            }
                        }
                        else if (e.Button == MouseButtons.Right)
                        {
                            if (mouseDownLink.Type == LinkType.Url)
                            {
                                urlContextMenu.Show(this, e.Location);
                            }
                            else
                            {
                                GuiEngine.Current.HandleLink(mouseDownLink);
                            }
                        }
                    }
                }
            }
            
            if (e.Button == MouseButtons.Right && mouseDownLink == null) {
                normalContextMenu.Show(this, e.Location);
            }

            mouseDownLink = null;

            base.OnMouseUp(e);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            if (AppSettings.ChatLinksDoubleClickOnly && mouseDownLink != null)
            {
                GuiEngine.Current.HandleLink(mouseDownLink);
            } else {
                //select the word
                if (mouseDownWord != null && leftClick)
                {
                    clearOtherSelections();
                    Invalidate();
                    selection = mouseDownSelection;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            lock (bufferLock)
            {
                if (buffer != null)
                {
                    buffer.Dispose();
                    buffer = null;
                }
            }

            updateMessageBounds();
        }

        Brush lastReadMessageBrush = Brushes.Red;

        DateTime lastDrawn = DateTime.Now;

        bool redraw = false;

        public void ProposeInvalidation()
        {
            if (lastDrawn + TimeSpan.FromMilliseconds(1000 / 60f) > DateTime.Now)
            {
                redraw = true;
                return;
            }

            lastDrawn = DateTime.Now;

            redraw = false;
            Invalidate();

            Task.Delay(1000 / 60).ContinueWith(task =>
            {
                if (redraw)
                {
                    this.Invoke(() => Invalidate());
                }
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            lock (bufferLock)
            {
                lock (GuiEngine.Current.GifEmotesLock) {
                    try
                    {
                        GifEmotesOnScreen.Clear();
                        GuiEngine.Current.GifEmotesOnScreen.Clear();
                        if (buffer == null)
                        {
                            buffer = context.Allocate(e.Graphics, ClientRectangle);
                        }

                        var g = buffer.Graphics;
                        //var g = e.Graphics;

                        g.Clear((App.ColorScheme.ChatBackground as SolidBrush).Color);

                        var borderPen = Selected ? App.ColorScheme.ChatBorderFocused : App.ColorScheme.ChatBorder;

                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        // DRAW MESSAGES
                        var M = GetMessagesClone();

                        if (M != null && M.Length > 0)
                        {
                            var startIndex = Math.Max(0, (int)_scroll.Value);
                            if (startIndex < M.Length)
                            {
                                var yStart = MessagePadding.Top - (int)(M[startIndex].Height * (_scroll.Value % 1));
                                var h = Height - MessagePadding.Top - MessagePadding.Bottom;

                                if (startIndex < M.Length)
                                {
                                    var y = yStart;

                                    for (var i = startIndex; i < M.Length; i++)
                                    {
                                        var msg = M[i];
                                        var allowMessageSeparator = (!AppSettings.ChatShowLastReadMessageIndicator || i == 0 ||
                                            LastReadMessage != M[i-1]) && AllowMessageSeparator;
                                        MessageRenderer.DrawMessage(g, msg, MessagePadding.Left, y, 
                                            selection, i, !App.UseDirectX, GifEmotesOnScreen, 
                                            allowMessageSeperator: allowMessageSeparator);

                                        if (y - msg.Height > h)
                                        {
                                            break;
                                        }

                                        y += msg.Height;

                                        if (AppSettings.ChatShowLastReadMessageIndicator && 
                                            LastReadMessage == msg && i != M.Length - 1)
                                        {
                                            g.FillRectangle(lastReadMessageBrush, 0, y, Width, 1);
                                        }
                                    }

       
                                }

                                if (App.UseDirectX)
                                {
                                    SharpDX.Direct2D1.DeviceContextRenderTarget renderTarget = null;
                                    var dc = g.GetHdc();

                                    renderTarget = new SharpDX.Direct2D1.DeviceContextRenderTarget(MessageRenderer.D2D1Factory,
                                        MessageRenderer.RenderTargetProperties);

                                    renderTarget.BindDeviceContext(dc, new RawRectangle(0, 0, Width, Height));

                                    renderTarget.BeginDraw();

                                    //renderTarget.TextRenderingParams = new SharpDX.DirectWrite.RenderingParams(Fonts.Factory, 1, 1, 1, SharpDX.DirectWrite.PixelGeometry.Flat, SharpDX.DirectWrite.RenderingMode.CleartypeGdiClassic);
                                    renderTarget.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;

                                    var y = yStart;

                                    var brushes = new Dictionary<RawColor4, SCB>();

                                    var textColor = App.ColorScheme.Text;
                                    var textBrush = new SCB(renderTarget, 
                                        new RawColor4(textColor.R / 255f, textColor.G / 255f, textColor.B / 255f, 1));

                                    for (var i = startIndex; i < M.Length; i++)
                                    {
                                        var msg = M[i];

                                        foreach (var word in msg.Words)
                                        {
                                            if (word.Type == SpanType.Text)
                                            {
                                                SCB brush;

                                                if (word.Color == null)
                                                {
                                                    brush = textBrush;
                                                }
                                                else
                                                {
                                                    var hsl = word.Color.Value;

                                                    if (App.ColorScheme.IsLightTheme)
                                                    {
                                                        if (hsl.Saturation > 0.4f)
                                                        {
                                                            hsl = hsl.WithSaturation(0.4f);
                                                        }
                                                        if (hsl.Luminosity > 0.5f)
                                                        {
                                                            hsl = hsl.WithLuminosity(0.5f);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (hsl.Luminosity < 0.5f)
                                                        {
                                                            hsl = hsl.WithLuminosity(0.5f);
                                                        }

                                                        if (hsl.Luminosity < 0.6f && hsl.Hue > 0.54444 && hsl.Hue < 0.8333)
                                                        {
                                                            hsl = hsl.WithLuminosity(hsl.Luminosity + (float)Math.Sin((hsl.Hue - 0.54444) / (0.8333 - 0.54444) * Math.PI) * hsl.Saturation * 0.2f);
                                                        }

                                                        if (hsl.Luminosity < 0.8f && (hsl.Hue < 0.06 || hsl.Hue > 0.92))
                                                        {
                                                            hsl = hsl.WithLuminosity(hsl.Luminosity + (msg.HasAnyHighlightType(HighlightType.Highlighted) ? 0.27f : 0.1f) * hsl.Saturation);
                                                        }
                                                    }

                                                    if (hsl.Luminosity >= 0.95f)
                                                    {
                                                        hsl = hsl.WithLuminosity(0.95f);
                                                    }

                                                    float r, _g, b;
                                                    hsl.ToRGB(out r, out _g, out b);
                                                    var color = new RawColor4(r, _g, b, 1f);

                                                    if (!brushes.TryGetValue(color, out brush))
                                                    {
                                                        brushes[color] = brush = new SCB(renderTarget, color);
                                                    }
                                                }

                                                if (word.SplitSegments == null)
                                                {
                                                    renderTarget.DrawText((string)word.Value, Fonts.GetTextFormat(word.Font),
                                                        new RawRectangleF(MessagePadding.Left + word.X, y + word.Y, 10000, 10000), brush);
                                                }
                                                else
                                                {
                                                    foreach (var split in word.SplitSegments)
                                                        renderTarget.DrawText(split.Item1, Fonts.GetTextFormat(word.Font), 
                                                            new RawRectangleF(MessagePadding.Left + split.Item2.X, y + split.Item2.Y, 10000, 10000), brush);
                                                }
                                            }
                                        }

                                        if (y - msg.Height > h)
                                        {
                                            break;
                                        }

                                        y += msg.Height;
                                    }

                                    foreach (var b in brushes.Values)
                                    {
                                        b.Dispose();
                                    }

                                    renderTarget.EndDraw();

                                    textBrush.Dispose();
                                    g.ReleaseHdc(dc);
                                    renderTarget.Dispose();
                                }

                                {
                                    var y = yStart;

                                    Brush disabledBrush = new SolidBrush(Color.FromArgb(172, (App.ColorScheme.ChatBackground as SolidBrush)?.Color ?? Color.Black));
                                    for (var i = startIndex; i < M.Length; i++)
                                    {
                                        var msg = M[i];

                                        if (msg.Disabled)
                                        {
                                            g.SmoothingMode = SmoothingMode.None;

                                            g.FillRectangle(disabledBrush, 0, y + 1, Width, msg.Height - 1);
                                        }

                                        if (y - msg.Height > h)
                                        {
                                            break;
                                        }

                                        y += msg.Height;
                                    }
                                    disabledBrush.Dispose();
                                }
                            }
                        }

                        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

                        OnPaintOnBuffer(g);

                        buffer.Render(e.Graphics);
                    }
                    catch (Exception exc)
                    {
                        exc.Log("graphics");
                    }
                }
            }
        }

        protected virtual void OnPaintOnBuffer(Graphics g)
        {

        }

        public override void HandleKeys(Keys keys)
        {
            switch (keys)
            {
                case Keys.Control | Keys.C:
                    CopySelection(false);
                    break;

                case Keys.Control | Keys.X:
                    CopySelection(true);
                    break;

                default:
                    base.HandleKeys(keys);
                    break;
            }

            base.HandleKeys(keys);
        }

        // Public Functions
        public Message MessageAtPoint(Point p, out int index)
        {
            if (MessageLock != null)
            {
                lock (MessageLock)
                {
                    var messages = Messages;

                    for (var i = Math.Max(0, (int)_scroll.Value); i < messages.Length; i++)
                    {
                        var m = messages[i];
                        if (m.Y > p.Y - m.Height)
                        {
                            index = i;
                            return m;
                        }
                    }
                }
            }
            index = -1;
            return null;
        }

        public void CopySelection(bool clear)
        {
            string text = null;

            text = GetSelectedText(clear);

            if (!string.IsNullOrEmpty(text))
                Clipboard.SetText(text);
        }

        public virtual string GetSelectedText(bool clear)
        {
            if (selection == null || selection.IsEmpty)
                return null;

            var b = new StringBuilder();

            if (MessageLock != null)
            {
                lock (MessageLock)
                {
                    var messages = Messages;

                    var isFirstLine = true;

                    for (var currentLine = selection.First.MessageIndex; currentLine <= selection.Last.MessageIndex; currentLine++)
                    {
                        if (isFirstLine)
                        {
                            isFirstLine = false;
                        }
                        else
                        {
                            b.Append('\n');
                        }

                        var message = messages[currentLine];

                        var first = selection.First;
                        var last = selection.Last;

                        var appendNewline = false;

                        for (var i = 0; i < message.Words.Count; i++)
                        {
                            if ((currentLine != first.MessageIndex || i >= first.WordIndex) && (currentLine != last.MessageIndex || i <= last.WordIndex))
                            {
                                var word = message.Words[i];

                                if (appendNewline)
                                {
                                    appendNewline = false;
                                    b.Append(' ');
                                }

                                if (word.Type == SpanType.Text)
                                {
                                    for (var j = 0; j < (word.SplitSegments?.Length ?? 1); j++)
                                    {
                                        if ((first.MessageIndex == currentLine && first.WordIndex == i && first.SplitIndex > j) || (last.MessageIndex == currentLine && last.WordIndex == i && last.SplitIndex < j))
                                            continue;

                                        var split = word.SplitSegments?[j];
                                        var text = split?.Item1 ?? (string)word.Value;
                                        var rect = split?.Item2 ?? new CommonRectangle(word.X, word.Y, word.Width, word.Height);

                                        var textLength = text.Length;

                                        var offset = (first.MessageIndex == currentLine && first.SplitIndex == j && first.WordIndex == i) ? first.CharIndex : 0;
                                        var length = ((last.MessageIndex == currentLine && last.SplitIndex == j && last.WordIndex == i) ? last.CharIndex : textLength) - offset;

                                        b.Append(text.Substring(offset, length));

                                        if (j + 1 == (word.SplitSegments?.Length ?? 1) && ((last.MessageIndex > currentLine) || last.WordIndex > i))
                                            appendNewline = true;
                                    }
                                }
                                else if (word.Type == SpanType.LazyLoadedImage)
                                {
                                    var textLength = word.Type == SpanType.Text ? ((string)word.Value).Length : 2;

                                    var offset = (first.MessageIndex == currentLine && first.WordIndex == i) ? first.CharIndex : 0;
                                    var length = ((last.MessageIndex == currentLine && last.WordIndex == i) ? last.CharIndex : textLength) - offset;

                                    if (word.CopyText != null)
                                    {
                                        if (offset == 0)
                                            b.Append(word.CopyText);
                                        if (offset + length == 2 && word.HasTrailingSpace)
                                            appendNewline = true;
                                    }
                                }
                            }
                        }
                    }
                }

            }

            return b.ToString();
        }

        public void ClearSelection()
        {
            if (!(selection?.IsEmpty ?? true))
            {
                selection = null;

                Invalidate();
            }
        }

        static object lastReadMessageTag = "LastReadMessage";

        public void SetLastReadMessage()
        {
            if (MessageLock != null)
            {
                lock (MessageLock)
                {
                    if (Messages.Length != 0)
                    {
                        LastReadMessage = Messages[Messages.Length - 1];

                        _scroll.RemoveHighlightsWhere(highlight => highlight.Tag == lastReadMessageTag);

                        if (AppSettings.ChatShowLastReadMessageIndicator)
                        {
                            _scroll.AddHighlight(Messages.Length - 1, Color.Red, ScrollBarHighlightStyle.SingleLine,
                                lastReadMessageTag);
                        }
                    }
                }
            }
        }

        // Private Helpers
        protected virtual void clearOtherSelections() { }

        protected virtual void updateMessageBounds(bool emoteChanged = false)
        {
            object g = App.UseDirectX ? null : CreateGraphics();

            // determine if
            double scrollbarThumbHeight = 0;
            var totalHeight = Height - MessagePadding.Top - MessagePadding.Bottom;
            var currentHeight = 0;
            var tmpHeight = Height - MessagePadding.Top - MessagePadding.Bottom;
            var enableScrollbar = false;
            var messageCount = 0;

            if (MessageLock != null)
            {
                lock (MessageLock)
                {

                    var messages = Messages;
                    messageCount = messages.Length;

                    var visibleStart = Math.Max(0, (int)_scroll.Value);

                    // set EmotesChanged for messages
                    if (emoteChanged)
                    {
                        for (var i = 0; i < messages.Length; i++)
                        {
                            messages[i].EmoteBoundsChanged = true;
                        }
                    }

                    // calculate bounds for visible messages
                    for (var i = visibleStart; i < messages.Length; i++)
                    {
                        var msg = messages[i];

                        msg.CalculateBounds(g, Width - MessagePadding.Left - MessagePadding.Right, EnableHatEmotes);
                        currentHeight += msg.Height;

                        if (currentHeight > totalHeight)
                        {
                            break;
                        }
                    }

                    // calculate bounds for messages at the bottom to determine the size of the scrollbar thumb
                    for (var i = messages.Length - 1; i >= 0; i--)
                    {
                        var msg = messages[i];
                        msg.CalculateBounds(g, Width - MessagePadding.Left - MessagePadding.Right, EnableHatEmotes);
                        scrollbarThumbHeight++;

                        tmpHeight -= msg.Height;
                        if (tmpHeight < 0)
                        {
                            enableScrollbar = true;
                            scrollbarThumbHeight -= 1 - (double)tmpHeight / msg.Height;
                            break;
                        }
                    }
                }
            }

            (g as Graphics)?.Dispose();

            this.Invoke(() =>
            {
                try
                {
                    if (enableScrollbar)
                    {
                        _scroll.Enabled = true;
                        _scroll.LargeChange = scrollbarThumbHeight;
                        _scroll.Maximum = messageCount - 1;

                        if (scrollAtBottom)
                            _scroll.Value = messageCount - scrollbarThumbHeight;
                    }
                    else
                    {
                        _scroll.Enabled = false;
                        _scroll.Value = 0;
                    }
                }
                catch { }
            });
        }

        protected void checkScrollBarPosition()
        {
            scrollAtBottom = !_scroll.Enabled || _scroll.Maximum < _scroll.Value + _scroll.LargeChange + 0.0001;
        }
    }
}
