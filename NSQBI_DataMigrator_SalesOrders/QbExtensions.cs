using nsoftware.InQB;
using Overby.Extensions.Attachments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NSQBI_DataMigrator_SalesOrders
{
    static class QbExtensions
    {
        static readonly Memoizer Cache = new Memoizer();
        public static T OpenConn<T>(this T comp) where T : Component
        {
            ((dynamic)comp).OpenQBConnection();
            return comp;
        }

        public static T SetOffline<T>(this T comp) where T : Component
        {
            var compType = comp.GetType();
            const string PropName = nameof(Customer.QBRequestMode);
            var prop = Cache.Get(compType, () => compType.GetProperty(PropName));
            var value = Cache.Get(compType, () =>
                Enum.GetValues(prop.PropertyType).Cast<object>().Single(n => n.ToString().ContainsIgnoreCase("offline")));
            prop.SetValue(comp, value);
            return comp;
        }

        public static string SetStopOnError(this Component c, bool value)
        {
            dynamic d = c;
            return d.Config($"StopOnError={value}");
        }

        public static void QueueWithCallback(this Qbobject qobj, string aggregate, Action<QBObjectResult> action)
        {
            qobj.Queue(aggregate);
            qobj.RegCallback(action);
        }

        public static void RegCallback(this Qbobject qobj, Action<QBObjectResult> action)
        {
            qobj.GetOrSetAttached(() => new List<Action<QBObjectResult>>()).Value.Add(action);
        }

        public static void ProcessQueueAndCallback(this Qbobject qobj)
        {
            qobj.ProcessQueue();
            qobj.Callback();
        }

        public static void Callback(this Qbobject qobj)
        {
            // make a copy of the list so callbacks can queue more operations
            var cbList = qobj.GetOrSetAttached(() => new List<Action<QBObjectResult>>()).Value;
            var cbsCopy = cbList.ToArray();
            cbList.Clear();

            var zip = qobj.Results.Zip(cbsCopy, (r, cb) => new { r, cb });

            foreach (var x in zip)
                x.cb(x.r);
        }
    }
}
