# Lazy Dependency Injection for Autofac Container

A [LazyProxy](https://github.com/servicetitan/lazy-proxy) can be used for IoC containers to improve performance by changing the resolve behavior.

More info can be found in the article about [Lazy Dependency Injection for .NET](https://dev.to/hypercodeplace/lazy-dependency-injection-37en).

## Get Packages

The library provides in NuGet.

```
Install-Package LazyProxy.Autofac
```

## Get Started

Consider the following service:

```CSharp
public interface IMyService
{
    void Foo();
}

public class MyService : IMyService
{
    public MyService() => Console.WriteLine("Ctor");
    public void Foo() => Console.WriteLine("Foo");
}
```

A lazy registration for this service can be added like this:

```CSharp
// Creating a container builder
var containerBuilder = new ContainerBuilder();

// Adding a lazy registration
containerBuilder.RegisterLazy<IMyService, MyService>();

// Building a container
using var container = containerBuilder.Build();

Console.WriteLine("Resolving the service...");
var service = container.Resolve<IMyService>();

Console.WriteLine("Executing the 'Foo' method...");
service.Foo();
```

The output for this example:

```
Resolving the service...
Executing the 'Foo' method...
Ctor
Foo
```

## License

This project is licensed under the Apache License, Version 2.0. - see the [LICENSE](https://github.com/servicetitan/lazy-proxy-Autofac/blob/master/LICENSE) file for details.