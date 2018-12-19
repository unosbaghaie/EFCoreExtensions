using EFCoreExtensions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EFCoreExtensions
{
    public static class ExpressionsToSql
    {

        static string commandTextTemplate = @"{Select} {TableName} {Hint}";
        static string selectTextTemplate = @"Select {Columns} from {TableName} {Hint} Where {Where}";


        public static IQueryable<TResult> Select3<TSource, TResult>(this IQueryable<TSource> query, Expression<Func<TSource, TResult>> selector, Expression<Func<TSource, bool>> predicate) where TSource : class
        {

            var where = new WhereBuilder().ToRawSql(predicate);

            var columns = new WhereBuilder().GetColumns(selector);

            var tableName = query.GetTableName2();

            selectTextTemplate = selectTextTemplate
                .Replace("{Columns}", columns)
                .Replace("{TableName}", tableName)
                .Replace("{Hint}", "with(nolock)")
                .Replace("{Where}", where);

            var dbContext = query.GetDbContext();
            var command = query.GetDbContext().CreateStoreCommand();
            command.CommandText = selectTextTemplate;

            var ownConnection = false;

            try
            {
                if (dbContext.Database.GetDbConnection().State != ConnectionState.Open)
                {
                    ownConnection = true;
                    dbContext.Database.OpenConnection();
                }

                using (var reader = command.ExecuteReader())
                {
                    var objCustomerList = reader.MapToList<User>();
                }
            }
            finally
            {
                if (ownConnection && dbContext.Database.GetDbConnection().State != ConnectionState.Closed)
                {
                    dbContext.Database.CloseConnection();
                }
            }

            return null;
        }


        public static IQueryable<TResult> SelectWithCommand<TSource, TResult>(this IQueryable<TSource> query, Expression<Func<TSource, TResult>> selector, Expression<Func<TSource, bool>> predicate) where TSource : class
        {

            // , Expression<Func<TSource, TResult>> selector, Expression<Func<TSource, bool>> predicate
            var compiled = selector.Compile();
            var memberBindings = ((MemberInitExpression)selector.Body).Bindings;
            var accessors = memberBindings
                .Select(x => x.Member.Name)
                .Select(x => new PropertyOrFieldAccessor(typeof(TSource).GetProperty(x, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)))
                .ToList();

            RelationalQueryContext queryContext;
            var relationalCommand = query.CreateCommand(out queryContext);

            var querySelect = relationalCommand.CommandText;

            var tableName = GetTableName2(query);
            var UseTableLock = true;

            var fromTableName = "";
            if (UseTableLock)
                fromTableName = $@"FROM {tableName} AS [q] with(nolock) ";
            else
                fromTableName = $@"FROM {tableName} AS [q] ";

            querySelect = querySelect.Replace($@"FROM {tableName} AS [q]", fromTableName);

            var dbContext = query.GetDbContext();
            var command = query.GetDbContext().CreateStoreCommand();
            command.CommandText = querySelect;


            var ownConnection = false;

            try
            {
                if (dbContext.Database.GetDbConnection().State != ConnectionState.Open)
                {
                    ownConnection = true;
                    dbContext.Database.OpenConnection();
                }

                using (var reader = command.ExecuteReader())
                {
                    var objCustomerList = reader.MapToList<User>();
                }
            }
            finally
            {
                if (ownConnection && dbContext.Database.GetDbConnection().State != ConnectionState.Closed)
                {
                    dbContext.Database.CloseConnection();
                }
            }

            return null;
        }

        public static IQueryable<TSource> SelectWithCommand2<TSource>(this IQueryable<TSource> query) where TSource : class
        {
            RelationalQueryContext queryContext;
            var relationalCommand = query.CreateCommand(out queryContext);

            var querySelect = relationalCommand.CommandText;

            var tableName = GetTableName2(query);
            var UseTableLock = true;


            var tableAbbreviation = "";
            var u = Regex.Matches(querySelect, @"(FROM) (\[(\w)+\]) (\w+) (\[(\w)+\])").ToList();
            foreach (var item in u)
            {
                var lastIndexOf = item.Value.LastIndexOf('[');
                tableAbbreviation = item.Value.Substring(lastIndexOf);
            }

             querySelect = Regex.Replace(querySelect, @"(FROM) (\[(\w)+\]) (\w+) (\[(\w)+\])", $@"FROM {tableName} AS {tableAbbreviation} with(nolock)");


            var dbContext = query.GetDbContext();
            var command = query.GetDbContext().CreateStoreCommand();
            command.CommandText = querySelect;


            var ownConnection = false;

            try
            {
                if (dbContext.Database.GetDbConnection().State != ConnectionState.Open)
                {
                    ownConnection = true;
                    dbContext.Database.OpenConnection();
                }

                using (var reader = command.ExecuteReader())
                {
                    var objCustomerList = reader.MapToList<User>();
                }
            }
            finally
            {
                if (ownConnection && dbContext.Database.GetDbConnection().State != ConnectionState.Closed)
                {
                    dbContext.Database.CloseConnection();
                }
            }

            return null;
        }



        public static string GetTableName2<TSource>(this IQueryable<TSource> query)
        {

            var context = query.GetDbContext();

            var databaseCreator = context.Database.GetService<IDatabaseCreator>();

            var assembly = databaseCreator.GetType().GetTypeInfo().Assembly;

            var assemblyName = assembly.GetName().Name;

            var type = assembly.GetType("Microsoft.EntityFrameworkCore.SqlServerMetadataExtensions");
            var dynamicProviderEntityType = type.GetMethod("SqlServer", new[] { typeof(IEntityType) });

            var entity = context.Model.FindEntityType(typeof(TSource));


            var sqlServer = (IRelationalEntityTypeAnnotations)dynamicProviderEntityType.Invoke(null, new[] { entity });

            // GET mapping
            return string.IsNullOrEmpty(sqlServer.Schema) ? string.Concat("[", sqlServer.TableName, "]") : string.Concat("[", sqlServer.Schema, "].[", sqlServer.TableName, "]");


        }
        public static string GetTableName<TSource>(this IQueryable<TSource> query)
        {


            var context = query.GetDbContext();

            var databaseCreator = context.Database.GetService<IDatabaseCreator>();

            var assembly = databaseCreator.GetType().GetTypeInfo().Assembly;

            var assemblyName = assembly.GetName().Name;

            var type = assembly.GetType("Microsoft.EntityFrameworkCore.SqlServerMetadataExtensions");
            var dynamicProviderEntityType = type.GetMethod("SqlServer", new[] { typeof(IEntityType) });
            var dynamicProviderProperty = type.GetMethod("SqlServer", new[] { typeof(IProperty) });

            string tableName = "";
            string primaryKeys = "";


            var dbContext = query.GetDbContext();
            var entity = dbContext.Model.FindEntityType(typeof(TSource));


            {
                var sqlServer = (IRelationalEntityTypeAnnotations)dynamicProviderEntityType.Invoke(null, new[] { entity });

                // GET mapping
                tableName = string.IsNullOrEmpty(sqlServer.Schema) ? string.Concat("[", sqlServer.TableName, "]") : string.Concat("[", sqlServer.Schema, "].[", sqlServer.TableName, "]");

                // GET keys mappings
                var columnKeys = new List<string>();
                foreach (var propertyKey in entity.GetKeys().ToList()[0].Properties)
                {
                    var mappingProperty = dynamicProviderProperty.Invoke(null, new[] { propertyKey });

                    var columnNameProperty = mappingProperty.GetType().GetProperty("ColumnName", BindingFlags.Public | BindingFlags.Instance);
                    columnKeys.Add((string)columnNameProperty.GetValue(mappingProperty));
                }

                // GET primary key join
                primaryKeys = string.Join(Environment.NewLine + "AND ", columnKeys.Select(x => string.Concat("A.[", x, "] = B.[", x, "]")));
            }


            return "";

            //// DbContext knows everything about the model.
            //var model = dbContext.GetType();

            //// Get all the entity types information contained in the DbContext class, ...
            //var entityTypes = model.GetEntityTypes();

            //// ... and get one by entity type information of "FooBars" DbSet property.
            //var entityTypeOfFooBar = entityTypes.First(t => t.ClrType == typeof(User));

            //// The entity type information has the actual table name as an annotation!
            //var tableNameAnnotation = entityTypeOfFooBar.GetAnnotation("Relational:TableName");
            //var tableNameOfFooBarSet = tableNameAnnotation.Value.ToString();
            //return tableNameOfFooBarSet;

        }

        public static IRelationalCommand CreateCommand(this IQueryable source, out RelationalQueryContext queryContext)
        {
            bool EFCore_2_1 = false;

            var compilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            var compiler = compilerField.GetValue(source.Provider);

            // REFLECTION: Query.Provider.NodeTypeProvider (Use property for nullable logic)
            var nodeTypeProviderProperty = compiler.GetType().GetProperty("NodeTypeProvider", BindingFlags.NonPublic | BindingFlags.Instance);

            object nodeTypeProvider;
            object QueryModelGenerator = null;

            if (nodeTypeProviderProperty == null)
            {
                EFCore_2_1 = true;

                var QueryModelGeneratorField = compiler.GetType().GetField("_queryModelGenerator", BindingFlags.NonPublic | BindingFlags.Instance);
                QueryModelGenerator = QueryModelGeneratorField.GetValue(compiler);

                var nodeTypeProviderField = QueryModelGenerator.GetType().GetField("_nodeTypeProvider", BindingFlags.NonPublic | BindingFlags.Instance);
                nodeTypeProvider = nodeTypeProviderField.GetValue(QueryModelGenerator);
            }
            else
            {
                nodeTypeProvider = nodeTypeProviderProperty.GetValue(compiler);
            }

            var queryContextFactoryField = compiler.GetType().GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            var queryContextFactory = (IQueryContextFactory)queryContextFactoryField.GetValue(compiler);

            queryContext = (RelationalQueryContext)queryContextFactory.Create();

            var databaseField = compiler.GetType().GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            var database = (IDatabase)databaseField.GetValue(compiler);

            // REFLECTION: Query.Provider._queryCompiler
            var queryCompilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            var queryCompiler = queryCompilerField.GetValue(source.Provider);

            // REFLECTION: Query.Provider._queryCompiler._evaluatableExpressionFilter

            IEvaluatableExpressionFilter evaluatableExpressionFilter = null;

            if (EFCore_2_1)
            {
                evaluatableExpressionFilter = (IEvaluatableExpressionFilter)QueryModelGenerator.GetType().GetField("_evaluatableExpressionFilter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(QueryModelGenerator);
            }
            else
            {
                evaluatableExpressionFilter = (IEvaluatableExpressionFilter)compiler.GetType().GetField("_evaluatableExpressionFilter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(queryCompiler);
            }


            Expression newQuery;
            IQueryCompilationContextFactory queryCompilationContextFactory;

            var dependenciesProperty = typeof(Database).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dependenciesProperty != null)
            {
                var dependencies = dependenciesProperty.GetValue(database);

                var queryCompilationContextFactoryField = typeof(DbContext).GetTypeFromAssembly_Core("Microsoft.EntityFrameworkCore.Storage.DatabaseDependencies")
                                                                           .GetProperty("QueryCompilationContextFactory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                queryCompilationContextFactory = (IQueryCompilationContextFactory)queryCompilationContextFactoryField.GetValue(dependencies);

                var dependenciesProperty2 = typeof(QueryCompilationContextFactory).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
                var dependencies2 = dependenciesProperty2.GetValue(queryCompilationContextFactory);

                // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory.Logger
                var loggerField = typeof(DbContext).GetTypeFromAssembly_Core("Microsoft.EntityFrameworkCore.Query.Internal.QueryCompilationContextDependencies")
                                                    .GetProperty("Logger", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var logger = loggerField.GetValue(dependencies2);

                var parameterExtractingExpressionVisitorConstructors = typeof(ParameterExtractingExpressionVisitor).GetConstructors();

                if (parameterExtractingExpressionVisitorConstructors.Any(x => x.GetParameters().Length == 5))
                {
                    // EF Core 2.1
                    var parameterExtractingExpressionVisitorConstructor = parameterExtractingExpressionVisitorConstructors.First(x => x.GetParameters().Length == 5);
                    var parameterExtractingExpressionVisitor = (ParameterExtractingExpressionVisitor)parameterExtractingExpressionVisitorConstructor.Invoke(new object[] { evaluatableExpressionFilter, queryContext, logger, true, false });

                    // CREATE new query from query visitor
                    newQuery = parameterExtractingExpressionVisitor.ExtractParameters(source.Expression);
                }
                else
                {
                    // EF Core 2.1 Preview 2. 
                    var parameterExtractingExpressionVisitorConstructor = parameterExtractingExpressionVisitorConstructors.First(x => x.GetParameters().Length == 6);
                    var _context = queryContext.GetType().GetProperty("Context", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    var context = _context.GetValue(queryContext);

                    ParameterExtractingExpressionVisitor parameterExtractingExpressionVisitor = null;

                    if (parameterExtractingExpressionVisitorConstructor.GetParameters().Where(x => x.ParameterType == typeof(DbContext)).Any())
                    {
                        parameterExtractingExpressionVisitor = (ParameterExtractingExpressionVisitor)parameterExtractingExpressionVisitorConstructor.Invoke(new object[] { evaluatableExpressionFilter, queryContext, logger, context, true, false });
                    }
                    else
                    {
                        parameterExtractingExpressionVisitor = (ParameterExtractingExpressionVisitor)parameterExtractingExpressionVisitorConstructor.Invoke(new object[] { evaluatableExpressionFilter, queryContext, logger, null, true, false });
                    }

                    // CREATE new query from query visitor
                    newQuery = parameterExtractingExpressionVisitor.ExtractParameters(source.Expression);
                }
            }
            else
            {
                // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory
                var queryCompilationContextFactoryField = typeof(Database).GetField("_queryCompilationContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
                queryCompilationContextFactory = (IQueryCompilationContextFactory)queryCompilationContextFactoryField.GetValue(database);

                // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory.Logger
                var loggerField = queryCompilationContextFactory.GetType().GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance);
                var logger = loggerField.GetValue(queryCompilationContextFactory);

                // CREATE new query from query visitor
                var extractParametersMethods = typeof(ParameterExtractingExpressionVisitor).GetMethod("ExtractParameters", BindingFlags.Public | BindingFlags.Static);
                newQuery = (Expression)extractParametersMethods.Invoke(null, new object[] { source.Expression, queryContext, evaluatableExpressionFilter, logger });
                //ParameterExtractingExpressionVisitor.ExtractParameters(source.Expression, queryContext, evaluatableExpressionFilter, logger);
            }

            //var query = new QueryAnnotatingExpressionVisitor().Visit(source.Expression);
            //var newQuery = ParameterExtractingExpressionVisitor.ExtractParameters(query, queryContext, evalutableExpressionFilter);


            QueryParser queryparser = null;
            if (EFCore_2_1)
            {
                var queryParserMethod = QueryModelGenerator.GetType().GetMethod("CreateQueryParser", BindingFlags.NonPublic | BindingFlags.Instance);
                queryparser = (QueryParser)queryParserMethod.Invoke(QueryModelGenerator, new[] { nodeTypeProvider });
            }
            else
            {
                var queryParserMethod = compiler.GetType().GetMethod("CreateQueryParser", BindingFlags.NonPublic | BindingFlags.Instance);
                queryparser = (QueryParser)queryParserMethod.Invoke(compiler, new[] { nodeTypeProvider });
            }

            var queryModel = queryparser.GetParsedQuery(newQuery);

            var queryModelVisitor = (RelationalQueryModelVisitor)queryCompilationContextFactory.Create(false).CreateQueryModelVisitor();
            var createQueryExecutorMethod = queryModelVisitor.GetType().GetMethod("CreateQueryExecutor");
            var createQueryExecutorMethodGeneric = createQueryExecutorMethod.MakeGenericMethod(source.ElementType);
            createQueryExecutorMethodGeneric.Invoke(queryModelVisitor, new[] { queryModel });

            var queries = queryModelVisitor.Queries;
            var sqlQuery = queries.ToList()[0];


            var command = sqlQuery.CreateDefaultQuerySqlGenerator().GenerateSql(queryContext.ParameterValues);

            return command;
        }

        public static DbContext GetDbContext(this IQueryable query)
        {
            var compilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            var compiler = (QueryCompiler)compilerField.GetValue(query.Provider);

            var queryContextFactoryField = compiler.GetType().GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            var queryContextFactory = (RelationalQueryContextFactory)queryContextFactoryField.GetValue(compiler);


            object stateManagerDynamic;

            var dependenciesProperty = typeof(RelationalQueryContextFactory).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dependenciesProperty != null)
            {
                // EFCore 2.x
                var dependencies = dependenciesProperty.GetValue(queryContextFactory);

                var stateManagerField = typeof(DbContext).GetTypeFromAssembly_Core("Microsoft.EntityFrameworkCore.Query.QueryContextDependencies").GetProperty("StateManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                stateManagerDynamic = stateManagerField.GetValue(dependencies);
            }
            else
            {
                // EFCore 1.x
                var stateManagerField = typeof(QueryContextFactory).GetProperty("StateManager", BindingFlags.NonPublic | BindingFlags.Instance);
                stateManagerDynamic = stateManagerField.GetValue(queryContextFactory);
            }

            IStateManager stateManager = stateManagerDynamic as IStateManager;

            if (stateManager == null)
            {
                Microsoft.EntityFrameworkCore.Internal.LazyRef<IStateManager> lazyStateManager = stateManagerDynamic as Microsoft.EntityFrameworkCore.Internal.LazyRef<IStateManager>;
                if (lazyStateManager != null)
                {
                    stateManager = lazyStateManager.Value;
                }
            }

            if (stateManager == null)
            {
                stateManager = ((dynamic)stateManagerDynamic).Value;
            }


            return stateManager.Context;
        }


    }
}
