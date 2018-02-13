namespace NSQBI_DataMigrator_SalesOrders
{
    public static class MigState
    {
        public const int ShouldSkip = -1;
        public const int Pending = 0;
        public const int CopiedToQB = 1;
        public const int DidSkip = 2;

        public const int BillReadyToPay = 10;
        public const int BillReadyToRefund = 11;
    }
}
