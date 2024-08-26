using Chatterino.Common;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Chatterino.Controls {
    public class AutoComplete : Form
    {
        
        public void redraw() {
            Invalidate();
            Update();
        }

        private ListBox AutoCompleteListBox = new AutoCompleteListBox();
        private CustomScrollBar CustomScrollBar = new CustomScrollBar() {
            SmallChange = 1,
            Minimum = 0
        };
        
        public string []items {get;private set;} = null;
        private int selected = 0;
        private ChatControl _chatControl = null;
        private Brush textbrush = Brushes.Black;
        private Color backcolor = Color.White;

        public AutoComplete(ChatControl chatControl) {
            FormBorderStyle = FormBorderStyle.None;

            Padding = new Padding(8, 4, 8, 4);
            ShowInTaskbar = false;

            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.Selectable, false);
            DoubleBuffered = true;

            Size = new Size(chatControl.Width < 300 ? chatControl.Width : 300, 95);

            CustomScrollBar.Size = new Size(SystemInformation.VerticalScrollBarWidth, Height - 1);
            CustomScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            CustomScrollBar.Scroll += (s, e) => {
                if (CustomScrollBar.Value < 0 || CustomScrollBar.Value >= items.Length) {
                    return;
                }
                selected = (int)CustomScrollBar.Value;
                AutoCompleteListBox.SetSelected(selected, true);
                CustomScrollBar.Invalidate();
            };

            AutoCompleteListBox.Size = new Size(Width - SystemInformation.VerticalScrollBarWidth, Height);
            AutoCompleteListBox.DrawMode = DrawMode.OwnerDrawFixed;
            AutoCompleteListBox.DrawItem += new DrawItemEventHandler(ListBox_DrawItem);
            AutoCompleteListBox.Enabled = false;
            Enabled = true;
            Controls.Add(AutoCompleteListBox);
            Controls.Add(CustomScrollBar);
            _chatControl = chatControl;
            AppSettings.ThemeChanged += (s, e) => setColors();
            setColors();
        }
        
        private void setColors() {
            if (App.ColorScheme.IsLightTheme) {
                textbrush = Brushes.Black;
                backcolor = Color.White;
                AutoCompleteListBox.BackColor = backcolor;
            } else {
                textbrush = Brushes.White;
                backcolor = Color.Black;
                AutoCompleteListBox.BackColor = backcolor;
            }
        }
        
        public void UpdateItems(string []items) {
            if (items == null) {
                items = new string[1] {""};
            } else if (items.Length==0) {
                Array.Resize(ref items, 1);
                items[0] = "";
            }
            this.items = items;
            AutoCompleteListBox.Items.Clear();
            AutoCompleteListBox.Items.AddRange(items);
            CustomScrollBar.Maximum = items.Length - 1;
            CustomScrollBar.Value = 0;
            if (items.Length > 6) {
                CustomScrollBar.Enabled = true;
            } else {
                CustomScrollBar.Enabled = false;
            }
            selected = 0;
            AutoCompleteListBox.SetSelected(0, true);
            CustomScrollBar.Invalidate();
        }
        
        public void ClearItems() {
            AutoCompleteListBox.Items.Clear();
            items = null;
            selected = 0;
            CustomScrollBar.Maximum = 0;
            CustomScrollBar.Value = 0;
            CustomScrollBar.Enabled = false;
        }
        
        public void UpdateLocation(int left, int top) {
            Location = new Point(left, top-this.Height);
            _chatControl = App.MainForm.Selected as ChatControl;
            Size = new Size(_chatControl.Width < 300 ? _chatControl.Width : 300, 95);
            CustomScrollBar.Size = new Size(SystemInformation.VerticalScrollBarWidth, Height - 1);
            CustomScrollBar.Location = new Point(Width - SystemInformation.VerticalScrollBarWidth - 1, 1);
            AutoCompleteListBox.Size = new Size(Width - (CustomScrollBar.Enabled ? SystemInformation.VerticalScrollBarWidth : 0), Height);
        }
        
        public void MoveSelection(bool up) {
            if (up) {
                selected--;
                if (selected < 0) {
                    selected = items.Length-1;
                }
            } else {
                selected++;
                if (selected >= items.Length) {
                    selected = 0;
                }
            }
            AutoCompleteListBox.SetSelected(selected, true);
            CustomScrollBar.Value = selected;
            CustomScrollBar.Invalidate();
        }
        
        public int GetSelectionIndex() {
            return AutoCompleteListBox.SelectedIndex;
        }
        
        public string GetSelection() {
            if (items != null) {
                string item = (string)AutoCompleteListBox.SelectedItem;
                return item;
            }
            
            return "";
        }
        
        const int WHEEL_DELTA = 120;
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int numticks = e.Delta/WHEEL_DELTA;
            selected -= numticks;
            if (selected<0) {
                selected = 0;
            } else if (selected >= items.Length) {
                selected = items.Length - 1;
            }
            AutoCompleteListBox.SetSelected(selected, true);
            CustomScrollBar.Value = selected;
            CustomScrollBar.Invalidate();
            base.OnMouseWheel(e);
        }
        
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOPMOST | WS_EX_NOACTIVATE;
                return createParams;
            }
        }
        
        private const int WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 0x0003;

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE) 
            {
                 m.Result = (IntPtr)MA_NOACTIVATE;
                 return;
            }
            base.WndProc(ref m);
        }
        
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
        
        protected override void OnFontChanged(EventArgs e)
        {
            redraw();

            base.OnFontChanged(e);
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index;
            index = AutoCompleteListBox.IndexFromPoint(e.X, e.Y);
            if (index != -1) {
                selected = index;
                AutoCompleteListBox.SetSelected(selected, true);
                CustomScrollBar.Value = selected;
                CustomScrollBar.Invalidate();
            }
            base.OnMouseMove(e);
        }
        
        protected override void OnMouseClick(MouseEventArgs e)
        {
            int index;
            index = AutoCompleteListBox.IndexFromPoint(e.X,e.Y);
            if (index != -1) {
                selected = index;
                AutoCompleteListBox.SetSelected(selected, true);
                _chatControl = App.MainForm.Selected as ChatControl;
                _chatControl.SelectAutoComplete();
            }
            base.OnMouseClick(e);
        }
        
        private void ListBox_DrawItem(object sender, 
            DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.Graphics.DrawString(AutoCompleteListBox.Items[e.Index].ToString(), 
                e.Font, textbrush, e.Bounds, StringFormat.GenericDefault);
            e.DrawFocusRectangle();
        }
    }
}