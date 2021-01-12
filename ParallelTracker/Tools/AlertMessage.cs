using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public class AlertMessage
    {
        public AlertMessage(AlertMessageType type, string text)
        {
            Type = type;
            Text = text;
        }

        public AlertMessageType Type { get; private set;  }
        public string Text { get; private set; }
    }

    public enum AlertMessageType
    {
        None,
        Success,
        Error,
        Info,
        Warnig
    }
}
