using CoreTechs.Common;
using Overby.Extensions.Attachments;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace NSQBI_DataMigrator_SalesOrders
{
    static class Extensions
    {
        public static bool ContainsIgnoreCase(this string s, string other) =>
            s.Contains(other, StringComparison.CurrentCultureIgnoreCase);

        public static T StartRateCalc<T>(this T pb) where T : ProgressBarBase
        {
            pb.SetAttached(new RateCalc());
            return pb;
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source) =>
            Batch(source, TimeSpan.FromSeconds(30));

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, TimeSpan targetDuration, Func<int, int> modifySize = null)
        {
            modifySize = modifySize ?? (_ => _);
            var history = new LinkedList<double>();
            using (var it = source.GetEnumerator())
            {
                var size = 1;
                var sw = new System.Diagnostics.Stopwatch();
                while (it.MoveNext())
                {
                    sw.Restart();
                    yield return it.AsEnumerable(true).Take(size);
                    history.AddLast(sw.Elapsed.TotalSeconds);

                    if (history.Count > 3)
                        history.RemoveFirst();

                    var rate = history.Average() / size;
                    size = modifySize((int)Math.Max(1, targetDuration.TotalSeconds / rate));
                }
            }
        }

        public static SQLiteParameter SqliteParam(this object value, string name) =>
            new SQLiteParameter(name, value);

        public static void TickRate(this ProgressBarBase pb, string message = null)
        {
            var rc = pb.GetOrSetAttached(() => new RateCalc()).Value;
            rc.Tick();
            pb.Tick($"{rc} - {message}");
        }
    }
}
