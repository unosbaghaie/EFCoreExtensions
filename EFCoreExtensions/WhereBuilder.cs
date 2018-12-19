using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreExtensions
{
    public class WhereBuilder
    {
        // private readonly IProvider _provider;
        // private TableDefinition _tableDef;

        public WhereBuilder() //IProvider provider
        {
            // _provider = provider;
        }

        public WherePart ToSql(MethodCallExpression methodCallExpression)
        {

            var m = methodCallExpression.Arguments;
            //_tableDef = _provider.GetTableDefinitionFor<T>();
            return null;
        }

        public WherePart ToSql<T>(Expression<Func<T, bool>> expression)
        {
            //_tableDef = _provider.GetTableDefinitionFor<T>();
            var i = 1;
            return Recurse(ref i, expression.Body, isUnary: true);
        }

        public string ToRawSql<T>(Expression<Func<T, bool>> expression)
        {
            //_tableDef = _provider.GetTableDefinitionFor<T>();
            var i = 1;
            var whereParts = Recurse(ref i, expression.Body, isUnary: true);

            if (!whereParts.Parameters.Any())
                return whereParts.Sql;

            StringBuilder finalQuery = new StringBuilder();
            finalQuery.Append(whereParts.Sql);
            foreach (var p in whereParts.Parameters)
            {
                var val = "@" + p.Key;
                finalQuery = finalQuery.Replace(val, p.Value.ToString());
            }
            return finalQuery.ToString();
        }

        public string ToRawSql2(MethodCallExpression expression)
        {
            var i = 1;

            var arg1 = ((MethodCallExpression)expression).Arguments[0];
            var arg2 = ((MethodCallExpression)arg1).Arguments[1];

            var whereParts = Recurse(ref i, (((LambdaExpression)((UnaryExpression)arg2).Operand)).Body, isUnary: true);

            if (!whereParts.Parameters.Any())
                return whereParts.Sql;

            StringBuilder finalQuery = new StringBuilder();
            finalQuery.Append(whereParts.Sql);
            foreach (var p in whereParts.Parameters)
            {
                var val = "@" + p.Key;
                finalQuery = finalQuery.Replace(val, p.Value.ToString());
            }
            return finalQuery.ToString();
        }

        public string GetColumns<TSource, TResult>(Expression<Func<TSource, TResult>> expression)
        {
            var visitor = new ExprVisitor();
            visitor.Visit(expression.Body);
            if (visitor.IsFound)
            {
                var members = visitor.EntityProperties.Select(q => q.MemberName).ToList();
                return String.Join(",", members);
            }
            else
            {
                Console.WriteLine("No properties found.");
            }
            return null;

        }

        public string GetJoinedColumnNames(Expression expression)
        {
            var visitor = new ExprVisitor();
            visitor.Visit(expression);
            if (visitor.IsFound)
            {
                var members = visitor.EntityProperties.Select(q => q.MemberName).ToList();
                return String.Join(",", members);
            }
            else
            {
                Console.WriteLine("No properties found.");
            }
            return null;

        }

       
        private object GetColumns(Expression body)
        {
            return "";
        }

        public class EntityPropert
        {
            public string MemberName { get; set; }
            public Type MemberType { get; set; }
        }

        class ExprVisitor : ExpressionVisitor
        {
            public List<EntityPropert> EntityProperties = new List<EntityPropert>();
            public bool IsFound { get; private set; }
            public string MemberName { get; private set; }
            public Type MemberType { get; private set; }
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member.MemberType == MemberTypes.Property)
                {
                    IsFound = true;
                    MemberName = node.Member.Name;
                    MemberType = node.Type;
                    EntityProperties.Add(new EntityPropert() { MemberName = node.Member.Name, MemberType = node.Type });
                }
                return base.VisitMember(node);
            }
        }

        class WhereVisitor : ExpressionVisitor
        {


            

            protected override Expression VisitMember
                (MemberExpression member)
            {
                if (member.Expression is ConstantExpression &&
                    member.Member is FieldInfo)
                {
                    object container =
                        ((ConstantExpression)member.Expression).Value;
                    object value = ((FieldInfo)member.Member).GetValue(container);
                    Console.WriteLine("Got value: {0}", value);
                }
                return base.VisitMember(member);
            }
        }

        public class AndAlsoModifier : ExpressionVisitor
        {
            public Expression Modify(Expression expression)
            {
                return Visit(expression);
            }
            protected override Expression VisitLambda<T>(Expression<T> lambdaExpression)
            {

                // Note: Don't skip visiting parameter here.
                // SelectMany does not use parameter in lambda but we should still block it from evaluating
                base.VisitLambda(lambdaExpression);

                
                return lambdaExpression;
            }

            public override Expression Visit(Expression node)
            {

                if (node is UnaryExpression)
                {
                    var unary = (UnaryExpression)node;
                        this.Visit(unary.Operand);
                }


                if (node is BinaryExpression)
                {
                    this.Visit(((BinaryExpression)node).Left);
                    this.Visit(((BinaryExpression)node).Right);

                }

                if (node is MethodCallExpression)
                    this.Visit(((MethodCallExpression)node).Arguments[1]);

                //if (node.Left is MemberExpression && node.Right is ConstantExpression)
                //{
                //    //base.VisitBinary(b);
                //    return base.VisitBinary(node);
                //}

                //if (node.Left is BinaryExpression && node.Right is ConstantExpression)
                //{

                //}

                return base.Visit(node);
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                return base.VisitUnary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node is MethodCallExpression)
                {
                    var i = 0;
                    var methodCall = (MethodCallExpression)node;
                    // LIKE queries:
                   
                    if (methodCall.Method.Name == "Contains")
                    {
                        Expression collection;
                        Expression property;
                        if (methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 2)
                        {
                            collection = methodCall.Arguments[0];
                            property = methodCall.Arguments[1];
                        }
                        else if (!methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 1)
                        {
                            collection = methodCall.Object;
                            property = methodCall.Arguments[0];
                        }
                        else
                        {
                            throw new Exception("Unsupported method call: " + methodCall.Method.Name);
                        }
                        var values = (IEnumerable)GetValue(collection);
                        
                    }
                    throw new Exception("Unsupported method call: " + methodCall.Method.Name);
                }

                return base.VisitMethodCall(node);
            }

            protected override Expression VisitBinary(BinaryExpression binaryExpression)
            {

               


                //if (b.Left is BinaryExpression || b.Left is MethodCallExpression)
                //    this.Visit(b.Left);

                //if (b.Right is BinaryExpression || b.Right is MethodCallExpression)
                //    this.Visit(b.Right);

                //if (b.NodeType == ExpressionType.AndAlso)
                //{
                //    Expression left = this.Visit(b.Left);
                //    Expression right = this.Visit(b.Right);

                //    // Make this binary expression an OrElse operation instead of an AndAlso operation.  
                //    return Expression.MakeBinary(ExpressionType.OrElse, left, right, b.IsLiftedToNull, b.Method);
                //}

                return base.VisitBinary(binaryExpression);
            }
        }



        public PropertyInfo GetPropertyInfo<TSource, TProperty>(
    Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

            return propInfo;
        }


        private WherePart Recurse(ref int i, Expression expression, bool isUnary = false, string prefix = null, string postfix = null)
        {
            if (expression is UnaryExpression)
            {
                var unary = (UnaryExpression)expression;
                //return WherePart.Concat(NodeTypeToString(unary.NodeType), Recurse(ref i, unary.Operand, true));
                return Recurse(ref i, unary.Operand, true);

            }
            if (expression is BinaryExpression)
            {
                var body = (BinaryExpression)expression;
                return WherePart.Concat(Recurse(ref i, body.Left), NodeTypeToString(body.NodeType), Recurse(ref i, body.Right));
            }
            if (expression is ConstantExpression)
            {
                var constant = (ConstantExpression)expression;
                var value = constant.Value;
                if (value is int)
                {
                    return WherePart.IsSql(value.ToString());
                }
                if (value is string)
                {
                    if (prefix == null && postfix == null)
                        value = "'" + (string)value + "'";
                    else
                        value = prefix + (string)value + postfix;
                }
                if (value is DateTime)
                {
                    value = "'" + value + "'";
                }
                if (value is DateTime?)
                {
                    value = "'" + (string)value + "'";
                }
                if (value is TimeSpan)
                {
                    value = "'" + value + "'";
                }
                if (value is TimeSpan?)
                {
                    value = "'" + (string)value + "'";
                }
                if (value is bool && isUnary)
                {
                    return WherePart.Concat(WherePart.IsParameter(i++, value), "=", WherePart.IsSql("1"));
                }
                return WherePart.IsParameter(i++, value);
            }
            if (expression is MemberExpression)
            {
                var member = (MemberExpression)expression;

                if (member.Member is PropertyInfo)
                {
                    var property = (PropertyInfo)member.Member;
                    //var colName = _tableDef.GetColumnNameFor(property.Name);
                    var colName = property.Name;
                    if (isUnary && member.Type == typeof(bool))
                    {
                        return WherePart.Concat(Recurse(ref i, expression), "=", WherePart.IsParameter(i++, true));
                    }
                    return WherePart.IsSql("[" + colName + "]");
                }
                if (member.Member is FieldInfo)
                {
                    var value = GetValue(member);
                    if (value is string)
                    {
                        value = prefix + (string)value + postfix;
                    }
                    return WherePart.IsParameter(i++, value);
                }
                throw new Exception($"Expression does not refer to a property or field: {expression}");
            }
            if (expression is MethodCallExpression)
            {
                var methodCall = (MethodCallExpression)expression;
                // LIKE queries:
                if (methodCall.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
                {
                    return WherePart.Concat(Recurse(ref i, methodCall.Object), "LIKE", Recurse(ref i, methodCall.Arguments[0], prefix: "'%", postfix: "%'"));
                }
                if (methodCall.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
                {
                    return WherePart.Concat(Recurse(ref i, methodCall.Object), "LIKE", Recurse(ref i, methodCall.Arguments[0], prefix: "'", postfix: "%'"));
                }
                if (methodCall.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
                {
                    return WherePart.Concat(Recurse(ref i, methodCall.Object), "LIKE", Recurse(ref i, methodCall.Arguments[0], prefix: "'%", postfix: "'"));
                }
                // IN queries:
                if (methodCall.Method.Name == "Contains")
                {
                    Expression collection;
                    Expression property;
                    if (methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 2)
                    {
                        collection = methodCall.Arguments[0];
                        property = methodCall.Arguments[1];
                    }
                    else if (!methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 1)
                    {
                        collection = methodCall.Object;
                        property = methodCall.Arguments[0];
                    }
                    else
                    {
                        throw new Exception("Unsupported method call: " + methodCall.Method.Name);
                    }
                    var values = (IEnumerable)GetValue(collection);
                    return WherePart.Concat(Recurse(ref i, property), "IN", WherePart.IsCollection(ref i, values));
                }
                throw new Exception("Unsupported method call: " + methodCall.Method.Name);
            }
            throw new Exception("Unsupported expression: " + expression.GetType().Name);
        }

        public string ValueToString(object value, bool isUnary, bool quote)
        {
            if (value is bool)
            {
                if (isUnary)
                {
                    return (bool)value ? "(1=1)" : "(1=0)";
                }
                return (bool)value ? "1" : "0";
            }
            //return _provider.ValueToString(value, quote);
            return value.ToString();
        }

        private static object GetValue(Expression member)
        {
            // source: http://stackoverflow.com/a/2616980/291955
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private static string NodeTypeToString(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Negate:
                    return "-";
                case ExpressionType.Not:
                    return "NOT";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Subtract:
                    return "-";
            }
            throw new Exception($"Unsupported node type: {nodeType}");
        }
    }

    public class WherePart
    {
        public string Sql { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public static WherePart IsSql(string sql)
        {
            return new WherePart()
            {
                Parameters = new Dictionary<string, object>(),
                Sql = sql
            };
        }

        public static WherePart IsParameter(int count, object value)
        {
            return new WherePart()
            {
                Parameters = { { count.ToString(), value } },
                Sql = $"@{count}"
            };
        }

        public static WherePart IsCollection(ref int countStart, IEnumerable values)
        {
            var parameters = new Dictionary<string, object>();
            var sql = new StringBuilder("(");
            foreach (var value in values)
            {
                parameters.Add((countStart).ToString(), value);
                sql.Append($"@{countStart},");
                countStart++;
            }
            if (sql.Length == 1)
            {
                sql.Append("null,");
            }
            sql[sql.Length - 1] = ')';
            return new WherePart()
            {
                Parameters = parameters,
                Sql = sql.ToString()
            };
        }

        public static WherePart Concat(string @operator, WherePart operand)
        {
            return new WherePart()
            {
                Parameters = operand.Parameters,
                Sql = $"({@operator} {operand.Sql})"
            };
        }

        public static WherePart Concat(WherePart left, string @operator, WherePart right)
        {
            return new WherePart()
            {
                Parameters = left.Parameters.Union(right.Parameters).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Sql = $"({left.Sql} {@operator} {right.Sql})"
            };
        }
    }
}
