using System;
using System.Configuration;

namespace NSQBI_DataMigrator_SalesOrders
{
    public class Factory
    {
        private readonly Action<dynamic> _mutate;

        public Factory(Action<dynamic> mutate)
        {
            _mutate = mutate;
        }

        public T Create<T>() where T : new() => Create(typeof(T));

        public dynamic Create(Type type)
        {
            var x = Activator.CreateInstance(type);
            _mutate?.Invoke(x);
            return x;
        }

        public static readonly Factory ForQbModels =
            new Factory(x =>
            {
                x.QBXMLVersion = "13.0";
                x.QBConnectionString = ConfigurationManager.ConnectionStrings["Target"].ConnectionString;
            });
    }
}
