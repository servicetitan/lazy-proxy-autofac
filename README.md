# Lazy injection for Autofac container

A [LazyProxy](https://github.com/servicetitan/lazy-proxy) can be used for IoC containers to change the resolving behaviour.

Dependencies registered as lazy are created as dynamic proxy objects built in real time, but the real classes are resolved only after the first execution of proxy method or property.

Also dynamic lazy proxy allows injection of circular dependencies.

```C#
var containerBuilder = new ContainerBuilder();
containerBuilder.RegisterLazy<IFoo, Foo>();
var container = containerBuilder.Build();

Console.WriteLine("Resolving service...");
var foo = container.Resolve<IFoo>();

Console.WriteLine("Bar execution...");
foo.Bar();

// Resolving service...
// Bar execution...
// Hello from ctor
// Hello from Bar

```

## License

This project is licensed under the Apache License, Version 2.0. - see the [LICENSE](https://github.com/servicetitan/lazy-proxy-unity/blob/master/LICENSE) file for details.