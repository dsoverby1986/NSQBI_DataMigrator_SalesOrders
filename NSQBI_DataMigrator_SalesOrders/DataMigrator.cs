using ShellProgressBar;
using System;
using System.Data.SQLite;
using System.Threading;

namespace NSQBI_DataMigrator_SalesOrders
{
    public partial class DataMigrator : IDisposable
    {
        readonly ProgressBarOptions _pbOpts = new ProgressBarOptions { CollapseWhenFinished = false };
        public ProgressBar MainPbar { get; set; }
        public SQLiteConnection Stage { get; }
        public Factory Target { get; }
        public CancellationTokenSource CancelTokenSource { get; } = new CancellationTokenSource();

        public DataMigrator(SQLiteConnection stage, Factory target)
        {
            Stage = stage;
            Target = target;
        }

        public void MigrateEverything()
        {
            var jobs = new[]
            {
                new { name = nameof(MigrateSalesOrders), execute = new Action(MigrateSalesOrders) }
            };

            using (MainPbar = new ProgressBar(jobs.Length, "ALL WORK"))
            {
                foreach (var job in jobs)
                {
                    job.execute();
                    MainPbar.Tick($"Finished: {job.name}");
                }
            }
        }

        private void EnsureNotCancelled() =>
            CancelTokenSource.Token.ThrowIfCancellationRequested();

        public void Dispose() { }
    }
}
