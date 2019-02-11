using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ReactiveUI;
using Splat.ReactiveUIExtensions.Attributes;
using Splat.ReactiveUIExtensions.Helpers;

namespace Splat.ReactiveUIExtensions
{
    public static class LocatorExtensions
    {
        public static void RegisterLazySingleton<TImpl, TAs>(this IMutableDependencyResolver resolver)
                    where TImpl : TAs
        {
            RegisterType(resolver, typeof(TImpl).GetTypeInfo(), typeof(TAs).GetTypeInfo(), singleInstance: true);
        }

        public static void RegisterType<TImpl, TAs>(this IMutableDependencyResolver resolver)
            where TImpl : TAs
        {
            RegisterType(resolver, typeof(TImpl).GetTypeInfo(), typeof(TAs).GetTypeInfo());
        }

        public static void RegisterViewsAndViewModels(this IMutableDependencyResolver resolver, Assembly assembly, Type baseViewModelType)
        {
            RegisterViewsAndViewModels<IViewFor>(resolver, assembly, baseViewModelType);
        }

        public static void RegisterViewsAndViewModels<TIViewFor>(this IMutableDependencyResolver resolver, Assembly assembly, Type baseViewModelType)
        {
            // for each type that implements IViewFor
            foreach (TypeInfo ti in assembly.DefinedTypes
                .Where(ti => !ti.IsAbstract && !ti.IsGenericTypeDefinition))
            {
                RegisterViewsAndViewModels<TIViewFor>(resolver, ti, baseViewModelType);
            }
        }

        public static void RegisterViewsAndViewModels(this IMutableDependencyResolver resolver, TypeInfo ti, Type baseViewModelType)
        {
            RegisterViewsAndViewModels<IViewFor>(resolver, ti, baseViewModelType);
        }

        public static void RegisterViewsAndViewModels<TIViewFor>(this IMutableDependencyResolver resolver, TypeInfo ti, Type baseViewModelType)
        {
            if (ti.ImplementedInterfaces.Contains(typeof(TIViewFor)))
            {
                // grab the first _implemented_ interface that also implements IViewFor, this should be the expected IViewFor<>
                foreach (Type ivf in ti.ImplementedInterfaces.Where(t =>
                    t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(TIViewFor))))
                {
                    // need to check for null because some classes may implement IViewFor but not IViewFor<T> - we don't care about those
                    if (ivf != null)
                    {
                        // my kingdom for c# 6!
                        RegisterType(resolver, ti, ivf);
                    }
                }
            }
            else if (baseViewModelType.IsAssignableFrom(ti))
            {
                RegisterType(resolver, ti, ti);
                RegisterType(resolver, ti, baseViewModelType, ti.FullName);
                if (baseViewModelType.IsInterface)
                {
                    foreach (Type vmt in ti.ImplementedInterfaces.Where(t =>
                        t.GetTypeInfo().ImplementedInterfaces.Contains(baseViewModelType)))
                    {
                        RegisterType(resolver, ti, vmt);
                    }
                }
            }
        }

        private static void RegisterType(IMutableDependencyResolver resolver, TypeInfo ti, Type serviceType, string contract = null, bool singleInstance = false)
        {
            if (string.IsNullOrEmpty(contract))
            {
                ViewContractAttribute contractSource = ti.GetCustomAttribute<ViewContractAttribute>();
                contract = contractSource != null ? contractSource.Contract : string.Empty;
            }

            Func<object> factory = TypeFactoryCreator(ti, resolver);
            if (singleInstance
                || ti.GetCustomAttribute<SingleInstanceViewAttribute>() != null
                || ti.GetCustomAttribute<SingleInstanceAttribute>() != null)
            {
                resolver.RegisterLazySingleton(factory, serviceType, contract);
            }
            else
            {
                resolver.Register(factory, serviceType, contract);
            }
        }

        public static Func<TypeInfo, IMutableDependencyResolver, Func<object>> TypeFactoryCreator { get; set; } =
            (TypeInfo typeInfo, IMutableDependencyResolver resolver) =>
            {
                ConstructorInfo constructorInfo = typeInfo.DeclaredConstructors.Where(ci => ci.IsPublic)
                    .OrderByDescending(ci => ci.GetParameters().Count())
                    .First();
                ParameterInfo[] parameters = constructorInfo.GetParameters();
                if (!parameters.Any())
                {
                    return Expression.Lambda<Func<object>>(Expression.New(constructorInfo)).Compile();
                }

                Func<object> func = () =>
                   {
                       return constructorInfo.Invoke(
                           parameters.Select(p =>
                           {
                               if (typeof(string) == p.ParameterType || p.ParameterType.IsPrimitive)
                               {
                                   return null;//we don't do non-class injections
                               }
                               if (typeof(IEnumerable).IsAssignableFrom(p.ParameterType))
                               {
                                   Type elementType = ReflectionHelpers.GetElementType(p.ParameterType);
                                   return resolver.GetServices(elementType).ToArrayOfType(elementType);
                               }

                               return resolver.GetService(p.ParameterType);
                           })
                           .ToArray());
                   };
                return func;
            };
    }
}
