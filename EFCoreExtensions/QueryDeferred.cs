using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EFCoreExtensions
{
    public class QueryDeferred<TResult>
    {
        /// <summary>Constructor.</summary>
        /// <param name="query">The deferred query.</param>
        /// <param name="expression">The deferred expression.</param>

        public QueryDeferred(IQueryable query, Expression expression)

        {
            Expression = expression;


            Query = new EntityQueryable<TResult>((IAsyncQueryProvider)query.Provider);
            var expressionProperty = typeof(QueryableBase<>).MakeGenericType(typeof(TResult)).GetProperty("Expression", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            expressionProperty.SetValue(Query, expression);

        }

    /// <summary>Gets or sets the deferred expression.</summary>
    /// <value>The deferred expression.</value>
    public Expression Expression { get; protected internal set; }

    /// <summary>Gets or sets the deferred query.</summary>
    /// <value>The deferred query.</value>
    public IQueryable<TResult> Query { get; protected internal set; }

    /// <summary>Execute the deferred expression and return the result.</summary>
    /// <returns>The result of the deferred expression executed.</returns>
    public TResult Execute()
    {
        return Query.Provider.Execute<TResult>(Expression);
    }



        /// <summary>Execute asynchrounously the deferred expression and return the result.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the deferred expression executed asynchrounously.</returns>
        public Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
        {


            var asyncQueryProvider = Query.Provider as IAsyncQueryProvider;

            return asyncQueryProvider != null ?
                asyncQueryProvider.ExecuteAsync<TResult>(Expression, cancellationToken) :
                Task.Run(() => Execute(), cancellationToken);

        }

}
}
