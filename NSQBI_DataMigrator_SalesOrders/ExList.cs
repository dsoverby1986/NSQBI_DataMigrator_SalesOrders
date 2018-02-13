using System;
using System.Collections;
using System.Collections.Generic;

namespace NSQBI_DataMigrator_SalesOrders
{
    class ExList : IEnumerable<Exception>
    {
        readonly List<Exception> _exs = new List<Exception>();

        public bool Attempt(Action action, Action<Exception> exMod = null, Func<Exception, bool> pred = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex) when (pred?.Invoke(ex) ?? false)
            {
                return true;
            }
            catch (Exception ex)
            {
                exMod?.Invoke(ex);
                Add(ex);
                return false;
            }
        }

        public void ThrowIfAny()
        {
            if (_exs.Count == 0)
                return;

            if (_exs.Count == 1)
                throw _exs[0];

            throw new AggregateException(_exs);
        }

        public void Add(Exception ex) => _exs.Add(ex);

        public IEnumerator<Exception> GetEnumerator() => _exs.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
