﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EFCoreExtensions
{
    /// <summary>Interace for QueryFuture class.</summary>
#if QUERY_INCLUDEOPTIMIZED
    internal abstract class BaseQueryFuture
#else
    public abstract class BaseQueryFuture
#endif
    {
        /// <summary>Gets the value indicating whether the query future has a value.</summary>
        /// <value>true if this query future has a value, false if not.</value>
        public bool HasValue { get; protected set; }

        /// <summary>Gets or sets the batch that owns the query future.</summary>
        /// <value>The batch that owns the query future.</value>
        public QueryFutureBatch OwnerBatch { get; set; }


    /// <summary>Gets or sets the query deferred.</summary>
    /// <value>The query deferred.</value>
        public IQueryable Query { get; set; }

        /// <summary>Gets or sets the query deferred executor.</summary>
        /// <value>The query deferred executor.</value>
        public object QueryExecutor { get; set; }

        /// <summary>Gets or sets a context for the query deferred.</summary>
        /// <value>The query deferred context.</value>
        public QueryContext QueryContext { get; set; }

        /// <summary>Gets or sets the query connection.</summary>
        /// <value>The query connection.</value>
        internal IRelationalConnection QueryConnection { get; set; }

        internal Action RestoreConnection { get;set;}

        public virtual void ExecuteInMemory()
        {
            
        }

        /// <summary>Creates executor and get command.</summary>
        /// <returns>The new executor and get command.</returns>
        public virtual IRelationalCommand CreateExecutorAndGetCommand(out RelationalQueryContext queryContext)
        {
            bool isEFCore2x = false;
            bool EFCore_2_1 = false;

            var context = Query.GetDbContext();

            // REFLECTION: Query._context.StateManager
#if NETSTANDARD2_0
            var stateManager = context.ChangeTracker.GetStateManager();
#else
            var stateManagerProperty = typeof(DbContext).GetProperty("StateManager", BindingFlags.NonPublic | BindingFlags.Instance);
            var stateManager = (StateManager)stateManagerProperty.GetValue(context);
#endif

            // REFLECTION: Query._context.StateManager._concurrencyDetector
            var concurrencyDetectorField = typeof(StateManager).GetField("_concurrencyDetector", BindingFlags.NonPublic | BindingFlags.Instance);
            var concurrencyDetector = (IConcurrencyDetector)concurrencyDetectorField.GetValue(stateManager);

            // REFLECTION: Query.Provider._queryCompiler
            var queryCompilerField = typeof (EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            var queryCompiler = queryCompilerField.GetValue(Query.Provider);

            // REFLECTION: Query.Provider.NodeTypeProvider (Use property for nullable logic)
            var nodeTypeProviderProperty = queryCompiler.GetType().GetProperty("NodeTypeProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            object nodeTypeProvider;
            object QueryModelGenerator = null;

            if (nodeTypeProviderProperty == null)
            {
                EFCore_2_1 = true;

                var QueryModelGeneratorField = queryCompiler.GetType().GetField("_queryModelGenerator", BindingFlags.NonPublic | BindingFlags.Instance);
                QueryModelGenerator = QueryModelGeneratorField.GetValue(queryCompiler);

                var nodeTypeProviderField = QueryModelGenerator.GetType().GetField("_nodeTypeProvider", BindingFlags.NonPublic | BindingFlags.Instance);
                nodeTypeProvider = nodeTypeProviderField.GetValue(QueryModelGenerator);
            }
            else
            {
                nodeTypeProvider = nodeTypeProviderProperty.GetValue(queryCompiler);
            }

            // REFLECTION: Query.Provider._queryCompiler.CreateQueryParser();
#if NETSTANDARD2_0
            QueryParser createQueryParser = null;
            if (EFCore_2_1)
            {
                var queryParserMethod = QueryModelGenerator.GetType().GetMethod("CreateQueryParser", BindingFlags.NonPublic | BindingFlags.Instance);
                createQueryParser = (QueryParser)queryParserMethod.Invoke(QueryModelGenerator, new[] { nodeTypeProvider });
            }
            else
            {
                var queryParserMethod = queryCompiler.GetType().GetMethod("CreateQueryParser", BindingFlags.NonPublic | BindingFlags.Instance);
                createQueryParser = (QueryParser)queryParserMethod.Invoke(queryCompiler, new[] { nodeTypeProvider });
            }
#else
            var createQueryParserMethod = queryCompiler.GetType().GetMethod("CreateQueryParser", BindingFlags.NonPublic | BindingFlags.Static);
            var createQueryParser = (QueryParser)createQueryParserMethod.Invoke(null, new[] { nodeTypeProvider });
#endif

            // REFLECTION: Query.Provider._queryCompiler._database
            var databaseField = queryCompiler.GetType().GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            var database = (IDatabase) databaseField.GetValue(queryCompiler);

            // REFLECTION: Query.Provider._queryCompiler._evaluatableExpressionFilter
#if NETSTANDARD2_0
            IEvaluatableExpressionFilter evaluatableExpressionFilter = null;

            if (EFCore_2_1)
            {
                evaluatableExpressionFilter = (IEvaluatableExpressionFilter)QueryModelGenerator.GetType().GetField("_evaluatableExpressionFilter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(QueryModelGenerator);
            }
            else
            {
                evaluatableExpressionFilter = (IEvaluatableExpressionFilter)queryCompiler.GetType().GetField("_evaluatableExpressionFilter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(queryCompiler);
            }
#else
            var evaluatableExpressionFilterField = queryCompiler.GetType().GetField("_evaluatableExpressionFilter", BindingFlags.NonPublic | BindingFlags.Static);
            var evaluatableExpressionFilter = (IEvaluatableExpressionFilter) evaluatableExpressionFilterField.GetValue(null);
#endif

            // REFLECTION: Query.Provider._queryCompiler._queryContextFactory
            var queryContextFactoryField = queryCompiler.GetType().GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            var queryContextFactory = (IQueryContextFactory) queryContextFactoryField.GetValue(queryCompiler);

            // REFLECTION: Query.Provider._queryCompiler._queryContextFactory.CreateQueryBuffer
            var createQueryBufferDelegateMethod = (typeof (QueryContextFactory)).GetMethod("CreateQueryBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
            var createQueryBufferDelegate = (Func<IQueryBuffer>) createQueryBufferDelegateMethod.CreateDelegate(typeof (Func<IQueryBuffer>), queryContextFactory);

            // REFLECTION: Query.Provider._queryCompiler._queryContextFactory._connection
            var connectionField = queryContextFactory.GetType().GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
            var connection = (IRelationalConnection) connectionField.GetValue(queryContextFactory);

            IQueryCompilationContextFactory queryCompilationContextFactory;
            object logger;

            var dependenciesProperty = typeof(Database).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
            if(dependenciesProperty != null)
            {
                // EFCore 2.x
                
                isEFCore2x = true;
                var dependencies = dependenciesProperty.GetValue(database);

                var queryCompilationContextFactoryField = typeof(DbContext).GetTypeFromAssembly_Core("Microsoft.EntityFrameworkCore.Storage.DatabaseDependencies")
                                                                           .GetProperty("QueryCompilationContextFactory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                queryCompilationContextFactory = (IQueryCompilationContextFactory)queryCompilationContextFactoryField.GetValue(dependencies);

                var dependenciesProperty2 = typeof(QueryCompilationContextFactory).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
                var dependencies2 = dependenciesProperty2.GetValue(queryCompilationContextFactory);

                // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory.Logger
                var loggerField =  typeof(DbContext).GetTypeFromAssembly_Core("Microsoft.EntityFrameworkCore.Query.Internal.QueryCompilationContextDependencies")
                                                    .GetProperty("Logger", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // (IInterceptingLogger<LoggerCategory.Query>)
                logger = loggerField.GetValue(dependencies2);
            }
            else
            {
                // EFCore 1.x
                
                // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory
                var queryCompilationContextFactoryField = typeof (Database).GetField("_queryCompilationContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
                queryCompilationContextFactory = (IQueryCompilationContextFactory) queryCompilationContextFactoryField.GetValue(database);

                // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory.Logger
                var loggerField = queryCompilationContextFactory.GetType().GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance);
                logger = loggerField.GetValue(queryCompilationContextFactory);
            }


            // CREATE connection
            {
                QueryConnection = context.Database.GetService<IRelationalConnection>();
               
                var innerConnection = new CreateEntityConnection(QueryConnection.DbConnection, null);
                var innerConnectionField = typeof(RelationalConnection).GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance);
                var initalConnection = innerConnectionField.GetValue(QueryConnection);

                innerConnectionField.SetValue(QueryConnection, new Microsoft.EntityFrameworkCore.Internal.LazyRef<DbConnection>(() => innerConnection));

                RestoreConnection = () => innerConnectionField.SetValue(QueryConnection, initalConnection);
            }


            // CREATE query context
            {
                var relationalQueryContextType = typeof(RelationalQueryContext);
                var relationalQueryContextConstructor = relationalQueryContextType.GetConstructors()[0];

                // EF Core 1.1 preview
                if (relationalQueryContextConstructor.GetParameters().Length == 5)
                {
                    // REFLECTION: Query.Provider._queryCompiler._queryContextFactory.ExecutionStrategyFactory
                    var executionStrategyFactoryField = queryContextFactory.GetType().GetProperty("ExecutionStrategyFactory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    var executionStrategyFactory = executionStrategyFactoryField.GetValue(queryContextFactory);

                    var lazyRefStateManager = new LazyRef<IStateManager>(() => stateManager);

                    queryContext = (RelationalQueryContext)relationalQueryContextConstructor.Invoke(new object[] { createQueryBufferDelegate, QueryConnection, lazyRefStateManager, concurrencyDetector, executionStrategyFactory });
                }
                else if(isEFCore2x)
                {
                    // REFLECTION: Query.Provider._queryCompiler._queryContextFactory.ExecutionStrategyFactory
                    var executionStrategyFactoryField = queryContextFactory.GetType().GetProperty("ExecutionStrategyFactory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    var executionStrategyFactory = executionStrategyFactoryField.GetValue(queryContextFactory);

                    var lazyRefStateManager = new LazyRef<IStateManager>(() => stateManager);

                    var dependenciesProperty3 = typeof(RelationalQueryContextFactory).GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
                    var dependencies3 = dependenciesProperty3.GetValue(queryContextFactory);

                    queryContext = (RelationalQueryContext)relationalQueryContextConstructor.Invoke(new object[] { dependencies3, createQueryBufferDelegate, QueryConnection, executionStrategyFactory });
                }
                else
                {
                    queryContext = (RelationalQueryContext) relationalQueryContextConstructor.Invoke(new object[] {createQueryBufferDelegate, QueryConnection, stateManager, concurrencyDetector});
                }
            }


            Expression newQuery;


            if(isEFCore2x)
            {
                var parameterExtractingExpressionVisitorConstructors = typeof(ParameterExtractingExpressionVisitor).GetConstructors();

                if (parameterExtractingExpressionVisitorConstructors.Any(x => x.GetParameters().Length == 5))
                {
                    // EF Core 2.1
                    var parameterExtractingExpressionVisitorConstructor = parameterExtractingExpressionVisitorConstructors.First(x => x.GetParameters().Length == 5);
                    var parameterExtractingExpressionVisitor = (ParameterExtractingExpressionVisitor)parameterExtractingExpressionVisitorConstructor.Invoke(new object[] { evaluatableExpressionFilter, queryContext, logger, true, false });

                    // CREATE new query from query visitor
                    newQuery = parameterExtractingExpressionVisitor.ExtractParameters(Query.Expression);
                }
                else
                {

	                var parameterExtractingExpressionVisitorConstructor = parameterExtractingExpressionVisitorConstructors.First(x => x.GetParameters().Length == 6);

					ParameterExtractingExpressionVisitor parameterExtractingExpressionVisitor = null; 

	                // EF Core 2.1   
					if (parameterExtractingExpressionVisitorConstructor.GetParameters().Where(x => x.ParameterType == typeof(DbContext)).Any())
					{
						parameterExtractingExpressionVisitor = (ParameterExtractingExpressionVisitor)parameterExtractingExpressionVisitorConstructor.Invoke(new object[] { evaluatableExpressionFilter, queryContext, logger, context, true, false });
					}
					else
					{
						parameterExtractingExpressionVisitor = (ParameterExtractingExpressionVisitor)parameterExtractingExpressionVisitorConstructor.Invoke(new object[] { evaluatableExpressionFilter, queryContext, logger, null, true, false });
					} 

                    // CREATE new query from query visitor
                    newQuery = parameterExtractingExpressionVisitor.ExtractParameters(Query.Expression);
                }
            }
            else
            {
                // CREATE new query from query visitor
                var extractParametersMethods = typeof(ParameterExtractingExpressionVisitor).GetMethod("ExtractParameters", BindingFlags.Public | BindingFlags.Static);
                newQuery = (Expression) extractParametersMethods.Invoke(null, new object[] {Query.Expression, queryContext, evaluatableExpressionFilter, logger});
            }

            // PARSE new query
            var queryModel = createQueryParser.GetParsedQuery(newQuery);

            // CREATE query model visitor
            var queryModelVisitor = (RelationalQueryModelVisitor) queryCompilationContextFactory.Create(false).CreateQueryModelVisitor();

            // REFLECTION: Query.Provider._queryCompiler._database._queryCompilationContextFactory.Create(false).CreateQueryModelVisitor().CreateQueryExecutor()
            var createQueryExecutorMethod = queryModelVisitor.GetType().GetMethod("CreateQueryExecutor");
            var createQueryExecutorMethodGeneric = createQueryExecutorMethod.MakeGenericMethod(Query.ElementType);
            var queryExecutor = createQueryExecutorMethodGeneric.Invoke(queryModelVisitor, new[] {queryModel});

            // SET value
            QueryExecutor = queryExecutor;
            QueryContext = queryContext;

            // RETURN the IRealationCommand
            var sqlQuery = queryModelVisitor.Queries.First();
            var relationalCommand = sqlQuery.CreateDefaultQuerySqlGenerator().GenerateSql(queryContext.ParameterValues);
            return relationalCommand;
        }


        /// <summary>Sets the result of the query deferred.</summary>
        /// <param name="reader">The reader returned from the query execution.</param>
        public virtual void SetResult(DbDataReader reader)
        {
        }

        /// <summary>Sets the result of the query deferred.</summary>
        /// <param name="reader">The reader returned from the query execution.</param>
        public IEnumerator<T> GetQueryEnumerator<T>(DbDataReader reader)
        {

           
            ((CreateEntityConnection)QueryConnection.DbConnection).OriginalDataReader = reader;
            var queryExecutor = (Func<QueryContext, IEnumerable<T>>) QueryExecutor;
            var queryEnumerable = queryExecutor(QueryContext);
            return queryEnumerable.GetEnumerator();

        }

        public virtual void GetResultDirectly()
        {

        }
        public virtual Task GetResultDirectlyAsync(CancellationToken cancellationToken)
        {
            throw new Exception("Not implemented");
        }

    }
}