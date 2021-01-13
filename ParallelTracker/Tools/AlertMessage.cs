using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public class AlertMessasge
    {
        public AlertMessasge()
        {

        }

        public AlertMessasge(string type, string text)
        {
            Type = type;
            Text = text;
        }

        public string Type { get; set; }
        public string Text { get; set; }

        public override string ToString() => Text;
    }

    public static class AlertMessageType
    {
        public const string Success = "success";
        public const string Danger = "danger";
        public const string Warning = "warning";
        public const string Info = "info";
    }
}
