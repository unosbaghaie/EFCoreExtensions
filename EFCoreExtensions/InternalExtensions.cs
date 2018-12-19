using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace EFCoreExtensions
{
    internal static partial class InternalExtensions
    {
        /// <summary>A DbContext extension method that creates a new store command.</summary>
        /// <param name="context">The context to act on.</param>
        /// <returns>The new store command from the DbContext.</returns>
        public static DbCommand CreateStoreCommand(this DbContext context)
        {
            var entityConnection = context.Database.GetDbConnection();
            var command = entityConnection.CreateCommand();
            var entityTransaction = context.Database.GetService<IRelationalConnection>().CurrentTransaction;
            if (entityTransaction != null)
            {
                command.Transaction = entityTransaction.GetDbTransaction();
            }

            var commandTimeout = context.Database.GetCommandTimeout();
            if (commandTimeout.HasValue)
            {
                command.CommandTimeout = commandTimeout.Value;
            }

            return command;
        }


        internal static Type GetTypeFromAssembly_Core(this Type fromType, string name)
        {
#if NETSTANDARD1_3
            return fromType.GetTypeInfo().Assembly.GetType(name);
#else
            return fromType.Assembly.GetType(name);
#endif
        }







        public static void CopyFrom(this DbParameter @this, IRelationalParameter from, object value)
        {
            @this.ParameterName = from.InvariantName;

            if (from is TypeMappedRelationalParameter)
            {
                var relationalTypeMappingProperty = typeof(TypeMappedRelationalParameter).GetProperty("RelationalTypeMapping", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (relationalTypeMappingProperty != null)
                {
                    var relationalTypeMapping = (RelationalTypeMapping)relationalTypeMappingProperty.GetValue(from);

                    if (relationalTypeMapping.DbType.HasValue)
                    {
                        @this.DbType = relationalTypeMapping.DbType.Value;
                    }
                }
            }

            @this.Value = value ?? DBNull.Value;
        }

        public static void CopyFrom(this DbParameter @this, IRelationalParameter from, object value, string newParameterName)
        {
            @this.ParameterName = newParameterName;

            if (from is TypeMappedRelationalParameter)
            {
                var relationalTypeMappingProperty = typeof(TypeMappedRelationalParameter).GetProperty("RelationalTypeMapping", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (relationalTypeMappingProperty != null)
                {
                    var relationalTypeMapping = (RelationalTypeMapping)relationalTypeMappingProperty.GetValue(from);

                    if (relationalTypeMapping.DbType.HasValue)
                    {
                        @this.DbType = relationalTypeMapping.DbType.Value;
                    }
                }
            }

            @this.Value = value ?? DBNull.Value;




        }
    }
}
