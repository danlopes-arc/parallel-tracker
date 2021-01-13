using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ParallelTracker.Tools
{
    public static class TempDataExtensions
    {
        public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class
        {
            tempData[key] = JsonSerializer.Serialize(value);
        }

        public static T Get<T>(this ITempDataDictionary tempData, string key) where T : class
        {
            tempData.TryGetValue(key, out object o);
            return o == null ? null : JsonSerializer.Deserialize<T>((string)o);
        }

        public static void AddAlertMessage(this ITempDataDictionary tempData, AlertMessasge message)
        {
            var messages = tempData.Get<IEnumerable<AlertMessasge>>("AlertMessages");
            if (messages == null)
            {
                messages = new List<AlertMessasge>();
            }
            tempData.Put("AlertMessages", messages.Append(message));
        }

        public static IEnumerable<AlertMessasge> GetAlertMessages(this ITempDataDictionary tempData)
        {
            var messages = tempData.Get<IEnumerable<AlertMessasge>>("AlertMessages");
            if (messages == null)
            {
                messages = new List<AlertMessasge>();
            }
            return messages;
        }
    }
}
