using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;

namespace LazyProxy.Autofac
{
    /// <summary>
    /// Extension methods for lazy registration.
    /// </summary>
    public static class AutofacExtensions
    {
        private static readonly ConstructorInfo RegistrationBuilderConstructor;

        static AutofacExtensions()
        {
            // There is no way to create RegistrationBuilder with TypedService / KeyedService without reflection.
            RegistrationBuilderConstructor = typeof(ILifetimeScope).Assembly
                .GetType("Autofac.Builder.RegistrationBuilder`3")
                .MakeGenericType(
                    typeof(object),
                    typeof(SimpleActivatorData),
                    typeof(SingleRegistrationStyle))
                .GetConstructor(new[]
                {
                    typeof(Service),
                    typeof(SimpleActivatorData),
                    typeof(SingleRegistrationStyle)
                });
        }

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
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
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
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
                throw new NotSupportedException("The lazy registration is supported only for interfaces.");
            }

            var registrationName = Guid.NewGuid().ToString();
            IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> registration;

            if (typeTo.IsGenericTypeDefinition)
            {
                var nonLazyRegistration = builder.RegisterGeneric(typeTo).Named(registrationName, typeFrom);
                nonLazyRegistrationMutator?.Mutate(nonLazyRegistration);

                registration = builder.RegisterGenericFactory(typeFrom, name,
                    (c, t, n, p) => CreateLazyProxy(c, t, registrationName, p));
            }
            else
            {
                var nonLazyRegistration = builder.RegisterType(typeTo).Named(registrationName, typeFrom);
                nonLazyRegistrationMutator?.Mutate(nonLazyRegistration);

                registration = builder.Register(
                    (c, p) => CreateLazyProxy(c.Resolve<IComponentContext>(), typeFrom, registrationName, p));
            }

            return name == null
                ? registration.As(typeFrom)
                : registration.Named(name, typeFrom);
        }

        /// <summary>
        /// Registers a delegate as a component for open generic types.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="type"><see cref="Type"/> of the registered component.</param>
        /// <param name="name">Name of the registered component.</param>
        /// <param name="factory">The delegate to register.</param>
        /// <returns>Registration builder allowing the registration to be configured.</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>
            RegisterGenericFactory(this ContainerBuilder builder, Type type, string name,
                Func<IComponentContext, Type, string, Parameter[], object> factory)
        {
            var registration = (IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle>)
                RegistrationBuilderConstructor.Invoke(new[]
                {
                    string.IsNullOrEmpty(name)
                        ? (object) new TypedService(type)
                        : new KeyedService(name, type),

                    new SimpleActivatorData(new DelegateActivator(type,
                        (c, p) =>
                        {
                            var parameters = p.ToArray();
                            var serviceType = parameters.Named<Type>(OpenGenericFactoryRegistrationSource.ServiceType);
                            var context = c.Resolve<IComponentContext>();

                            return factory(context, serviceType, name, parameters);
                        })),

                    new SingleRegistrationStyle()
                });

            registration.RegistrationData.DeferredCallback = builder.RegisterCallback(
                cr => cr.AddRegistrationSource(
                    new OpenGenericFactoryRegistrationSource(
                        registration.RegistrationData,
                        registration.ActivatorData)));

            return registration;
        }

        private static object CreateLazyProxy(
            IComponentContext context, Type type, string name, IEnumerable<Parameter> parameters) =>
            LazyProxyBuilder.CreateInstance(type,
                () => context.ResolveNamed(name, type, parameters)
            );
    }
}
