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

        /// <summary>
        /// Registers the views and view models.
        /// </summary>
        /// <param name="resolver">Resolver.</param>
        /// <param name="assembly">The assembly to scan for views and viewmodels.</param>
        /// <typeparam name="TBaseViewModelType">The base viewmodel type</typeparam>
        public static void RegisterViewsAndViewModels<TBaseViewModel>(this IMutableDependencyResolver resolver, Assembly assembly)
        {
            RegisterViewsAndViewModels<TBaseViewModel, IViewFor>(resolver, assembly);
        }

        /// <summary>
        /// Registers the views and view models.
        /// </summary>
        /// <param name="resolver">Resolver.</param>
        /// <param name="assembly">The assembly to scan for views and viewmodels.</param>
        /// <typeparam name="TBaseViewModelType">The base viewmodel type</typeparam>
        /// <typeparam name="TIViewFor">The basetype for views.</typeparam>
        public static void RegisterViewsAndViewModels<TBaseViewModel, TIViewFor>(this IMutableDependencyResolver resolver, Assembly assembly)
        {
            foreach (TypeInfo ti in assembly.DefinedTypes.Where(ti => !ti.IsAbstract && !ti.IsGenericTypeDefinition))
            {
                RegisterView<TIViewFor>(resolver, ti);
                RegisterViewModel<TBaseViewModel>(resolver, ti);
            }
        }

        /// <summary>
        /// Registers the views.
        /// </summary>
        /// <param name="resolver">Resolver.</param>
        /// <param name="assembly">The assembly to scan for views.</param>
        /// <param name="registerDirectAlso">If set to <c>true</c>, this will register the view on the container as an implementation of TIViewFor.
        /// Otherwise, only second level implementations are registered. Eg, if TIViewFor is <c>IViewFor</c>, only <c>IViewFor&lt;object&gt;</c> is registered, not IViewFor self.
        /// Default is <c>false</c>.</param>
        /// <typeparam name="TIViewFor">The basetype for views.</typeparam>
        public static void RegisterViews<TIViewFor>(this IMutableDependencyResolver resolver, Assembly assembly, bool registerDirectAlso = false)
        {
            foreach (TypeInfo ti in assembly.DefinedTypes.Where(ti => !ti.IsAbstract && !ti.IsGenericTypeDefinition))
            {
                RegisterView<TIViewFor>(resolver, ti, registerDirectAlso);
            }
        }

        /// <summary>
        /// Registers the view models.
        /// </summary>
        /// <param name="resolver">Resolver.</param>
        /// <param name="assembly">The assembly to scan for viewmodels.</param>
        /// <typeparam name="TBaseViewModelType">The base viewmodel type</typeparam>
        public static void RegisterViewModels<TBaseViewModelType>(this IMutableDependencyResolver resolver, Assembly assembly)
        {
            foreach (TypeInfo ti in assembly.DefinedTypes.Where(ti => !ti.IsAbstract && !ti.IsGenericTypeDefinition))
            {
                RegisterViewModel<TBaseViewModelType>(resolver, ti);
            }
        }

        /// <summary>
        /// Registers a view, if the criteria are met
        /// </summary>
        /// <param name="resolver">Resolver.</param>
        /// <param name="ti">Typeinfo for this view</param>
        /// <param name="registerDirectAlso">If set to <c>true</c>, this will register the view on the container as an implementation of TIViewFor.
        /// Otherwise, only second level implementations are registered. Eg, if TIViewFor is <c>IViewFor</c>, only <c>IViewFor&lt;object&gt;</c> is registered, not IViewFor self.
        /// Default is <c>false</c>.</param>
        /// <typeparam name="TIViewFor">The basetype for views.</typeparam>
        public static void RegisterView<TIViewFor>(this IMutableDependencyResolver resolver, TypeInfo ti, bool registerDirectAlso = false)
        {
            if (ti.ImplementedInterfaces.Contains(typeof(TIViewFor)))
            {
                // grab the first _implemented_ interface that also implements IViewFor, this should be the expected IViewFor<>
                foreach (Type ivf in ti.ImplementedInterfaces.Where(t =>
                    t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(TIViewFor))))
                {
                    RegisterType(resolver, ti, ivf);
                }
            }
        }

        public static void RegisterViewModel<TBaseViewModelType>(this IMutableDependencyResolver resolver, TypeInfo ti)
        {
            if (typeof(TBaseViewModelType).IsAssignableFrom(ti))
            {
                RegisterType(resolver, ti, ti);
                RegisterType(resolver, ti, typeof(TBaseViewModelType), ti.FullName);
                if (typeof(TBaseViewModelType).IsInterface)
                {
                    foreach (Type vmt in ti.ImplementedInterfaces.Where(t =>
                        t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(TBaseViewModelType))))
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

            Func<object> factory = TypeFactoryCreator(ti);
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

        public static Func<TypeInfo, Func<object>> TypeFactoryCreator { get; set; } =
            (TypeInfo typeInfo) =>
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
                                   return Locator.Current.GetServices(elementType).ToArrayOfType(elementType);
                               }

                               return Locator.Current.GetService(p.ParameterType);
                           })
                           .ToArray());
                   };
                return func;
            };
    }
}
