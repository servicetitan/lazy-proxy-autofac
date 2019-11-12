using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Core;
using Xunit;

[assembly: InternalsVisibleTo("LazyProxy.DynamicTypes")]

namespace LazyProxy.Autofac.Tests
{
    public class AutofacExtensionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void RegistrationMustThrowNotSupportedExceptionForNonInterfaces(string name) =>
            Assert.Throws<NotSupportedException>(() => new ContainerBuilder().RegisterLazy<Service1, Service1>(name));

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void ServiceCtorMustBeExecutedAfterMethodIsCalledForTheFirstTime(string name)
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);
            containerBuilder.RegisterType<Service2>().As<IService2>();

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IService1>()
                    : container.ResolveNamed<IService1>(name);

                Assert.Empty(_service1Id);
                Assert.Empty(_service2Id);

                var result1 = service.Method();

                Assert.Equal(Service1MethodValue, result1);
                Assert.NotEmpty(_service1Id);
                Assert.NotEmpty(_service2Id);

                var prevService1Id = _service1Id;
                var prevService2Id = _service2Id;

                var result2 = service.Method();

                Assert.Equal(Service1MethodValue, result2);
                Assert.Equal(prevService1Id, _service1Id);
                Assert.Equal(prevService2Id, _service2Id);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void ServiceCtorMustBeExecutedAfterPropertyGetterIsCalledForTheFirstTime(string name)
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);
            containerBuilder.RegisterType<Service2>().As<IService2>();

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IService1>()
                    : container.ResolveNamed<IService1>(name);

                Assert.Empty(_service1Id);
                Assert.Empty(_service2Id);

                var result1 = service.Property;

                Assert.Equal(Service1PropertyValue, result1);
                Assert.NotEmpty(_service1Id);
                Assert.NotEmpty(_service2Id);

                var prevService1Id = _service1Id;
                var prevService2Id = _service2Id;

                var result2 = service.Property;

                Assert.Equal(Service1PropertyValue, result2);
                Assert.Equal(prevService1Id, _service1Id);
                Assert.Equal(prevService2Id, _service2Id);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void ServiceCtorMustBeExecutedAfterPropertySetterIsCalledForTheFirstTime(string name)
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);
            containerBuilder.RegisterType<Service2>().As<IService2>();

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IService1>()
                    : container.ResolveNamed<IService1>(name);

                Assert.Empty(_service1Id);
                Assert.Empty(_service2Id);

                service.Property = "newValue1";

                Assert.NotEmpty(_service1Id);
                Assert.NotEmpty(_service2Id);

                var prevService1Id = _service1Id;
                var prevService2Id = _service2Id;

                service.Property = "newValue2";

                Assert.Equal(prevService1Id, _service1Id);
                Assert.Equal(prevService2Id, _service2Id);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void DependentLazyServicesMustNotBeCreatedUntilTheyMembersIsCalled(string name)
        {
            _service1Id = string.Empty;
            _service2Id = string.Empty;

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);
            containerBuilder.RegisterLazy<IService2, Service2>();

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IService1>()
                    : container.ResolveNamed<IService1>(name);

                Assert.Empty(_service1Id);
                Assert.Empty(_service2Id);

                var result1 = service.Method();

                Assert.Equal(Service1MethodValue, result1);
                Assert.NotEmpty(_service1Id);
                Assert.Empty(_service2Id); // Service2 has not been created yet.

                var prevService1Id = _service1Id;
                var result2 = service.Method(s => s.Method());

                Assert.Equal($"{Service1MethodValue}{Service2MethodValue}", result2);
                Assert.Equal(prevService1Id, _service1Id);
                Assert.NotEmpty(_service2Id);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void ArgumentsMustBePassedToMethodCorrectly(string name)
        {
            const string arg1 = nameof(arg1);
            const string arg2 = nameof(arg2);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);
            containerBuilder.RegisterLazy<IService2, Service2>();

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IService1>()
                    : container.ResolveNamed<IService1>(name);

                var result1 = service.Method(arg: arg1);
                Assert.Equal($"{Service1MethodValue}{arg1}", result1);

                var result2 = service.Method(s => s.Method(arg2), arg1);
                Assert.Equal($"{Service1MethodValue}{Service2MethodValue}{arg2}{arg1}", result2);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void DependencyResolutionExceptionMustBeThrownAfterMethodIsCalledWhenDependentServiceIsNotRegistered(
            string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IService1>()
                    : container.ResolveNamed<IService1>(name);

                Assert.Throws<DependencyResolutionException>(() => service.Method());

                using (var scope = container.BeginLifetimeScope())
                {
                    service = name == null
                        ? scope.Resolve<IService1>()
                        : scope.ResolveNamed<IService1>(name);

                    Assert.Throws<DependencyResolutionException>(() => service.Method());
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void ServiceMustBeResolvedInScopeHavingRegisteredDependentService(string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IService1, Service1>(name);

            using (var container = containerBuilder.Build())
            {
                using (var scope = container.BeginLifetimeScope(b => b.RegisterType<Service2>().As<IService2>()))
                {
                    var service = name == null
                        ? scope.Resolve<IService1>()
                        : scope.ResolveNamed<IService1>(name);

                    // Must do not throw
                    service.Method();
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void InternalServiceMustBeResolvedFromAssemblyMarkedAsVisibleForProxy(string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy<IInternalService, InternalService>(name);

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IInternalService>()
                    : container.ResolveNamed<IInternalService>(name);

                var result = service.Method();

                Assert.Equal(InternalServiceMethodValue, result);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void OverridesMustBeAppliedByProxy(string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<Service2>().As<IService2>();
            containerBuilder.RegisterLazy<IServiceToTestOverrides, ServiceToTestOverrides>(name);

            using (var container = containerBuilder.Build())
            {
                const ServiceToTestOverrides.SomeEnum enumValue = ServiceToTestOverrides.SomeEnum.Value2;
                const string stringValue = nameof(stringValue);
                const string arg = nameof(arg);

                var parameters = new Parameter[]
                {
                    new NamedParameter("someEnum", enumValue),
                    new ResolvedParameter(
                        (p, c) => p.ParameterType == typeof(IService2),
                        (p, c) => new OtherService2()),
                    new TypedParameter(
                        typeof(ServiceToTestOverrides.Argument),
                        new ServiceToTestOverrides.Argument(stringValue))
                };

                var service = name == null
                    ? container.Resolve<IServiceToTestOverrides>(parameters)
                    : container.ResolveNamed<IServiceToTestOverrides>(name, parameters);

                var result = service.Method(arg);

                Assert.Equal($"{enumValue}_{Service2ExMethodValue}{arg}_{stringValue}", result);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void ClosedGenericServiceMustBeResolved(string name)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterLazy(
                typeof(IGenericService<ParameterType1, ParameterType2, ParameterType3>),
                typeof(GenericService<ParameterType1, ParameterType2, ParameterType3>), name);

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IGenericService<ParameterType1, ParameterType2, ParameterType3>>()
                    : container.ResolveNamed<IGenericService<ParameterType1, ParameterType2, ParameterType3>>(name);

                var result = service.Get(new ParameterType1(), new ParameterType2(), 42);

                Assert.Equal(
                    $"{typeof(ParameterType1).Name}_" +
                    $"{typeof(ParameterType2).Name}_" +
                    $"{typeof(int).Name}",
                    result.Value);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void OpenGenericServiceMustBeResolved(string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy(typeof(IGenericService<,,>), typeof(GenericService<,,>), name);

            using (var container = containerBuilder.Build())
            {
                var service = name == null
                    ? container.Resolve<IGenericService<ParameterType1, ParameterType2, ParameterType3>>()
                    : container.ResolveNamed<IGenericService<ParameterType1, ParameterType2, ParameterType3>>(name);

                var result = service.Get(new ParameterType1(), new ParameterType2(), 42);

                Assert.Equal(
                    $"{typeof(ParameterType1).Name}_" +
                    $"{typeof(ParameterType2).Name}_" +
                    $"{typeof(int).Name}",
                    result.Value);
            }
        }

        #region Lifetime tests

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void SingleInstanceServiceLifetimeMustBeActual(string name) =>
            AssertSingleInstanceServiceLifetimeMustBeActual<IService>(typeof(IService), typeof(Service), name);

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void SingleInstanceServiceLifetimeMustBeActualForOpenGenericService(string name) =>
            AssertSingleInstanceServiceLifetimeMustBeActual<
                IGenericService<ParameterType1, ParameterType2, ParameterType3>>(
                typeof(IGenericService<,,>), typeof(GenericService<,,>), name);

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void InstancePerLifetimeScopeServiceLifetimeMustBeActual(string name) =>
            AssertInstancePerLifetimeScopeServiceLifetimeMustBeActual<IService>(
                typeof(IService), typeof(Service), name);

        [Theory]
        [InlineData(null)]
        [InlineData("name")]
        public void InstancePerLifetimeScopeServiceLifetimeMustBeActualForOpenGenericService(string name)
        {
            AssertInstancePerLifetimeScopeServiceLifetimeMustBeActual<
                IGenericService<ParameterType1, ParameterType2, ParameterType3>>(
                typeof(IGenericService<,,>), typeof(GenericService<,,>), name);
        }

        [Theory]
        [InlineData(ServiceLifetime.Unknown, null)]
        [InlineData(ServiceLifetime.Unknown, "name")]
        [InlineData(ServiceLifetime.InstancePerDependency, null)]
        [InlineData(ServiceLifetime.InstancePerDependency, "name")]
        public void InstancePerDependencyServiceLifetimeMustBeActual(ServiceLifetime lifetime, string name) =>
            AssertInstancePerDependencyServiceLifetimeMustBeActual<IService>(
                typeof(IService), typeof(Service), lifetime, name);

        [Theory]
        [InlineData(ServiceLifetime.Unknown, null)]
        [InlineData(ServiceLifetime.Unknown, "name")]
        [InlineData(ServiceLifetime.InstancePerDependency, null)]
        [InlineData(ServiceLifetime.InstancePerDependency, "name")]
        public void InstancePerDependencyServiceLifetimeMustBeActualForOpenGenericService(
            ServiceLifetime lifetime, string name)
        {
            AssertInstancePerDependencyServiceLifetimeMustBeActual<
                IGenericService<ParameterType1, ParameterType2, ParameterType3>>(
                typeof(IGenericService<,,>), typeof(GenericService<,,>), lifetime, name);
        }

        #endregion

        #region Private members

        [ThreadStatic]
        private static string _service1Id;

        [ThreadStatic]
        private static string _service2Id;

        private const string Service1PropertyValue = nameof(Service1PropertyValue);
        private const string Service1MethodValue = nameof(Service1MethodValue);
        private const string Service2MethodValue = nameof(Service2MethodValue);
        private const string Service2ExMethodValue = nameof(Service2ExMethodValue);
        private const string InternalServiceMethodValue = nameof(InternalServiceMethodValue);

        #endregion

        #region Private members for lifetime testing

        private class Resolves : IEnumerable<Guid>
        {
            public SingleContainerResolves RootContainerResolves { get; set; }
            public SingleContainerResolves Scope1Resolves { get; set; }
            public SingleContainerResolves Scope2Resolves { get; set; }

            public IEnumerator<Guid> GetEnumerator() =>
                RootContainerResolves
                    .Concat(Scope1Resolves)
                    .Concat(Scope2Resolves)
                    .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class SingleContainerResolves : IEnumerable<Guid>
        {
            public Guid Resolve1 { get; set; }
            public Guid Resolve2 { get; set; }

            public IEnumerator<Guid> GetEnumerator() => All().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<Guid> All()
            {
                yield return Resolve1;
                yield return Resolve2;
            }
        }

        private static void AssertSingleInstanceServiceLifetimeMustBeActual<T>(Type typeFrom, Type typeTo, string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy(typeFrom, typeTo, name, ServiceLifetime.SingleInstance);

            using (var container = containerBuilder.Build()) {
                var resolves = GetResolves<T>(container, name);
                var rootContainerResolves = resolves.RootContainerResolves;
                var childContainer1Resolves = resolves.Scope1Resolves;
                var childContainer2Resolves = resolves.Scope2Resolves;

                AssertAllInstancesAreEqual(
                    rootContainerResolves.Resolve1,
                    rootContainerResolves.Resolve2,
                    childContainer1Resolves.Resolve1,
                    childContainer1Resolves.Resolve2,
                    childContainer2Resolves.Resolve1,
                    childContainer2Resolves.Resolve2
                );
            }
        }

        private static void AssertInstancePerLifetimeScopeServiceLifetimeMustBeActual<T>(
            Type typeFrom, Type typeTo, string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy(typeFrom, typeTo, name, ServiceLifetime.InstancePerLifetimeScope);

            using (var container = containerBuilder.Build()) {
                var resolves = GetResolves<T>(container, name);
                var rootContainerResolves = resolves.RootContainerResolves;
                var childContainer1Resolves = resolves.Scope1Resolves;
                var childContainer2Resolves = resolves.Scope2Resolves;

                AssertAllInstancesAreDifferent(
                    rootContainerResolves.Resolve1,
                    childContainer1Resolves.Resolve1,
                    childContainer2Resolves.Resolve1
                );

                Assert.Equal(
                    rootContainerResolves.Resolve1,
                    rootContainerResolves.Resolve2);

                Assert.Equal(
                    childContainer1Resolves.Resolve1,
                    childContainer1Resolves.Resolve2);

                Assert.Equal(
                    childContainer2Resolves.Resolve1,
                    childContainer2Resolves.Resolve2);
            }
        }

        private static void AssertInstancePerDependencyServiceLifetimeMustBeActual<T>(
            Type typeFrom, Type typeTo, ServiceLifetime lifetime, string name)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterLazy(typeFrom, typeTo, name, lifetime);

            using (var container = containerBuilder.Build()) {
                var resolves = GetResolves<T>(container, name)
                    .Where(id => id != Guid.Empty)
                    .ToArray();

                AssertAllInstancesAreDifferent(resolves);
            }
        }

        private static Resolves GetResolves<T>(ILifetimeScope container, string name) =>
            new Resolves {
                RootContainerResolves = GetContainerResolves<T>(container, name),
                Scope1Resolves = GetScopeResolves<T>(container, name),
                Scope2Resolves = GetScopeResolves<T>(container, name)
            };

        private static SingleContainerResolves GetContainerResolves<T>(IComponentContext container, string name) =>
            new SingleContainerResolves {
                Resolve1 = GetResolvedId<T>(container, name),
                Resolve2 = GetResolvedId<T>(container, name),
            };

        private static SingleContainerResolves GetScopeResolves<T>(ILifetimeScope container, string name)
        {
            using (var scope = container.BeginLifetimeScope()) {
                return GetContainerResolves<T>(scope, name);
            }
        }

        private static Guid GetResolvedId<T>(IComponentContext container, string name) =>
            name == null
                ? ((IHasId) container.Resolve(typeof(T))).Id
                : ((IHasId) container.ResolveNamed(name, typeof(T))).Id;

        private static void AssertAllInstancesAreEqual(params Guid[] keys) =>
            Assert.True(keys.Distinct().Count() == 1, "All instances must be the same.");

        private static void AssertAllInstancesAreDifferent(params Guid[] keys) =>
            Assert.True(keys.Distinct().Count() == keys.Length, "All instances must be different.");

        #endregion

        #region Test classes

        public interface IHasId
        {
            Guid Id { get; }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IService : IHasId { }

        // ReSharper disable once MemberCanBePrivate.Global
        public class Service : IService
        {
            public Guid Id { get; } = Guid.NewGuid();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IService1
        {
            string Property { get; set; }
            string Method(Func<IService2, string> getDependentServiceValue = null, string arg = null);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Service1 : IService1
        {
            private readonly IService2 _otherService;

            public Service1(IService2 otherService)
            {
                _service1Id = Guid.NewGuid().ToString();
                _otherService = otherService;
            }

            public string Property { get; set; } = Service1PropertyValue;

            public string Method(Func<IService2, string> getDependentServiceValue, string arg) =>
                $"{Service1MethodValue}" +
                $"{(getDependentServiceValue == null ? string.Empty : getDependentServiceValue(_otherService))}" +
                $"{arg ?? string.Empty}";
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IService2
        {
            string Method(string arg = null);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Service2 : IService2
        {
            public Service2()
            {
                _service2Id = Guid.NewGuid().ToString();
            }

            public string Method(string arg) => $"{Service2MethodValue}{arg ?? string.Empty}";
        }

        private class OtherService2 : IService2
        {
            public string Method(string arg) => $"{Service2ExMethodValue}{arg ?? string.Empty}";
        }

        // ReSharper disable once MemberCanBePrivate.Global
        internal interface IInternalService
        {
            string Method(string arg = null);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once ClassNeverInstantiated.Global
        internal class InternalService : IInternalService
        {
            public string Method(string arg) => $"{InternalServiceMethodValue}{arg ?? string.Empty}";
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IServiceToTestOverrides
        {
            string Method(string arg);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ServiceToTestOverrides : IServiceToTestOverrides
        {
            // ReSharper disable once MemberCanBePrivate.Global
            public class Argument
            {
                public string Value { get; }

                public Argument(string value)
                {
                    Value = value;
                }
            }

            public enum SomeEnum
            {
                // ReSharper disable once UnusedMember.Local
                Value1,
                Value2
            }

            private readonly SomeEnum _someEnum;
            private readonly IService2 _service;
            private readonly Argument _argument;

            public string Method(string arg) => $"{_someEnum}_{_service.Method(arg)}_{_argument.Value}";

            public ServiceToTestOverrides(SomeEnum someEnum, IService2 service, Argument argument)
            {
                _someEnum = someEnum;
                _service = service;
                _argument = argument;
            }
        }

        public interface IParameterType { }

        public abstract class ParameterTypeBase
        {
            public string Value { get; set; }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public class ParameterType1 : IParameterType { }

        // ReSharper disable once MemberCanBePrivate.Global
        public struct ParameterType2 { }

        // ReSharper disable once MemberCanBePrivate.Global
        public class ParameterType3 : ParameterTypeBase, IParameterType { }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once TypeParameterCanBeVariant
        public interface IGenericService<T, in TIn, out TOut> : IHasId
            where T : class, IParameterType, new()
            where TIn : struct
            where TOut : ParameterTypeBase, IParameterType
        {
            TOut Get<TArg>(T arg1, TIn arg2, TArg arg3) where TArg : struct;
        }

        // ReSharper disable once ClassWithVirtualMembersNeverInherited.Local
        private class GenericService<T, TIn, TOut> : IGenericService<T, TIn, TOut>
            where T : class, IParameterType, new()
            where TIn : struct
            where TOut : ParameterTypeBase, IParameterType, new()
        {
            protected virtual string Get() => "";
            public Guid Id { get; }

            public TOut Get<TArg>(T arg1, TIn arg2, TArg arg3) where TArg : struct
            {
                return new TOut
                {
                    Value = $"{arg1.GetType().Name}_{arg2.GetType().Name}_{arg3.GetType().Name}{Get()}"
                };
            }

            public GenericService()
            {
                Id = Guid.NewGuid();
            }
        }

        #endregion
    }
}
