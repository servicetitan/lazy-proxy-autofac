using System;
using Autofac;

namespace LazyProxy.Autofac
{
    /// <summary>
    /// Extension methods for lazy registration.
    /// </summary>
    public static class AutofacExtensions
    {
        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <typeparam name="TFrom">The binded interface.</typeparam>
        /// <typeparam name="TTo">The binded class.</typeparam>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy<TFrom, TTo>(this ContainerBuilder builder)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy<TFrom, TTo>(null, ServiceLifetime.InstancePerDependency);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name.</param>
        /// <typeparam name="TFrom">The binded interface.</typeparam>
        /// <typeparam name="TTo">The binded class.</typeparam>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy<TFrom, TTo>(this ContainerBuilder builder, string name)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy<TFrom, TTo>(name, ServiceLifetime.InstancePerDependency);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="serviceLifetime">The instance lifetime.</param>
        /// <typeparam name="TFrom">The binded interface.</typeparam>
        /// <typeparam name="TTo">The binded class.</typeparam>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy<TFrom, TTo>(
            this ContainerBuilder builder, ServiceLifetime serviceLifetime)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy<TFrom, TTo>(null, serviceLifetime);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name.</param>
        /// <param name="serviceLifetime">The instance lifetime.</param>
        /// <typeparam name="TFrom">The binded interface.</typeparam>
        /// <typeparam name="TTo">The binded class.</typeparam>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy<TFrom, TTo>(
            this ContainerBuilder builder, string name, ServiceLifetime serviceLifetime)
            where TTo : TFrom where TFrom : class =>
            builder.RegisterLazy(typeof(TFrom), typeof(TTo), name, serviceLifetime);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method execution.
        /// </summary>
        /// <param name="typeFrom">The binded interface.</param>
        /// <param name="typeTo">The binded class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy(
            this ContainerBuilder builder, Type typeFrom, Type typeTo) =>
            builder.RegisterLazy(typeFrom, typeTo, null, ServiceLifetime.InstancePerDependency);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="typeFrom">The binded interface.</param>
        /// <param name="typeTo">The binded class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name.</param>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy(
            this ContainerBuilder builder, Type typeFrom, Type typeTo, string name) =>
            builder.RegisterLazy(typeFrom, typeTo, name, ServiceLifetime.InstancePerDependency);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="typeFrom">The binded interface.</param>
        /// <param name="typeTo">The binded class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="serviceLifetime">The instance lifetime.</param>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy(
            this ContainerBuilder builder, Type typeFrom, Type typeTo, ServiceLifetime serviceLifetime) =>
            builder.RegisterLazy(typeFrom, typeTo, null, serviceLifetime);

        /// <summary>
        /// Is used to register interface TFrom to class TTo by creation a lazy proxy at runtime.
        /// The real class To will be instantiated only after first method or property execution.
        /// </summary>
        /// <param name="typeFrom">The binded interface.</param>
        /// <param name="typeTo">The binded class.</param>
        /// <param name="builder">The instance of the Autofac container builder.</param>
        /// <param name="name">The registration name.</param>
        /// <param name="serviceLifetime">The instance lifetime.</param>
        /// <returns>The instance of the Autofac container builder.</returns>
        public static ContainerBuilder RegisterLazy(
            this ContainerBuilder builder, Type typeFrom, Type typeTo, string name, ServiceLifetime serviceLifetime)
        {
            // There is no way to constraint it on the compilation step.
            if (!typeFrom.IsInterface)
            {
                throw new NotSupportedException("The lazy registration is supported only for interfaces.");
            }

            var registrationName = Guid.NewGuid().ToString();

            builder.RegisterType(typeTo).Named(registrationName, typeFrom);

            var registration = builder.Register((c, p) =>
            {
                var context = c.Resolve<IComponentContext>();

                return LazyProxyBuilder.CreateInstance(typeFrom,
                    () => context.ResolveNamed(registrationName, typeFrom, p)
                );
            });

//            Variant 2 (Overrides dont work
//            builder.RegisterType(typeTo).Named(registrationName, typeTo);
//
//            var funcType = typeof(Func<>);
//            var factoryType = funcType.MakeGenericType(typeTo);
//
//            var registration = builder.Register((c, p) =>
//                LazyProxyBuilder.CreateInstance(typeFrom, (Func<object>)c.ResolveNamed(registrationName, factoryType)));

            if (name == null)
            {
                registration.As(typeFrom);
            }
            else
            {
                registration.Named(name, typeFrom);
            }

            switch (serviceLifetime)
            {
                case ServiceLifetime.Unknown:
                case ServiceLifetime.InstancePerDependency:
                    registration.InstancePerDependency().ExternallyOwned();
                    break;
                case ServiceLifetime.SingleInstance:
                    registration.SingleInstance();
                    break;
                case ServiceLifetime.InstancePerLifetimeScope:
                    registration.InstancePerLifetimeScope();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }

            return builder;
        }
    }

    /// <summary>
    /// Service lifetime provided by the IoC container.
    /// </summary>
    public enum ServiceLifetime
    {
        Unknown = 0,
        SingleInstance = 1,
        InstancePerLifetimeScope = 2,
        InstancePerDependency = 3
    }
}
