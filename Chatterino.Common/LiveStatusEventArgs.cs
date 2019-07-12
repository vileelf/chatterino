using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public class LiveStatusEventArgs : EventArgs
    {
        public bool IsLive;
        public LiveStatusEventArgs(bool IsLive)
        {
            this.IsLive = IsLive;
        }
    }
}
