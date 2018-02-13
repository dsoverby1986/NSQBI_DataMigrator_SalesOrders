using System;
using System.Collections.Generic;

namespace NSQBI_DataMigrator_SalesOrders
{
    class RateCalc
    {
        readonly LinkedList<long> _history = new LinkedList<long>(new[] { DateTime.Now.Ticks });

        public int MaxHistoryCount { get; set; } = 10000;

        public TimeSpan MaxHistorySpan { get; set; } = TimeSpan.FromMinutes(2.5);

        public void Tick()
        {
            _history.AddLast(DateTime.Now.Ticks);

            while (_history.Count > MaxHistoryCount || HistorySpan > MaxHistorySpan)
                _history.RemoveFirst();
        }

        TimeSpan HistorySpan => TimeSpan.FromTicks(_history.Last.Value - _history.First.Value);

        public Rate CurrentRate => new Rate
        {
            Count = _history.Count - 1,
            Duration = HistorySpan
        };

        public override string ToString() => CurrentRate.ToString();

        public struct Rate
        {
            public double Count { get; set; }
            public TimeSpan Duration { get; set; }

            public Rate Convert(TimeSpan newDuration) =>
                new Rate { Count = Count * newDuration.Ticks / Duration.Ticks, Duration = newDuration };

            static readonly TimeSpan _1s = TimeSpan.FromSeconds(1);

            public override string ToString() =>
                $"{Convert(_1s).Count:N1}/sec";
        }
    }
}
