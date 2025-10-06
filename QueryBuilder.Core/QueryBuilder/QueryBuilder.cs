using System;
using System.Linq.Expressions;

namespace QueryBuilder
{
    public class QueryBuilder<T>
    {
        private IQueryable<T> _query;
       
        private Expression<Func<T, bool>> _expression;

        public QueryBuilder(IQueryable<T> source)
        {
            _query = source;
        }

        public QueryBuilder()
        {

        }

        public QueryBuilder<T> AndWhere(string property, string op, object value )
            => BuildExpression(property, op, value, useAnd:true);

        public QueryBuilder<T> OrElse(string property, string op , object value )
            => BuildExpression(property, op, value, useAnd:false);  
        
        public Expression<Func<T, bool>> Build()
        {
            return _expression ?? (x => true);
        }

        public QueryBuilder<T> BuildExpression(string propertyName, string op, object value, bool useAnd)
        {
           
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value, property.Type);
 
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

        }
    }
}
