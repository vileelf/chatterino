using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chatterino.Common;

namespace Chatterino.Controls
{
    public class ToolTip : Form
    {
        private string tooltip = null;

        public string TooltipText
        {
            get { return tooltip; }
            set
            {
                if (tooltip != value)
                {
                    tooltip = value;
                    redraw();
                }
            }
        }
        
        private Image image;

        private LazyLoadedImage _image;

        public LazyLoadedImage Image
        {
            get { return _image; }
            set
            { 
                _image = value;
                redraw();
            }
        }

        void calcSize()
        {
            if (tooltip != null)
            {
                var size = CreateGraphics().MeasureString(tooltip, Font, 1000, format);
                if (image != null) {
                    lock (image) {
                        Size = new Size(Math.Max( Padding.Left + image.Width + Padding.Right, Padding.Left + (int)size.Width + Padding.Right), image.Height + 8 + Padding.Top + (int)size.Height + Padding.Bottom);
                    }
                } else {
                    Size = new Size(Math.Max( Padding.Left + Padding.Right, Padding.Left + (int)size.Width + Padding.Right), Padding.Top + (int)size.Height + Padding.Bottom);
                }
            }
        }
        
        public void redraw() {
            image = _image?.Image;
            calcSize();
            Invalidate();
            Update();
        }
        
        public ToolTip()
        {
            Font = Fonts.GetFont(Common.FontType.Small);

            Fonts.FontChanged += (s, e) => Font = Fonts.GetFont(Common.FontType.Small);

            FormBorderStyle = FormBorderStyle.None;
            Opacity = 0.8;

            Padding = new Padding(8, 4, 8, 4);
            ShowInTaskbar = false;
            
            this.Owner = App.MainForm;

            StartPosition = FormStartPosition.Manual;
            
            this.DoubleBuffered = true;

            //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //Win32.EnableWindowBlur(Handle);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        private const int WS_EX_TOPMOST = 0x00000008;
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOPMOST;
                return createParams;
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            TooltipText = TooltipText;

            base.OnFontChanged(e);
        }

        static StringFormat format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(App.ColorScheme.TooltipBackground, e.ClipRectangle);

            if (image != null)
            {
                lock (image) {
                    e.Graphics.DrawImage(image, 4, 4, image.Width, image.Height);
                }
            }

            if (tooltip != null)
            {
                e.Graphics.DrawString(tooltip, Font, App.ColorScheme.TooltipText, new Rectangle(0, image?.Height ?? 0, Width, Height - (image?.Height ?? 0)), format);
            }
        }
    }
}
