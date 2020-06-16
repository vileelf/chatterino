using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Chatterino.Common;

namespace Chatterino.Controls
{
    public class AutoComplete : Form
    {
        
        public void redraw() {
            Invalidate();
            Update();
        }
        
        private ListBox AutoCompleteListBox = new ListBox();
        
        public string []items {get;private set;} = null;
        private int selected = 0;
        private ChatControl _chatControl = null;
        private Brush textbrush = Brushes.Black;
        private Color backcolor = Color.White;
        
        public AutoComplete(ChatControl chatControl) {
            //Font = Fonts.GetFont(Common.FontType.Small);

            //Fonts.FontChanged += (s, e) => Font = Fonts.GetFont(Common.FontType.Small);

            FormBorderStyle = FormBorderStyle.None;

            Padding = new Padding(8, 4, 8, 4);
            ShowInTaskbar = false;
            
            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.Selectable, false);
            this.DoubleBuffered = true;
            
            //AutoCompleteListBox.Location = new Point(81, 69);
            AutoCompleteListBox.Size = new Size(chatControl.Width<300?chatControl.Width:300, 95);
            Size = AutoCompleteListBox.Size;
            AutoCompleteListBox.DrawMode = DrawMode.OwnerDrawFixed;
            AutoCompleteListBox.DrawItem += new DrawItemEventHandler(ListBox_DrawItem);
            AutoCompleteListBox.Enabled = false;
            this.Enabled = false;
            Controls.Add(AutoCompleteListBox);
            this._chatControl = chatControl;
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
            selected = 0;
            AutoCompleteListBox.SetSelected(0, true);
        }
        
        public void ClearItems() {
            AutoCompleteListBox.Items.Clear();
            items = null;
            selected = 0;
        }
        
        public void UpdateLocation(int left, int top) {
            this.Location = new Point(left, top-this.Height);
            this._chatControl = App.MainForm.Selected as ChatControl;
            AutoCompleteListBox.Size = new Size(_chatControl.Width<300?_chatControl.Width:300, 95);
            Size = AutoCompleteListBox.Size;
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
            index = AutoCompleteListBox.IndexFromPoint(e.X,e.Y);
            if(index!=-1) {
                selected = index;
                AutoCompleteListBox.SetSelected(selected, true);
            }
            base.OnMouseMove(e);
        }
        
        protected override void OnMouseClick(MouseEventArgs e)
        {
            int index;
            index = AutoCompleteListBox.IndexFromPoint(e.X,e.Y);
            if(index!=-1) {
                selected = index;
                AutoCompleteListBox.SetSelected(selected, true);
                this._chatControl = App.MainForm.Selected as ChatControl;
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