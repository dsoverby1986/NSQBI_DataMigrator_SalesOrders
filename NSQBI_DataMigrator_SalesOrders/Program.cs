using System.Data.SQLite;
using System.Configuration;

namespace NSQBI_DataMigrator_SalesOrders
{
    class Program
    {
        static void Main(string[] args)
        {
            SQLiteConnection stage = new SQLiteConnection(ConfigurationManager.ConnectionStrings["Staging"].ConnectionString);
            Factory target = Factory.ForQbModels;
            using (DataMigrator migrator = new DataMigrator(stage, target))
            {
                migrator.MigrateEverything();
            }
        }
    }
}