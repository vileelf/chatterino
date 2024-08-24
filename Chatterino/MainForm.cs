using Chatterino.Common;
using Chatterino.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Chatterino {
    public partial class MainForm : Form
    {
        private ColumnTabPage lastTabPage = null;
        private bool hasLoadedSize = false;
        public MainForm()
        {
            InitializeComponent();

            // set window bounds
            try
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Math.Max(0, AppSettings.WindowX), Math.Max(0, AppSettings.WindowY));
                Size = new Size(Math.Max(AppSettings.WindowWidth, 200), Math.Max(AppSettings.WindowHeight, 200));
                hasLoadedSize = true;
            }
            catch { }

            // top most
            TopMost = AppSettings.WindowTopMost;

            AppSettings.WindowTopMostChanged += (s, e) =>
            {
                TopMost = AppSettings.WindowTopMost;
            };

            // icon
            Icon = App.Icon;

            TwitchChannelJoiner.runChannelLoop();

            // load layout
            tabControl.LoadLayout(Path.Combine(Util.GetUserDataPath(), "Layout.xml"));

#if !DEBUG
            if (AppSettings.CurrentVersion != App.CurrentVersion.ToString())
            {
                AppSettings.CurrentVersion = App.CurrentVersion.ToString();

                if (App.CanShowChangelogs)
                {
                    ShowChangelog();
                    AppSettings.Save(null);
                }
            }
#endif

            // set title
            SetTitle();

            IrcManager.LoggedIn += (s, e) => SetTitle();

            ChatCommands.addChatCommands();

            // winforms specific
            BackColor = Color.Black;

            KeyPreview = true;

            Activated += (s, e) =>
            {
                App.WindowFocused = true;
            };

            Deactivate += (s, e) =>
            {
                App.WindowFocused = false;
                App.HideToolTip();
                (selected as ChatControl)?.CloseAutocomplete();

                setTabPageLastMessageThingy(tabControl.Selected as ColumnTabPage);

                Refresh();
            };

            tabControl.TabPageSelected += (s, e) =>
            {
                var tab = e.Value as ColumnTabPage;

                if (lastTabPage != null)
                {
                    lastTabPage.LastSelected = Selected;
                    setTabPageLastMessageThingy(lastTabPage);
                }

                if (tab != null)
                {
                    if (tab.LastSelected != null && tab.Columns.SelectMany(x => x.Widgets).Contains(tab.LastSelected))
                        Selected = tab.LastSelected;
                    else
                        Selected = tab?.Columns.FirstOrDefault()?.Widgets.FirstOrDefault();

                    Selected?.Focus();
                    (Selected as ChatControl)?.CloseAutocomplete();
                    if(Selected != null && (Selected as ChatControl).Channel != null) {
                        TwitchChannel.SelectedChannel = (Selected as ChatControl).Channel;
                    }
                    if (lastTabPage != null) {
                         foreach (var col in lastTabPage.Columns)
                        {
                            foreach (var control in col.Widgets)
                            {
                                var container = control as MessageContainerControl;

                                container.ClearBuffer();
                            }
                        }
                    }
                }

                lastTabPage = tab;
            };

            lastTabPage = tabControl.Selected as ColumnTabPage;
        }

        public IEnumerable<ColumnLayoutItem> VisibleSplits
        {
            get
            {
                return
                    ((ColumnTabPage) tabControl.Selected).Columns
                        .SelectMany(x => x.Widgets);
            }
        }

        private void setTabPageLastMessageThingy(ColumnTabPage page)
        {
            if (page == null) return;

            foreach (var col in page.Columns)
            {
                foreach (var control in col.Widgets)
                {
                    var container = control as MessageContainerControl;

                    container?.SetLastReadMessage();
                }
            }
        }

        private SearchDialog dialog = null;
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.F:
                    (selected as ChatControl)?.CloseAutocomplete();
                    if (dialog != null) {
                        dialog.Close();
                        dialog = null;
                    }
                    dialog = new SearchDialog("Search (results highlighted green)", (res, value) => {
                         if (res == DialogResult.Yes) { //next
                            (selected as ChatControl)?.SearchNext(value, true);
                         } else if (res == DialogResult.No) { //prev
                            (selected as ChatControl)?.SearchNext(value, false);
                         } else { //cancel
                            (selected as ChatControl)?.SearchFor("");
                         }
                         if (res != DialogResult.Yes && res!= DialogResult.No) {
                            dialog = null;
                         }
                     }) { Value = (selected as MessageContainerControl)?.GetSelectedText(false) };
                     dialog.Show();
                    break;
                case Keys.Control | Keys.U:
                    (selected as ChatControl)?.CloseAutocomplete();
                    tabControl.ShowUserSwitchPopup();
                    break;
                case Keys.Control | Keys.T:
                    (selected as ChatControl)?.CloseAutocomplete();
                    AddNewSplit();
                    break;
                case Keys.Control | Keys.W:
                    (selected as ChatControl)?.CloseAutocomplete();
                    RemoveSelectedSplit();
                    break;
                case Keys.Control | Keys.P:
                    (selected as ChatControl)?.CloseAutocomplete();
                    App.ShowSettings();
                    break;
                case Keys.Control | Keys.L:
                    (selected as ChatControl)?.CloseAutocomplete();
                    new LoginForm().ShowDialog();
                    break;
                case Keys.Alt | Keys.Left:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var tab = tabControl.Selected as ColumnTabPage;

                        if (tab != null && selected != null)
                        {
                            var index = tab.Columns.TakeWhile(x => !x.Widgets.Contains(selected)).Count();

                            if (index > 0)
                            {
                                var newCol = tab.Columns.ElementAt(index - 1);
                                Selected = newCol.Widgets.ElementAtOrDefault(tab.Columns.ElementAt(index).Widgets.TakeWhile(x => x != selected).Count()) ?? newCol.Widgets.Last();
                            }
                        }
                    }
                    break;
                case Keys.Alt | Keys.Right:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var tab = tabControl.Selected as ColumnTabPage;

                        if (tab != null && selected != null)
                        {
                            var index = tab.Columns.TakeWhile(x => !x.Widgets.Contains(selected)).Count();

                            if (index + 1 < tab.ColumnCount)
                            {
                                var newCol = tab.Columns.ElementAt(index + 1);
                                Selected = newCol.Widgets.ElementAtOrDefault(tab.Columns.ElementAt(index).Widgets.TakeWhile(x => x != selected).Count()) ?? newCol.Widgets.Last();
                            }
                        }
                    }
                    break;
                case Keys.Alt | Keys.Up:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var tab = tabControl.Selected as ColumnTabPage;

                        if (tab != null && selected != null)
                        {
                            var col = tab.Columns.First(x => x.Widgets.Contains(selected));

                            var index = col.Widgets.TakeWhile(x => x != selected).Count();

                            if (index > 0)
                            {
                                Selected = col.Widgets.ElementAt(index - 1);
                            }
                        }
                    }
                    break;
                case Keys.Alt | Keys.Down:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var tab = tabControl.Selected as ColumnTabPage;

                        if (tab != null && selected != null)
                        {
                            var col = tab.Columns.First(x => x.Widgets.Contains(selected));

                            var index = col.Widgets.TakeWhile(x => x != selected).Count();

                            if (index + 1 < col.WidgetCount)
                            {
                                Selected = col.Widgets.ElementAt(index + 1);
                            }
                        }
                    }
                    break;
                case Keys.Control | Keys.D1:
                case Keys.Control | Keys.D2:
                case Keys.Control | Keys.D3:
                case Keys.Control | Keys.D4:
                case Keys.Control | Keys.D5:
                case Keys.Control | Keys.D6:
                case Keys.Control | Keys.D7:
                case Keys.Control | Keys.D8:
                case Keys.Control | Keys.D9:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var tab = (keyData & ~Keys.Modifiers) - Keys.D0;

                        var t = tabControl.TabPages.ElementAtOrDefault(tab - 1);

                        if (t != null)
                        {
                            TabControl.Select(t);
                        }
                    }
                    break;
                case Keys.Control | Keys.Tab:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var index = tabControl.TabPages.TakeWhile(x => !x.Selected).Count();

                        if (tabControl.TabPages.Count() > index + 1)
                        {
                            tabControl.Select(tabControl.TabPages.ElementAt(index + 1));
                        }
                        else
                        {
                            tabControl.Select(tabControl.TabPages.ElementAt(0));
                        }
                    }
                    break;
                case Keys.Control | Keys.Shift | Keys.Tab:
                    {
                        (selected as ChatControl)?.CloseAutocomplete();
                        var index = tabControl.TabPages.TakeWhile(x => !x.Selected).Count();

                        if (index > 0)
                        {
                            tabControl.Select(tabControl.TabPages.ElementAt(index - 1));
                        }
                        else
                        {
                            tabControl.Select(tabControl.TabPages.ElementAt(tabControl.TabPages.Count() - 1));
                        }
                    }
                    break;
                case Keys.Tab:
                case Keys.Shift | Keys.Tab:
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    selected?.HandleKeys(keyData);
                    break;
                default:

                    return false;
            }

            return true;
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true;
        }

        public void SetTitle()
        {
            this.Invoke(() => Text = $"{(IrcManager.Account.IsAnon ? "<not logged in>" : IrcManager.Account.Username)} - Chatterino Classic for Twitch (v" + App.CurrentVersion.ToString()
#if DEBUG
            + " dev"
#endif
            + ")"
            );
        }

        ColumnLayoutItem selected = null;

        public ColumnLayoutItem Selected
        {
            get
            {
                return selected;
                //return tabControl.TabPages.SelectMany(a => ((ColumnTabPage)a).Columns.SelectMany(b => b.Widgets)).FirstOrDefault(c => c.Focused);
            }
            internal set
            {
                if (selected != value)
                {
                    selected = value;

                    value?.Focus();

                    App.SetEmoteListChannel((selected as ChatControl)?.Channel);
                }
            }
        }

        public Controls.TabControl TabControl
        {
            get
            {
                return tabControl;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        public void AddNewSplit()
        {
            var chatControl = new ChatControl();

            (tabControl.Selected as ColumnTabPage)?.AddColumn()?.Process(col =>
            {
                col.AddWidget(chatControl);
                col.Widgets.FirstOrDefault()?.Focus();
            });

            RenameSelectedSplit();
        }

        public void RemoveSelectedSplit()
        {
            var selected = Selected;

            if (selected != null)
            {
                ChatColumn column = null;
                ColumnTabPage _page = null;

                foreach (ColumnTabPage page in tabControl.TabPages.Where(x => x is ColumnTabPage))
                {
                    foreach (var c in page.Columns)
                    {
                        if (c.Widgets.Contains(selected))
                        {
                            _page = page;
                            column = c;
                            break;
                        }
                    }
                }

                if (column != null)
                {
                    column.RemoveWidget(selected);

                    if (column.WidgetCount == 0)
                        _page.RemoveColumn(column);
                }

                selected.Dispose();
            }
        }

        public void RenameSelectedSplit()
        {
            var focused = Selected as ChatControl;
            if (focused != null)
            {
                using (var dialog = new InputDialogForm("channel name") { Value = focused.ChannelName })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        focused.ChannelName = dialog.Value;
                    }
                }
            }
        }

        protected override void OnLocationChanged(EventArgs e) {
            if (!hasLoadedSize) { base.OnLocationChanged(e); return; }
            AppSettings.WindowX = Location.X;
            AppSettings.WindowY = Location.Y;
            AppSettings.Save(null);
            base.OnLocationChanged(e);
        }

        protected override void OnSizeChanged(EventArgs e) {
            if (!hasLoadedSize) { base.OnSizeChanged(e); return; }
            AppSettings.WindowWidth = Width;
            AppSettings.WindowHeight = Height;
            AppSettings.Save(null);
            base.OnSizeChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            
            TwitchChannelJoiner.stopChannelLoop();

            AppSettings.WindowX = Location.X;
            AppSettings.WindowY = Location.Y;
            AppSettings.WindowWidth = Width;
            AppSettings.WindowHeight = Height;

            base.OnClosing(e);
        }

        public void ShowChangelog()
        {
            try
            {
                if (File.Exists("./Changelog.md"))
                {
                    var page = new ColumnTabPage(TabControl);
                    page.CustomTitle = "Changelog";

                    page.AddColumn(new ChatColumn(new ChangelogControl(File.ReadAllText("./Changelog.md"))));

                    tabControl.InsertTab(0, page, true);
                }
            }
            catch { }
        }
    }
}
