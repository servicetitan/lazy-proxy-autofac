using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace LazyProxy.Autofac
{
    /// <summary>
    /// Extension methods for lazy registration.
    /// </summary>
    public static class AutofacExtensions
    {
        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <typeparam name="TFrom">The linked interface.</typeparam>
        /// <typeparam name="TTo">The linked class.</typeparam>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterLazy<TFrom, TTo>(this ContainerBuilder builder)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy(typeof(TFrom), typeof(TTo), null);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name. Null if named registration is not required.</param>
        /// <typeparam name="TFrom">The linked interface.</typeparam>
        /// <typeparam name="TTo">The linked class.</typeparam>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterLazy<TFrom, TTo>(this ContainerBuilder builder, string name)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy(typeof(TFrom), typeof(TTo), name);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method execution.
        /// </summary>
        /// <param name="typeFrom">The linked interface.</param>
        /// <param name="typeTo">The linked class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterLazy(this ContainerBuilder builder, Type typeFrom, Type typeTo) =>
            builder.RegisterLazy(typeFrom, typeTo, null);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="typeFrom">The linked interface.</param>
        /// <param name="typeTo">The linked class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name. Null if named registration is not required.</param>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterLazy(this ContainerBuilder builder, Type typeFrom, Type typeTo, string name)
        {
            // There is no way to constraint it on the compilation step.
            if (!typeFrom.IsInterface)
            {
                throw new NotSupportedException("The lazy registration is supported only for interfaces.");
            }

            builder.RegisterSource<OpenGenericFactoryRegistrationSource>();

            var registrationName = Guid.NewGuid().ToString();

            if (typeTo.IsGenericTypeDefinition)
            {
                builder.RegisterGeneric(typeTo).Named(registrationName, typeFrom);
            }
            else
            {
                builder.RegisterType(typeTo).Named(registrationName, typeFrom);
            }

            var registration = builder.Register((c, p) =>
            {
                var parameters = p.ToList();
                var context = c.Resolve<IComponentContext>();
                var serviceType = GetServiceType(typeFrom, parameters);

                return LazyProxyBuilder.CreateInstance(serviceType,
                    () => context.ResolveNamed(registrationName, serviceType, parameters)
                );
            });

            return name == null
                ? registration.As(typeFrom)
                : registration.Named(name, typeFrom);
        }

        private static Type GetServiceType(Type type, IEnumerable<Parameter> parameters) =>
            type.IsGenericType && !type.IsConstructedGenericType
                ? parameters.Named<Type>(OpenGenericFactoryRegistrationSource.ServiceType)
                : type;
    }
}