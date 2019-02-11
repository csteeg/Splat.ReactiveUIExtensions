using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Splat.ReactiveUIExtensions.Helpers
{
    public static class ReflectionHelpers
    {
        public static Type GetElementType(Type type)
        {
            // Type is Array
            // short-circuit if you expect lots of arrays
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            // type implements/extends IEnumerable<T>;
            Type enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }

        public static Array ToArrayOfType(this IEnumerable @this, Type newType)
        {
            Array result = Array.CreateInstance(newType, @this.Cast<object>().Count());
            int i = 0;
            foreach (object item in @this)
            {
                result.SetValue(item, i++);
            }

            return result;
        }

        public static Type GetImplementedInterfaceOf<T>(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(
                i => i.GetTypeInfo().ImplementedInterfaces.Contains(typeof(T)));
        }

        public static IEnumerable<Type> GetImplementedInterfacesOf<T>(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.Where(
                i => i.GetTypeInfo().ImplementedInterfaces.Contains(typeof(T)));
        }
    }
}
