﻿using Chatterino.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chatterino.Controls
{
    public partial class TabControl : Control
    {
        public event EventHandler<ValueEventArgs<TabPage>> TabPageSelected;

        public IEnumerable<TabPage> TabPages
        {
            get
            {
                return _tabPages.Select(x => x.Item2);
            }
        }


        List<Tuple<Tab, TabPage>> _tabPages = new List<Tuple<Tab, TabPage>>();
        public TabPage Selected { get; private set; }

        private bool IsLoading = false;

        public string SavePath { get; } = Path.Combine(Util.GetUserDataPath(), "Layout.xml");

        Tuple<Tab, TabPage> _selected
        {
            get
            {
                if (Selected == null)
                {
                    return null;
                }

                var index = _tabPages.FindIndex(x => x.Item2 == Selected);

                if (index == -1)
                {
                    return null;
                }

                return _tabPages[index];
            }
        }

        Button dropDownButton = new Button();
        MoreButton newTabButton = new MoreButton();
        MoreButton settingsButton = new MoreButton();
        MoreButton userButton = new MoreButton();
        static Image dropDownImage = null;

        static TabControl()
        {
            try
            {
                dropDownImage = Properties.Resources.ExpandChevronDown_16x;
            }
            catch
            {

            }
        }

        public TabControl()
        {
            SizeChanged += (s, e) =>
            {
                layout();
            };

            // drop down button
            dropDownButton.FlatStyle = FlatStyle.Flat;

            dropDownButton.Width = 24;
            dropDownButton.Height = 23;

            dropDownButton.Image = dropDownImage;

            Controls.Add(dropDownButton);

            dropDownButton.Click += (s, e) =>
            {
                var menu = new ContextMenu();

                foreach (var t in _tabPages)
                {
                    if (!t.Item1.Visible)
                    {
                        menu.MenuItems.Add(new MenuItem(t.Item2.Title));
                    }
                }

                menu.Show(dropDownButton, new Point(dropDownButton.Width, dropDownButton.Height), LeftRightAlignment.Left);
            };

            // settings button
            Controls.Add(settingsButton);
            settingsButton.Size = new Size(24, 24);
            settingsButton.Icon = MoreButtonIcon.Settings;

            settingsButton.Click += (s, e) =>
            {
                App.ShowSettings();
            };

            // user button
            Controls.Add(userButton);
            userButton.Size = new Size(24, 24);
            userButton.Location = new Point(24, 0);
            userButton.Icon = MoreButtonIcon.User;

            userButton.Click += (s, e) =>
            {
                ShowUserSwitchPopup();
            };

            // add tab button
            Controls.Add(newTabButton);
            newTabButton.Size = new Size(24, 24);

            newTabButton.Click += (s, e) =>
            {
                AddTab(new ColumnTabPage(this), true);
            };

            // colors
            App_ColorSchemeChanged(null, null);
            App.ColorSchemeChanged += App_ColorSchemeChanged;
        }

        public void ShowUserSwitchPopup()
        {
            var loc = userButton.PointToScreen(new Point(0, userButton.Height));

            new UserSwitchPopup
            {
                StartPosition = FormStartPosition.Manual,
                Location = loc
            }.Show();
        }

        protected override void Dispose(bool disposing)
        {
            App.ColorSchemeChanged -= App_ColorSchemeChanged;

            base.Dispose(disposing);
        }

        private void App_ColorSchemeChanged(object sender, EventArgs e)
        {
            BackColor = App.ColorScheme.TabPanelBG;
        }

        private void layout()
        {
            if (Bounds.Height > 0 && Bounds.Width > 0)
            {
                // calc tabs
                int maxLines = int.MaxValue, currentLine = 0;
                var lineHeight = Tab.GetHeight();

                int x = 48, y = 0, w = Bounds.Width - dropDownButton.Width;
                var firstInline = true;

                var allTabsVisible = true;

                // go through all the tabs
                for (var i = 0; i < _tabPages.Count; i++)
                {
                    var t = _tabPages[i];

                    // tab doesn't fit in line
                    if (!firstInline && x + t.Item1.Width > w)
                    {
                        // if can't add new line
                        if (currentLine + 1 >= maxLines)
                        {
                            allTabsVisible = false;

                            for (; i < _tabPages.Count; i++)
                            {
                                // do something with the tabs that are not on screen
                                _tabPages[i].Item1.Visible = false;
                            }
                            break;
                        }

                        currentLine++;

                        y += lineHeight;
                        t.Item1.Visible = true;
                        t.Item1.SetBounds(0, y, t.Item1.Width, lineHeight);

                        x = t.Item1.Width;
                    }
                    // tab doesn't fit in line
                    else
                    {
                        t.Item1.Visible = true;
                        t.Item1.SetBounds(x, y, t.Item1.Width, lineHeight);

                        x += t.Item1.Width;
                    }
                    firstInline = false;
                }

                newTabButton.Location = new Point(x, y);

                //dropDownButton.Location = new Point(Width - dropDownButton.Width - newTabButton.Width, y);
                //newTabButton.Location = new Point(Width - newTabButton.Width, y);

                dropDownButton.Visible = !allTabsVisible;

                y += Tab.GetHeight();

                Selected?.SetBounds(0, y, Bounds.Width, Math.Max(1, Bounds.Height - y));
            }
        }

        public void TabChanged()
        {
            if (!IsLoading)
            {
                SaveLayout(SavePath);
            }
        }

        // public
        public void AddTab(TabPage page, bool select = false)
        {
            var tab = new Tab(this, page);

            _tabPages.Add(new Tuple<Tab, TabPage>(tab, page));

            Controls.Add(tab);

            if (select || _tabPages.Count == 1)
            {
                this.Select(page);
            }

            layout();
            TabChanged();
        }

        public void InsertTab(int index, TabPage page, bool select = false)
        {
            var tab = new Tab(this, page);

            _tabPages.Insert(index, new Tuple<Tab, TabPage>(tab, page));

            Controls.Add(tab);

            if (select || _tabPages.Count == 1)
            {
                this.Select(page);
            }

            layout();
            TabChanged();
        }

        public void RemoveTab(TabPage page)
        {
            var index = _tabPages.FindIndex(x => x.Item2 == page);

            if (index == -1)
                throw new ArgumentException("\"child\" is not a child of this control.");

            var ctab = page as ColumnTabPage;

            if (ctab != null)
            {
                foreach (ChatControl c in ctab.Columns.SelectMany(x => x.Widgets).Where(w => w is ChatControl))
                {
                    TwitchChannel.RemoveChannel(c.ChannelName);
                }
            }

            Controls.Remove(_tabPages[index].Item1);
            _tabPages.RemoveAt(index);

            Controls.Remove(page);

            bool tabAdded = false;
            if (index < _tabPages.Count)
            {
                Select(_tabPages[index].Item2);
            }
            else if (_tabPages.Count > 0)
            {
                Select(_tabPages[index - 1].Item2);
            }
            else
            {
                var p = new ColumnTabPage(this);
                AddTab(p);
                tabAdded = true;
            }
            if (!tabAdded)
            {
                TabChanged();
            }
        }

        public void Select(TabPage page)
        {
            var s = _selected;

            if (s != null)
            {
                s.Item1.Selected = false;
                s.Item2.Selected = false;
                Controls.Remove(s.Item2);
            }

            Selected = page;
            s = _selected;

            if (Selected != null)
            {
                s.Item1.Selected = true;
                s.Item2.Selected = true;
                Controls.Add(s.Item2);
                layout();
                TabPageSelected?.Invoke(this, new ValueEventArgs<TabPage>(page));
            }
        }

        public void LoadLayout(string path)
        {
            IsLoading = true;
            try
            {
                if (File.Exists(path))
                {
                    var doc = XDocument.Load(path);

                    doc.Root.Process(root =>
                    {
                        foreach (var tab in doc.Elements().First().Elements("tab"))
                        {

                            var page = new ColumnTabPage(this);

                            page.CustomTitle = tab.Attribute("title")?.Value;

                            page.EnableNewMessageHighlights = (tab.Attribute("enableNewMessageHighlights")?.Value?.ToUpper() ?? "TRUE") == "TRUE";

                            page.EnableHighlightedMessageHighlights = (tab.Attribute("enableHighlightedMessageHighlights")?.Value?.ToUpper() ?? "TRUE") == "TRUE";

                            page.EnableGoLiveHighlights = (tab.Attribute("enableGoLiveHighlights")?.Value?.ToUpper() ?? "TRUE") == "TRUE";

                            page.EnableGoLiveNotifications = (tab.Attribute("enableGoLiveNotifications")?.Value?.ToUpper() ?? "TRUE") == "TRUE";

                            foreach (var col in tab.Elements("column"))
                            {
                                var column = new ChatColumn();

                                foreach (var chat in col.Elements("chat"))
                                {
                                    if (chat.Attribute("type")?.Value == "twitch")
                                    {

                                        var channel = chat.Attribute("channel")?.Value;
                                        try
                                        {
                                            var widget = new ChatControl();
                                            widget.ChannelName = channel;
                                            column.AddWidget(widget);
                                        }
                                        catch (Exception e)
                                        {
                                            GuiEngine.Current.log("error loading tab " + e.Message);
                                        }
                                    }
                                }

                                if (column.WidgetCount == 0)
                                {
                                    column.AddWidget(new ChatControl());
                                }

                                page.AddColumn(column);
                            }


                            AddTab(page);
                        }
                    });
                }
            }
            catch (Exception exc)
            {
                GuiEngine.Current.log("error loading layout " + exc.Message);
            }

            if (!TabPages.Any())
            {
                AddTab(new ColumnTabPage(this));
            }
            IsLoading = false;
        }

        public void SaveLayout(string path)
        {
            try
            {
                var doc = new XDocument();
                var root = new XElement("layout");
                doc.Add(root);

                foreach (ColumnTabPage page in TabPages)
                {
                    root.Add(new XElement("tab").With(xtab =>
                    {
                        if (page.CustomTitle != null)
                        {
                            xtab.SetAttributeValue("title", page.Title);
                        }

                        if (!page.EnableNewMessageHighlights)
                        {
                            xtab.SetAttributeValue("enableNewMessageHighlights", false);
                        }

                        if (!page.EnableHighlightedMessageHighlights)
                        {
                            xtab.SetAttributeValue("enableHighlightedMessageHighlights", false);
                        }

                        if (!page.EnableGoLiveHighlights)
                        {
                            xtab.SetAttributeValue("enableGoLiveHighlights", false);
                        }

                        if (!page.EnableGoLiveNotifications)
                        {
                            xtab.SetAttributeValue("enableGoLiveNotifications", false);
                        }

                        foreach (var col in page.Columns)
                        {
                            xtab.Add(new XElement("column").With(xcol =>
                            {
                                foreach (ChatControl widget in col.Widgets.Where(x => x is ChatControl))
                                {
                                    xcol.Add(new XElement("chat").With(x =>
                                    {
                                        x.SetAttributeValue("type", "twitch");
                                        x.SetAttributeValue("channel", widget.ChannelName ?? "");
                                    }));
                                }
                            }));
                        }
                    }));
                }

                doc.Save(path);
            }
            catch { }
        }
    }
}
