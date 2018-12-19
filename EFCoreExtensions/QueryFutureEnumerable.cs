
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;



using System.Linq;
using Microsoft.EntityFrameworkCore.Query.Internal;


namespace EFCoreExtensions
{
    /// <summary>Class for query future value.</summary>
    /// <typeparam name="T">The type of elements of the query.</typeparam>
#if QUERY_INCLUDEOPTIMIZED
    internal class QueryFutureEnumerable<T> : BaseQueryFuture, IEnumerable<T>
#else
    public class QueryFutureEnumerable<T> : BaseQueryFuture, IEnumerable<T>
#endif
    {
        /// <summary>The result of the query future.</summary>
        private IEnumerable<T> _result;

        /// <summary>Constructor.</summary>
        /// <param name="ownerBatch">The batch that owns this item.</param>
        /// <param name="query">
        ///     The query to defer the execution and to add in the batch of future
        ///     queries.
        /// </param>

        public QueryFutureEnumerable(QueryFutureBatch ownerBatch, IQueryable query)

        {
            OwnerBatch = ownerBatch;
            Query = query;
        }

    /// <summary>Gets the enumerator of the query future.</summary>
    /// <returns>The enumerator of the query future.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        if (!HasValue)
        {
            OwnerBatch.ExecuteQueries();
        }

        if (_result == null)
        {
            return new List<T>().GetEnumerator();
        }

        return _result.GetEnumerator();
    }


    /// <summary>Gets the enumerator of the query future.</summary>
    /// <returns>The enumerator of the query future.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    /// <summary>Sets the result of the query deferred.</summary>
    /// <param name="reader">The reader returned from the query execution.</param>
    public override void SetResult(DbDataReader reader)
    {
        //if (reader.GetType().FullName.Contains("Oracle"))
        //{
        //    var reader2 = new QueryFutureOracleDbReader(reader);
        //    reader = reader2;
        //}

        var enumerator = GetQueryEnumerator<T>(reader);

        using (enumerator)
        {
            SetResult(enumerator);
        }
    }

    public void SetResult(IEnumerator<T> enumerator)
    {
        // Enumerate on all items
        var list = new List<T>();
        while (enumerator.MoveNext())
        {
            list.Add(enumerator.Current);
        }
        _result = list;

        HasValue = true;
    }


		public override void ExecuteInMemory()
        {
            HasValue = true;
            _result = ((IQueryable<T>) Query).ToList();
        }

    public override void GetResultDirectly()
    {
        var query = ((IQueryable<T>)Query);

        GetResultDirectly(query);
    }




    internal void GetResultDirectly(IQueryable<T> query)
    {
        using (var enumerator = query.GetEnumerator())
        {
            SetResult(enumerator);
        }
    }
}
}
