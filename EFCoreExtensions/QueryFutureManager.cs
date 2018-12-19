using System.Runtime.CompilerServices;


using Microsoft.EntityFrameworkCore;
namespace EFCoreExtensions
{
    /// <summary>Manage EF+ Query Future Configuration.</summary>
#if QUERY_INCLUDEOPTIMIZED
    internal static class QueryFutureManager
#else
    public static class QueryFutureManager
#endif
    {
        /// <summary>Static constructor.</summary>
        static QueryFutureManager()
        {

            CacheWeakFutureBatch = new System.Runtime.CompilerServices.ConditionalWeakTable<DbContext, QueryFutureBatch>();

        }

        /// <summary>Gets or sets a value indicating whether we allow query batch.</summary>
        /// <value>True if allow query batch, false if not.</value>
        public static bool AllowQueryBatch { get; set; } = true;

        /// <summary>Gets or sets the weak table used to cache future batch associated to a context.</summary>
        /// <value>The weak table used to cache future batch associated to a context.</value>


        public static System.Runtime.CompilerServices.ConditionalWeakTable<DbContext, QueryFutureBatch> CacheWeakFutureBatch { get; set; }


        /// <summary>Adds or gets the future batch associated to the context.</summary>
        /// <param name="context">The context used to cache the future batch.</param>
        /// <returns>The future batch associated to the context.</returns>

        public static QueryFutureBatch AddOrGetBatch(DbContext context)

        {
            QueryFutureBatch futureBatch;

            if (!CacheWeakFutureBatch.TryGetValue(context, out futureBatch))
            {
                futureBatch = new QueryFutureBatch(context);
                CacheWeakFutureBatch.Add(context, futureBatch);
            }

            return futureBatch;
        }

        public static void ExecuteBatch(DbContext context)
        {

            var batch = AddOrGetBatch(context);

            batch.ExecuteQueries();
        }
    }
}