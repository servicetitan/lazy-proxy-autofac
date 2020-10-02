using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.OpenGenerics;

namespace LazyProxy.Autofac
{
    /// <summary>
    /// Extension methods for lazy registration.
    /// </summary>
    public static class AutofacExtensions
    {
        /// <summary>
        /// Is used to register non open generic interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name. Null if named registration is not required.</param>
        /// <param name="nonLazyRegistrationMutator">A mutator allowing to change the non-lazy registration.</param>
        /// <typeparam name="TFrom">The linked interface.</typeparam>
        /// <typeparam name="TTo">The linked class.</typeparam>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterLazy<TFrom, TTo>(this ContainerBuilder builder, string name = null,
                IRegistrationMutator nonLazyRegistrationMutator = null)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy(typeof(TFrom), typeof(TTo), name, nonLazyRegistrationMutator);

        /// <summary>
        /// Is used to register non open generic interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="typeFrom">The linked interface.</param>
        /// <param name="typeTo">The linked class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name. Null if named registration is not required.</param>
        /// <param name="nonLazyRegistrationMutator">A mutator allowing to change the non-lazy registration.</param>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterLazy(this ContainerBuilder builder, Type typeFrom, Type typeTo, string name = null,
                IRegistrationMutator nonLazyRegistrationMutator = null)
        {
            // There is no way to constraint it on the compilation step.
            if (!typeFrom.IsInterface)
            {
                throw new NotSupportedException(
                    "The lazy registration is supported only for interfaces.");
            }

            if (typeTo.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"{typeFrom} is an open generic type definition. Use the 'RegisterGenericLazy' method instead.");
            }

            var registrationName = Guid.NewGuid().ToString();
            var nonLazyRegistration = builder.RegisterType(typeTo).Named(registrationName, typeFrom);
            nonLazyRegistrationMutator?.Mutate(nonLazyRegistration);

            var registration = builder.Register((c, p) =>
                CreateLazyProxy(c.Resolve<IComponentContext>(), typeFrom, registrationName, p));

            return name == null
                ? registration.As(typeFrom)
                : registration.Named(name, typeFrom);
        }

        /// <summary>
        /// Is used to register open generic interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="typeFrom">The linked interface.</param>
        /// <param name="typeTo">The linked class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name. Null if named registration is not required.</param>
        /// <param name="nonLazyRegistrationMutator">A mutator allowing to change the non-lazy registration.</param>
        /// <returns>The instance of the Autofac registration builder.</returns>
        public static IRegistrationBuilder<object, OpenGenericDelegateActivatorData, DynamicRegistrationStyle>
            RegisterGenericLazy(this ContainerBuilder builder, Type typeFrom, Type typeTo, string name = null,
                IRegistrationMutator nonLazyRegistrationMutator = null)
        {
            // There is no way to constraint it on the compilation step.
            if (!typeFrom.IsInterface)
            {
                throw new NotSupportedException(
                    "The lazy registration is supported only for interfaces.");
            }

            if (!typeTo.IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    $"{typeFrom} is not an open generic type definition. Use the 'RegisterLazy' method instead.");
            }

            var registrationName = Guid.NewGuid().ToString();
            var nonLazyRegistration = builder.RegisterGeneric(typeTo).Named(registrationName, typeFrom);
            nonLazyRegistrationMutator?.Mutate(nonLazyRegistration);

            var registration = builder.RegisterGeneric((c, t, p) =>
            {
                var closedTypeFrom = typeFrom.MakeGenericType(t);
                return CreateLazyProxy(c.Resolve<IComponentContext>(), closedTypeFrom, registrationName, p);
            });

            return name == null
                ? registration.As(typeFrom)
                : registration.Named(name, typeFrom);
        }

        private static object CreateLazyProxy(
            IComponentContext context, Type type, string name, IEnumerable<Parameter> parameters) =>
            LazyProxyBuilder.CreateInstance(type,
                () => context.ResolveNamed(name, type, parameters)
            );
    }
}
