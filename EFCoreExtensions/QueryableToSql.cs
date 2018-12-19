using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EFCoreExtensions
{
    public static class QueryableToSql
    {

        public static string IQueryableToSql<TEntity>(this IQueryable<TEntity> query)
        {

            var exp = query.Expression;

            var me = (MethodCallExpression)exp;


            var columns = new WhereBuilder().GetJoinedColumnNames((Expression)me.Arguments[1]);
            var wheres = new WhereBuilder().ToRawSql2(me);
            var tableName = query.GetTableName2();

            return "";
        }
    }
}
