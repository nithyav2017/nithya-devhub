using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QueryBuilder.Utility
{
    public static class ResultHelper 
    {
        public static object SafeConvert(object value, Type targetType)
        {
            if (value == null) return null;

            var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Already the right type
            if (nonNullableType.IsInstanceOfType(value))
                return value;

            // Primitive types (IConvertible)
            if (value is IConvertible)
                return Convert.ChangeType(value, nonNullableType);

            // Collections or complex types → return as-is
            return value;
        }
        public static MethodInfo GetJoinMethod(string joinKind, Type outerType, Type innerType,Type keyType,  Type resultType)
        {
            var methodName = joinKind switch
            {
                "Inner" => "Join",
                "Left" => "GroupJoin", // adjust if needed
                _ => throw new NotSupportedException($"Join type {joinKind} not supported")
            };

            var method = typeof(Queryable)
                .GetMethods()
                .Where(m => m.Name == methodName && m.IsGenericMethodDefinition)
                .Where(m =>
                {
                    var parameters = m.GetParameters();
                    // Inner Join has 5 parameters: outer, inner, outerKey, innerKey, resultSelector
                    return methodName == "Join" ? parameters.Length == 5 : parameters.Length == 5;
                })
                .Single();

            return method;
        }

      
        public static MemberExpression FindPropertyRecursive(Expression param, string propertyName, Type targetType)
        {
            
            var type = param.Type;

            var prop = type.GetProperty(propertyName) ??(MemberInfo?) type.GetField(propertyName);
            if (prop != null)
                return Expression.PropertyOrField(param, propertyName);

            // If param is TransparentIdentifier
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TransparentIdentifier<,>))
            {
                 
                var outerExpr = Expression.Property(param, "Outer");
                var found = FindPropertyRecursive(outerExpr, propertyName,null);
                if (found != null)
                    return found;

                // Recurse into Inner
                var innerExpr = Expression.Property(param, "Inner");
                return FindPropertyRecursive(innerExpr, propertyName,null);
            }

            // Otherwise, regular property
            var propInfo = type.GetProperty(propertyName);
            if (propInfo != null) return Expression.Property(param, propInfo);

            return null!; 
        }

        public static Expression UnwrapTransparentIdentifiers(Expression source, Type targetType)
        {
            var current = source;
            Type currentType = current.Type;

            while (currentType.IsTransparentIdentifier())
            {
                // TI has properties: Left, Right
                var leftProp = currentType.GetProperty("Inner");
                var rightProp = currentType.GetProperty("Outer");

                var leftType = leftProp.PropertyType;
                var rightType = rightProp.PropertyType;

                if (leftType == targetType)
                    return Expression.Property(current, leftProp);

                if (rightType == targetType)
                    return Expression.Property(current, rightProp);

                // Else target might be deeper, so decide which branch to walk
                if (targetType == leftType || targetType.IsAssignableFrom(leftType))
                    current = Expression.Property(current, leftProp);
                else
                    current = Expression.Property(current, rightProp);

                currentType = current.Type;
            }

            return current;
        } 
        private static bool IsTransparentIdentifier(this Type type)
        {
            return type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(TransparentIdentifier<,>);
        }
    } 

}
