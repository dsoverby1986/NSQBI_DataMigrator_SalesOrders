using CoreTechs.Common.Database;
using nsoftware.InQB;
using System;
using System.Linq;
using System.Xml.Linq;
using static NSQBI_DataMigrator_SalesOrders.MigState;

namespace NSQBI_DataMigrator_SalesOrders
{
    public partial class DataMigrator
    {
        public void MigrateSalesOrders()
        {
            var jobs = (from d in Stage.QuerySql(
                    $"SELECT JOB_NO, XMIGDATA FROM JOBS WHERE XMIGSTAT = {CopiedToQB}").AsDynamic()
                        let xdoc = (XDocument)XDocument.Parse(d.XMIGDATA)
                        let jobNo = d.JOB_NO.ToString()
                        let qbid = (string)xdoc.Root.Element("ListID")
                        select new { jobNo, qbid }).ToDictionary(x => x.jobNo, x => x.qbid);

            var inventory = (from d in Stage.QuerySql(
                    $"SELECT ITEM_NO, XMIGDATA FROM INVENTORY WHERE XMIGSTAT = {CopiedToQB}").AsDynamic()
                             let xdoc = (XDocument)XDocument.Parse(d.XMIGDATA)
                             let itemNo = (string)d.ITEM_NO
                             let qbid = (string)xdoc.Root.Element("ListID")
                             select new { itemNo, qbid }).ToDictionary(x => x.itemNo, x => x.qbid);

            var exs = new ExList();

            var salesOrdersByJobAndDate = Stage.QuerySql($"SELECT rowid, * FROM SALES_ORDERS WHERE XMIGSTAT = {Pending}")
                .AsDynamic().GroupBy(x => new { x.JOB_NO, x.DATE }).ToArray();

            using (Stage.Connect())
            using (var qbo = Target.Create<Qbobject>().OpenConn())
            using (var so = Target.Create<Salesorder>().SetOffline())
            using (var pbar = MainPbar.Spawn(salesOrdersByJobAndDate.Length, "Copying Sales Orders", _pbOpts).StartRateCalc())
            {
                qbo.SetStopOnError(false);

                foreach (var batch in salesOrdersByJobAndDate.Batch())
                {
                    foreach (var salesOrder in batch)
                    {
                        EnsureNotCancelled();
                        so.Reset();
                        so.RefNumber = $"SO{salesOrder.Key.JOB_NO}";
                        so.CustomerId = jobs.FirstOrDefault(x => x.Key.Equals(salesOrder.Key.JOB_NO.ToString(), StringComparison.OrdinalIgnoreCase)).Value;

                        foreach (var salesOrderItem in salesOrder)
                        {
                            var itemId = inventory.FirstOrDefault(x => x.Key.Equals(salesOrderItem.ITEM_NO)).Value;
                            if (string.IsNullOrWhiteSpace(itemId))
                                continue;
                            so.LineItems.Add(new SalesOrderItem()
                            {
                                ItemId = itemId,
                                Description = salesOrderItem.DESC,
                                Quantity = salesOrderItem.QTY.ToString(),
                                Rate = salesOrderItem.PRICE.ToString()
                            });
                        }

                        so.TransactionDate = salesOrder.Key.DATE.ToString("yyyy-MM-dd");

                        //so.ManuallyClosed = ManuallyCloseds.mcManuallyClosed;
                        so.Config("IsManuallyClosed=True");

                        so.Add();

                        qbo.QueueWithCallback(so.QBRequestAggregate, r =>
                        {
                            if (exs.Attempt(() => so.ImportQBXML(r.Aggregate)))
                            {
                                Stage.ExecuteSql($"UPDATE SALES_ORDERS SET XMIGSTAT = {CopiedToQB}, XMIGDATA = @md WHERE JOB_NO = {salesOrder.Key.JOB_NO} AND [DATE]='{salesOrder.Key.DATE.ToString("yyyy-MM-dd HH:mm:ss")}'", r.Aggregate.SqliteParam("md"));
                            }
                        });


                        pbar.TickRate($"Copied Sales Order: SO{salesOrder.Key.JOB_NO}");
                    }

                    using (var tr = Stage.BeginTransaction())
                    {
                        qbo.ProcessQueueAndCallback();
                        tr.Commit();
                    }

                    exs.ThrowIfAny();
                }
            }
        }
    }
}
