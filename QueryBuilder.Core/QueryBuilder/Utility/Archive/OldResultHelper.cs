using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueryBuilder.Utility.Archive
{
    public static class OldResultHelper
    {
        public static dynamic Flatten(object obj)
        {
            if (obj == null) return null;

            var expando = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;

            var type = obj.GetType();

            if (type.IsGenericType && type.Name.StartsWith("TransparentIdentifier"))
            {
                var outer = type.GetProperty("Outer")!.GetValue(obj);
                var inner = type.GetProperty("Inner")!.GetValue(obj);

                foreach (var prop in (IDictionary<string, object>)Flatten(outer))
                {
                    expando[prop.Key] = prop.Value;
                }

                foreach (var prop in (IDictionary<string, object>)Flatten(inner))
                {
                    expando[prop.Key] = prop.Value;
                }

            }
            else
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    expando[prop.Name] = prop.GetValue(obj);
                }
            }
            return expando;
        }

        public static LambdaExpression BuildJoinResultSelector(Type outerType, Type innerType, Type resultType)
        {
            // Parameters representing the left and right elements in the join
            var outerParam = Expression.Parameter(outerType, "o");
            var innerParam = Expression.Parameter(innerType, "i");

            // Get the constructor of the TransparentIdentifier<Outer, Inner>
            var constructor = resultType.GetConstructor(new[] { outerType, innerType });
            if (constructor == null)
                throw new InvalidOperationException($"Constructor not found for {resultType}");

            // Create a new instance of TransparentIdentifier<Outer, Inner>(o, i)
            var newExpr = Expression.New(constructor, outerParam, innerParam);

            // Bind the Outer and Inner properties to the parameters
            var memberInit = Expression.MemberInit(
                newExpr,
                Expression.Bind(resultType.GetProperty("Outer")!, outerParam),
                Expression.Bind(resultType.GetProperty("Inner")!, innerParam)
            );

            // Return a lambda (o, i) => new TransparentIdentifier<Outer, Inner> { Outer = o, Inner = i }
            return Expression.Lambda(memberInit, outerParam, innerParam);
        }
        public static LambdaExpression BuildKeySelector(Type type, string propertyName)
        {
            var param = Expression.Parameter(type, "x");

            Expression propertyExpr;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TransparentIdentifier<,>))
                propertyExpr = QueryBuilder.Utility.ResultHelper.FindPropertyRecursive(param, propertyName, type); // join result
            else
                propertyExpr = Expression.PropertyOrField(param, propertyName); // root table

            return Expression.Lambda(propertyExpr, param);
        }
        public static LambdaExpression ConvertToTypedLambda(this LambdaExpression lambda, Type paramType, Type returnType)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(paramType, returnType);
            return Expression.Lambda(delegateType, lambda.Body, lambda.Parameters);
        }
        private static IQueryable GetQueryable(DbContext context, Type entitytype)
        {
            var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!;
            var generic = method.MakeGenericMethod(entitytype);
            return (IQueryable)generic.Invoke(context, null)!;
        }
        public static bool ApplyFilter(Dictionary<string, object> flatItem, string propertyName, string op, object value)
        {
            if (!flatItem.TryGetValue(propertyName, out var actualValue) || actualValue == null)
                return false;

            var convertedValue = Convert.ChangeType(value, actualValue.GetType());

            return op switch
            {
                "==" => actualValue.Equals(convertedValue),
                "!=" => !actualValue.Equals(convertedValue),
                ">" => Comparer.DefaultInvariant.Compare(actualValue, convertedValue) > 0,
                "<" => Comparer.DefaultInvariant.Compare(actualValue, convertedValue) < 0,
                ">=" => Comparer.DefaultInvariant.Compare(actualValue, convertedValue) >= 0,
                "<=" => Comparer.DefaultInvariant.Compare(actualValue, convertedValue) <= 0,
                "Contains" => actualValue.ToString().Contains(convertedValue.ToString()),
                "StartsWith" => actualValue.ToString().StartsWith(convertedValue.ToString()),
                "EndsWith" => actualValue.ToString().EndsWith(convertedValue.ToString()),
                _ => throw new NotSupportedException($"Operator {op} is not supported")
            };
        }

        
        public static string ResolveNestedPath(string fieldName, int joinPath)
        {
            string path = fieldName;
            for (int i = 0; i < joinPath; i++)
            {
                path = $"Outer.{path}";
            }
            return path;
        }


       /* public static IQueryable<T> Build_1<T>(IQueryable<T> source, DbContext context)
        {
            IQueryable current = source;
            Type currentType = typeof(T);

            // 1. Apply all joins first
            foreach (var join in _joins)
            {
                var outerType = current.ElementType;
                IQueryable innerQuery = (IQueryable)context
                                        .GetType()
                                        .GetMethod("Set", Type.EmptyTypes)!       // No parameters
                                        .MakeGenericMethod(join.InnerType)       // Specify the entity type
                                        .Invoke(context, null)!;

                // Build key selectors


                var outerParam = Expression.Parameter(current.ElementType, "o");
                var innerParam = Expression.Parameter(join.InnerType, "i");

                var innerKeyExpr = Expression.PropertyOrField(innerParam, join.InnerKey);

                var outerKeySelector = Expression.Lambda(
                                                ResultHelper.FindPropertyRecursive(outerParam, join.OuterKey, outerType),
                                                outerParam
                                            );
                var innerKeySelector = Expression.Lambda(
                                                Expression.PropertyOrField(innerParam, join.InnerKey),
                                                innerParam
                                            );
                var keyType = outerKeySelector.Body.Type;

                var resultType = typeof(TransparentIdentifier<,>).MakeGenericType(outerType, join.InnerType);

                var constructor = resultType.GetConstructor(new[] { current.ElementType, join.InnerType });

                var resultSelectorParamOuter = outerParam;
                var resultSelectorParamInner = innerParam;
                var resultSelectorExpr = Expression.New(constructor!, resultSelectorParamOuter, resultSelectorParamInner);
                var resultSelector = Expression.Lambda(resultSelectorExpr, resultSelectorParamOuter, resultSelectorParamInner);

                // Perform join
                var joinMethod = ResultHelper.GetJoinMethod(join.JoinType, outerType, join.InnerType, keyType, resultType)
                                                .MakeGenericMethod(outerType, join.InnerType, keyType, resultType);
                current = (IQueryable)joinMethod.Invoke(null, new object[] { current, innerQuery, outerKeySelector, innerKeySelector, resultSelector });
            }


            if (_typeFilters.Any())
            {
                var joinedType = current.ElementType;
                var predicate = BuildPredicateNew(joinedType, _typeFilters, false);

                if (predicate != null)
                {
                    var whereMethod = typeof(Queryable).GetMethods()
                        .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(joinedType);

                    current = (IQueryable)whereMethod.Invoke(null, new object[] { current, predicate });
                }
            }

            var param = Expression.Parameter(current.ElementType, "x");

            var rootExpr = QueryBuilder.Utility.ResultHelper.UnwrapTransparentIdentifiers(param, current.ElementType);
            var delegateType = typeof(Func<,>).MakeGenericType(current.ElementType, typeof(T));
            var selectLambda = Expression.Lambda(delegateType, rootExpr!, param);


            current = current.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Select",
                    new Type[] { current.ElementType, typeof(T) },
                    current.Expression,
                    selectLambda
                )
            );

            if (_typeFilters.TryGetValue(typeof(T), out var rootFilters) && rootFilters.Count > 0)
            {
                var predicate = BuildPredicateNew(typeof(T), _typeFilters, true);
                if (predicate != null)
                {
                    var whereMethod = typeof(Queryable).GetMethods()
                        .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(typeof(T));

                    current = (IQueryable<T>)whereMethod.Invoke(null, new object[] { current, predicate });
                }
            }
            return (IQueryable<T>)current;

        }

        public LambdaExpression BuildPredicateNew(Type entityType, Dictionary<Type, List<FilterCondition>> filters, bool rootTableFilter)
        {

            var typeFiltersForCurrent = GetFiltersForType(entityType, filters);
            if (typeFiltersForCurrent.Count == 0)
                return null;

            ParameterExpression param = Expression.Parameter(entityType, "x");
            Expression? combined = null;
            Expression? propExpr = null; ;
            foreach (var kvp in filters)
            {
                Type targetType = kvp.Key;
                var conditions = kvp.Value;

                foreach (var f in conditions)
                {

                    if (entityType == targetType)

                        propExpr = Expression.PropertyOrField(param, f.PropertyName);
                    else
                    {
                        if (!rootTableFilter)
                            continue;
                        // Joined entity — must search inside TransparentIdentifier
                        propExpr = QueryBuilder.Utility.ResultHelper.FindPropertyRecursive(
                            param,
                            f.PropertyName,
                            targetType
                        );
                        if (propExpr == null)
                            continue; // Property not found in this TransparentIdentifier
                    }
                    // Only allow scalar properties
                    if (propExpr.Type.IsClass && propExpr.Type != typeof(string))
                    {
                        throw new InvalidOperationException(
                            $"Cannot filter on entity type '{propExpr.Type.Name}'. Use scalar property instead."
                        );
                    }

                    var constant = Expression.Constant(Convert.ChangeType(f.Value, propExpr.Type), propExpr.Type);

                    //   var constant = Expression.Constant(f.Value, propExpr.Type);


                    Expression expr = f.Operator switch
                    {
                        "==" => Expression.Equal(propExpr, constant),
                        "!=" => Expression.NotEqual(propExpr, constant),
                        ">" => Expression.GreaterThan(propExpr, constant),
                        ">=" => Expression.GreaterThanOrEqual(propExpr, constant),
                        "<" => Expression.LessThan(propExpr, constant),
                        "<=" => Expression.LessThanOrEqual(propExpr, constant),
                        _ => throw new NotSupportedException()
                    };

                    combined = combined == null
                        ? expr
                        : f.UseAnd
                            ? Expression.AndAlso(combined, expr)
                            : Expression.OrElse(combined, expr);

                }
            }
            return combined != null
                ? Expression.Lambda(combined, param)
                : null!;
        }


         public static QueryBuilder<T> BuildExpression(string propertyName, string op, object value, bool useAnd)
        {
           
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = QueryBuilder.Utility.ResultHelper.FindPropertyRecursive(parameter, propertyName,null);
        
            var convertedValue = Convert.ChangeType(value, property.Type);
            var constant = Expression.Constant(convertedValue, property.Type);

            //var left= Expression.PropertyorField(parameter, propertyName);
            var right = Expression.Constant(value);

            Expression body = op switch
            {
                "==" => Expression.Equal(property, constant),
                "!=" => Expression.NotEqual(property, constant),
                ">" => Expression.GreaterThan(property, constant),
                "<" => Expression.LessThan(property, constant),
                ">=" => Expression.GreaterThanOrEqual(property, constant),
                "<=" => Expression.LessThanOrEqual(property, constant),
                "Contains" => Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, constant),
                "StartsWith" => Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, constant),
                "EndsWith" => Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, constant),
                _ => throw new NotSupportedException($"Operator {op} not supported")
            };


            var lambda = Expression.Lambda<Func<T,bool>>(body, parameter);

            if (_expression == null)
            {
                _expression =   lambda;
            }
            else
            {
                var invoked = Expression.Invoke(lambda, _expression.Parameters);
                Expression combined = useAnd
                    ? Expression.AndAlso(_expression.Body,invoked) : Expression.OrElse(_expression.Body,invoked);

                _expression = Expression.Lambda<Func<T, bool>>(combined, _expression.Parameters);
            }
            return this;

        } */

    }
}
