using Autofac.Builder;

namespace LazyProxy.Autofac
{
    /// <summary>
    /// A mutator allowing to change a registration.
    /// </summary>
    public interface IRegistrationMutator
    {
        /// <summary>
        /// Changes or adjusts the registration status.
        /// </summary>
        /// <param name="registration">A container registration.</param>
        /// <typeparam name="TLimit">The most specific type to which instances of the registration
        /// can be cast.</typeparam>
        /// <typeparam name="TActivatorData">Activator builder type.</typeparam>
        /// <typeparam name="TRegistrationStyle">Registration style type.</typeparam>
        /// <returns></returns>
        IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle>
            Mutate<TLimit, TActivatorData, TRegistrationStyle>(
                IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration);
    }
}