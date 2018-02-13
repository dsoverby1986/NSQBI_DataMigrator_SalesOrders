using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace NSQBI_DataMigrator_SalesOrders
{
    public class Memoizer
    {
        /// <remarks>
        /// Direct access provided to the memoization cache, so that values can be manually added/removed.
        /// </remarks>>
        public readonly ConcurrentDictionary<object, object> Cache = new ConcurrentDictionary<object, object>();

        /// <summary>
        /// Returns data produced by the factory. Future calls with matching keyData and namespace
        /// will return the "memoized" value.
        /// </summary>
        /// <typeparam name="T">The type of data to be returned.</typeparam>
        /// <param name="factory">The function that produces the data.</param>
        /// <param name="keyData">Data that is used as a cache key. This needs to have a good implementation of GetHashCode. Best to use anonymous objects with primitive properties.</param>
        /// <param name="namespace">
        /// A namespace for the cache key. 
        /// This is automatically set to the caller's member name when no value is passed in.</param>
        public T Get<T>(object keyData, Func<T> factory, [CallerMemberName] string @namespace = null)
        {
            var cacheKey = new { @namespace, keyData, type = typeof(T) };
            return (T)Cache.GetOrAdd(cacheKey, _ => factory());
        }

        public static readonly Memoizer Instance = new Memoizer();
    }
}
