using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;

namespace LazyProxy.Autofac
{
    /// <summary>
    /// Generates registrations for open generic factories.
    /// </summary>
    public sealed class OpenGenericFactoryRegistrationSource : IRegistrationSource
    {
        private readonly RegistrationData _registrationData;
        private readonly SimpleActivatorData _activatorData;

        /// <summary>
        /// Name of the key to get the closed generic type from the named parameters.
        /// </summary>
        public const string ServiceType = "ServiceType";

        /// <inheritdoc />
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>
        /// Creates a new instance of <see cref="OpenGenericFactoryRegistrationSource"/>.
        /// </summary>
        /// <param name="registrationData">Registration data.</param>
        /// <param name="activatorData">Activator data.</param>
        public OpenGenericFactoryRegistrationSource(
            RegistrationData registrationData,
            SimpleActivatorData activatorData)
        {
            _registrationData = registrationData ?? throw new ArgumentNullException(nameof(registrationData));
            _activatorData = activatorData ?? throw new ArgumentNullException(nameof(activatorData));
        }

        /// <inheritdoc />
        public IEnumerable<IComponentRegistration> RegistrationsFor(
            Service service, Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (registrationAccessor == null)
            {
                throw new ArgumentNullException(nameof(registrationAccessor));
            }

            if (!(service is IServiceWithType swt) || !swt.ServiceType.GetTypeInfo().IsGenericType)
            {
                return Enumerable.Empty<IComponentRegistration>();
            }

            var definitionService = (IServiceWithType) swt.ChangeType(swt.ServiceType.GetGenericTypeDefinition());

            if (!_registrationData.Services.Cast<IServiceWithType>().Any(s => s.Equals(definitionService)))
            {
                return Enumerable.Empty<IComponentRegistration>();
            }

            return new[]
            {
                new ComponentRegistration(
                    Guid.NewGuid(),
                    new DelegateActivator(swt.ServiceType, (c, parameters) =>
                    {
                        var activator = (DelegateActivator) _activatorData.Activator;
                        var newParameters = parameters.Concat(new[]
                        {
                            new NamedParameter(ServiceType, swt.ServiceType)
                        });

                        return activator.ActivateInstance(c, newParameters);
                    }),
                    GetLifetime(_registrationData.Lifetime),
                    _registrationData.Sharing,
                    _registrationData.Ownership,
                    new[] {service},
                    new Dictionary<string, object>())
            };
        }

        private static IComponentLifetime GetLifetime(IComponentLifetime lifetime)
        {
            if (lifetime.GetType() == typeof(CurrentScopeLifetime))
            {
                return new CurrentScopeLifetime();
            }

            if (lifetime.GetType() == typeof(RootScopeLifetime))
            {
                return new RootScopeLifetime();
            }

            throw new NotSupportedException($"Lifetime scope '${lifetime.GetType().Name}' is not supported.");
        }
    }
}
