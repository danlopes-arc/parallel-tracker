using ParallelTracker.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public static class Text
    {
        public static string GetControllerName(Type t)
        {
            var name = t.Name;
            var index = name.LastIndexOf("Controller");
            if (index < 1) return name;
            return name.Substring(0, index);
        }
    }
}
