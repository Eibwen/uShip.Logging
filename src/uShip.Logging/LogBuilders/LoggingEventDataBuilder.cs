using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Web;
using log4net.Core;
using log4net.Util;

namespace uShip.Logging
{
    public class LoggingEventDataBuilder
    {
        public virtual LoggingEventData Build(
            StackFrame loggerStackFrame,
            Severity severity,
            string message,
            Exception exception,
            PropertiesDictionary properties)
        {
            var method = loggerStackFrame.GetMethod();
            var reflectedType = method.ReflectedType;
            var reflectedFullName = reflectedType != null ? reflectedType.FullName : string.Empty;
            var reflectedName = reflectedType != null ? reflectedType.Name : string.Empty;

            var userName = Thread.CurrentPrincipal.Identity.Name;

            var eventData = new LoggingEventData
            {
                Domain = AppDomain.CurrentDomain.FriendlyName,
                Identity = userName,
                Level = severity.ToLog4NetLevel(),
                LocationInfo = new LocationInfo(reflectedFullName,
                    method.Name,
                    loggerStackFrame.GetFileName(),
                    loggerStackFrame.GetFileLineNumber().ToString()),
                LoggerName = reflectedName,
                Message = message ?? exception.Message,
                TimeStamp = DateTime.Now,
                UserName = userName,
                Properties = properties
            };

            return eventData;
        }
    }

    internal static class NameValueCollectionExtension
    {
        public static string ToQuery(this NameValueCollection nameValueCollection)
        {
            return string.Join("&",
                Array.ConvertAll(nameValueCollection.AllKeys,
                    key =>
                        string.Format("{0}={1}", HttpUtility.UrlEncode(key),
                            HttpUtility.UrlEncode(nameValueCollection[key]))));
        }
    }

    internal static class PropertiesDictionaryExtensions
    {
        internal static void SafeSetProp(this PropertiesDictionary props, string propKey, Func<string> valueGetter)
        {
            try
            {
                props[propKey] = valueGetter();
            }
            catch (Exception ex)
            {
                props[propKey] = string.Format("Failed setting {0} key in logger because {1}", propKey, ex.Message);
            }
        }
    }
}