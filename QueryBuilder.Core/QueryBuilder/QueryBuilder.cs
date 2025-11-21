using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace QueryBuilder
{
    public class QueryBuilder<T>   
    {
        private IQueryable<T> _query;       
        private Expression<Func<T, bool>> _expression;
         
        private readonly List<JoinsType> _joins = new();
        private readonly List<FilterCondition> _filters = new();
        private readonly Dictionary<Type, List<FilterCondition>> _typeFilters = new();
        public List<string> GroupByFields { get; } = new List<string>();
        public List<(string Field, string Aggregate, string Alias)> Aggregates { get; } = new List<(string, string, string)>();

        private int? _skip;
        private int? _take;

         private Type _currentOuterType  ;
        
        int _joinDepth = 0;

        public QueryBuilder()
        {
            
        }



        public QueryBuilder(IQueryable<T> source)
        {
            _query = source;
        }

        
 
        private QueryBuilder<T> AddFilter(string propertyName, string op, object value, Type? tableType, bool useAnd)
        {
            var type = tableType ?? typeof(T); // default to root table
            if (!_typeFilters.ContainsKey(type))
                _typeFilters[type] = new List<FilterCondition>();

            _typeFilters[type].Add(new FilterCondition
            {
                PropertyName = propertyName,
                Operator = op,
                Value = value,
                UseAnd = useAnd // keep track if this condition should be AND/OR
            });

            return this;
        }

        public QueryBuilder<T> AndWhere(string propertyName, string op, object value, Type? tableType = null)
        {
            return AddFilter(propertyName, op,value, tableType, useAnd: true);
        }

        public QueryBuilder<T> OrElse(string propertyName, string op, object value, Type? tableType = null)
        {
            return AddFilter(propertyName, op,value, tableType, useAnd: false);
        }

        public QueryBuilder<T> In(string propertyName, string op, object value, Type? tableType = null)
        {
            return AddFilter(propertyName, op, value, tableType, useAnd: true);
        }
         
        public QueryBuilder<T> Skip(int count)
        {
            _skip = count;
            return this;
        }

        public QueryBuilder<T> Take(int count)
        {
            _take = count;
            return this;
        }


        public Expression<Func<T, bool>> Build()       
        {
            return _expression ?? (x => true);
        }
         
        public QueryBuilder<T> AddJoin(Type innerType, string fromKey, string toKey, string joinType = "Inner")
        {
            _joins.Add( new JoinsType
            {

                InnerType = innerType,//typeof(TJoin),
                OuterType = _currentOuterType,
                InnerKey = fromKey,
                OuterKey = toKey,
                JoinType = joinType
            });
        
            return this;
        }
        public IQueryable ApplyJoin(IQueryable outerQuery, JoinsType join, IQueryable innerQuery, DbContext context, ref Type currentOuterType)
        { 
            var outerParam = Expression.Parameter(currentOuterType, "o");
            var innerParam = Expression.Parameter(join.InnerType, "i");

            // Build key selectors
            var outerKeyExpr = QueryBuilder.Utility.ResultHelper. FindPropertyRecursive(outerParam, join.OuterKey,null);
            var innerKeyExpr = Expression.PropertyOrField(innerParam, join.InnerKey);


            var outerKey = Expression.Convert(outerKeyExpr, innerKeyExpr.Type);

            var outerKeySelector = Expression.Lambda(outerKey, outerParam);
            var innerKeySelector = Expression.Lambda(innerKeyExpr, innerParam);

            // Build result type: TransparentIdentifier<Outer, Inner>
            var resultType = typeof(TransparentIdentifier<,>).MakeGenericType(currentOuterType, join.InnerType);

            // Build resultSelector: (o, i) => new TransparentIdentifier<Outer, Inner> { Outer = o, Inner = i }
            var constructor = resultType.GetConstructor(new[] { currentOuterType, join.InnerType });
            var newExpr = Expression.New(constructor, outerParam, innerParam);

             var memberInit = Expression.MemberInit(
                newExpr,
                Expression.Bind(resultType.GetProperty("Outer"), outerParam),
                Expression.Bind(resultType.GetProperty("Inner"), innerParam)
            ); 

             
            var resultSelector = Expression.Lambda(memberInit, outerParam, innerParam);

            // Build Join method call
            var joinMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Join" && m.GetParameters().Length == 5)
                .MakeGenericMethod(currentOuterType, join.InnerType, outerKeyExpr.Type, resultType);

            var call = Expression.Call(
                joinMethod,
                outerQuery.Expression,
                innerQuery.Expression,
                outerKeySelector,
                innerKeySelector,
                resultSelector
            );

            var joinedQuery = outerQuery.Provider.CreateQuery(call);

            // Update the current outer type for the next join
            currentOuterType = resultType;

            return joinedQuery;
        } 
        LambdaExpression BuildPredicate(Type type, List<FilterCondition> filters)
        {
            Expression combined = null;
            var param = Expression.Parameter(type, "x");

            foreach (var f in filters)
            {
                var property = Expression.PropertyOrField(param, f.PropertyName);
                Expression expr=null;

                var elementType = property.Type;
                IEnumerable values;
                Type? listType;
                MethodInfo containsMethod;

                switch (f.Operator)
                {
                    
                    case "==":
                        expr = Expression.Equal(property,
                            Expression.Constant(f.Value, property.Type));
                        break;

                    case "!=":
                        expr = Expression.NotEqual(property,
                            Expression.Constant(f.Value, property.Type));
                        break;

                    case ">":
                        expr = Expression.GreaterThan(property,
                            Expression.Constant(f.Value, property.Type));
                        break;

                    case ">=":
                        expr = Expression.GreaterThanOrEqual(property,
                            Expression.Constant(f.Value, property.Type));
                        break;

                    case "<":
                        expr = Expression.LessThan(property,
                            Expression.Constant(f.Value, property.Type));
                        break;

                    case "<=":
                        expr = Expression.LessThanOrEqual(property,
                            Expression.Constant(f.Value, property.Type));
                        break;
                    case "StartsWith":
                        expr = Expression.Call(property,
                                               typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
                                               Expression.Constant(f.Value, typeof(string)));
                        break;
                    case "EndsWith":
                        expr = Expression.Call(property,
                                              typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!,
                                               Expression.Constant(f.Value, typeof(string)));
                        break;
                    case "Contains":
                        expr = Expression.Call(property,
                                              typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                                               Expression.Constant(f.Value, typeof(string)));
                        break;
                    case "IN": 
                          elementType = property.Type;                       
                          listType = typeof(List<>).MakeGenericType(elementType);
                          expr= CreateInPredicate(property,f); 
                          break;
                    case "NOT IN":
                        var containExpr = CreateInPredicate(property, f);
                        expr = Expression.Not(containExpr);
                        break;
                    default:
                        throw new NotSupportedException($"Operator '{f.Operator}' not supported");
                }

                
                combined = combined == null
                    ? expr
                    : f.UseAnd
                        ? Expression.AndAlso(combined, expr)
                        : Expression.OrElse(combined, expr);
            }

            return Expression.Lambda(combined, param);

        }

        public QueryBuilder<T> GroupBy(params string[] fields)
        {
            GroupByFields.AddRange(fields);
            return this;
        }

        public QueryBuilder<T> SelectAggregate(string field, string aggregate, string alias)
        {
            Aggregates.Add((field, aggregate, alias));
            return this;
        }

        

        public IQueryable Build(IQueryable<T> source, DbContext context)
        {
            IQueryable current = source;
            Expression? combined = null;


            if (_typeFilters.TryGetValue(typeof(T), out var rootFilters) && rootFilters.Any())
            {
                // Build a single predicate combining all root filters (AND / OR)
                var predicate = BuildPredicate(typeof(T), rootFilters); // returns Expression<Func<T,bool>>

                if (predicate != null)
                {
                    current = current.Provider.CreateQuery(
                        Expression.Call(
                            typeof(Queryable),
                            "Where",
                            new Type[] { current.ElementType },
                            current.Expression,
                            predicate
                        )
                    );
                }
            }
            else
            {
                current = source;
            }

            foreach (var join in _joins)
            {
                _currentOuterType = current.ElementType;
 
                IQueryable innerQuery = (IQueryable)context
                                 .GetType()
                                 .GetMethod("Set", Type.EmptyTypes)!
                                 .MakeGenericMethod(join.InnerType)
                                 .Invoke(context, null)!; ;

                if (_typeFilters.TryGetValue(join.InnerType, out var joinFilters) && joinFilters.Count > 0)
                {
                    var predicate = BuildPredicate(join.InnerType, joinFilters); 

                    innerQuery = innerQuery.Provider.CreateQuery(
                      Expression.Call(
                          typeof(Queryable),
                          "Where",
                          new Type[] { join.InnerType },
                          innerQuery.Expression,
                          predicate
                      )
      );
                }


                current = ApplyJoin(current, join, innerQuery, context, ref _currentOuterType);

                _joinDepth++;
            }

            
            return current;

        } 
        

        private static List<FilterCondition> GetFiltersForType(Type elementType, Dictionary<Type, List<FilterCondition>> typeFilters)
        {
            if (typeFilters.TryGetValue(elementType, out var filters))
                return filters;

            // Check if it's a TransparentIdentifier
            if (elementType.IsGenericType && elementType.Name.StartsWith("TransparentIdentifier"))
            {
                var outerType = elementType.GetProperty("Outer")!.PropertyType;
                var innerType = elementType.GetProperty("Inner")!.PropertyType;

                var combined = new List<FilterCondition>();
                combined.AddRange(GetFiltersForType(outerType, typeFilters));
                combined.AddRange(GetFiltersForType(innerType, typeFilters));
                return combined;
            }

            return new List<FilterCondition>(); // no filters found
        }
        private Expression CreateInPredicate(Expression property,    FilterCondition filter)
        {
            IEnumerable values;
            if (filter.Value is IEnumerable enumerable && !(filter.Value is string))
            {
                values = enumerable.Cast<object>().Select(v => Convert.ChangeType(v, property.Type)).ToList();
            }
            else if (filter.Value.GetType().IsValueType && filter.Value.GetType().IsGenericType &&
                     filter.Value.GetType().FullName!.StartsWith("System.ValueTuple"))
            {
                var fields = filter.Value.GetType().GetFields();
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(property.Type))!;

                foreach (var field in fields)
                {
                    list.Add(Convert.ChangeType(field.GetValue(filter.Value)!, property.Type));
                }

                values = list;
            }
            else
            {
                // Single scalar value
                values = new List<object> { Convert.ChangeType(filter.Value, property.Type) };
            }
           var containsMethod = typeof(Enumerable)
                                                   .GetMethods()
                                                   .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                                                   .MakeGenericMethod(property.Type);

           return Expression.Call(null, containsMethod, Expression.Constant(values), property);

        } 
    }
}
