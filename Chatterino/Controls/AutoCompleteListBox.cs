using System.Windows.Forms;

namespace Chatterino.Controls {
    public class AutoCompleteListBox : ListBox {
        private bool _showScroll;
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                if (!_showScroll)
                    cp.Style = cp.Style & ~0x200000;
                return cp;
            }
        }
        public bool ShowScrollbar {
            get { return _showScroll; }
            set {
                if (value != _showScroll) {
                    _showScroll = value;
                    if (IsHandleCreated)
                        RecreateHandle();
                }
            }
        }
    }
}