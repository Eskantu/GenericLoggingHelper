# GenericLoggingHelper

GenericLoggingHelper is a small .NET 8 library that wraps interface-based services with a dynamic proxy so you can centralize exception handling and logging without repeating `try/catch` blocks across your codebase.

It integrates with `Microsoft.Extensions.DependencyInjection`, lets you define a global exception handler, and supports per-service overrides when you need custom behavior.

## Features

- Dynamic proxy generation for services registered by interface
- Centralized exception handling through a configurable callback
- Global logging configuration with optional per-service overrides
- Integration with `Microsoft.Extensions.DependencyInjection`
- Default fallback behavior when no `ILogger` is available
- Packaged as a reusable NuGet library

## How It Works

The library registers your concrete implementation in DI and returns a proxied version of the requested interface.

When a method is invoked through the proxy:

1. The real implementation is called.
2. If execution succeeds, the result flows back normally.
3. If an exception is thrown during the invocation, the configured `OnException` callback is executed.
4. The original exception is rethrown.

This gives you one place to define how exceptions should be logged or handled for multiple services.

## Installation

Add the package to your project once it is published to your NuGet feed:

```bash
dotnet add package GenericLoggingHelper
```

If you are consuming it from a private feed, configure that feed in your NuGet settings first.

## Quick Start

### 1. Define a service contract

```csharp
public interface IOrderService
{
    void Process();
}

public class OrderService : IOrderService
{
    public void Process()
    {
        throw new InvalidOperationException("Order processing failed");
    }
}
```

### 2. Register GenericLoggingHelper

```csharp
using Microsoft.Extensions.DependencyInjection;
using Eskantu.Logging;

var services = new ServiceCollection();

services.AddLogging();

services.AddGenericLoggingProxy(options =>
{
    options.OnException = (exception, method, args) =>
    {
        Console.WriteLine($"[Global] {method.Name} failed: {exception.Message}");
    };
});

services.AddWithLogging<IOrderService, OrderService>();
```

### 3. Resolve and use the service

```csharp
var provider = services.BuildServiceProvider();
var service = provider.GetRequiredService<IOrderService>();

service.Process();
```

When `Process()` throws, the proxy intercepts the exception, executes the configured callback, and then rethrows the exception.

## Per-Service Override

You can override the global behavior for a specific registration:

```csharp
services.AddWithLogging<IOrderService, OrderService>(options =>
{
    options.OnException = (exception, method, args) =>
    {
        Console.WriteLine($"[OrderService] {method.Name} failed with custom handling");
    };
});
```

This is useful when one service requires more detailed logging or a different exception policy.

## Public API

The main entry points are:

- `AddGenericLoggingProxy(Action<LoggingOptions> configure = null)`
- `AddWithLogging<TInterface, TImplementation>(Action<LoggingOptions> configureOverride = null)`
- `LoggingOptions`

`LoggingOptions` currently exposes:

- `OnException`: callback executed when the proxied call throws
- `Logger`: optional `ILogger` instance used by the default behavior

## Project Structure

- `GenericLoggingHelper/`: main library
- `GenericLoggingHelper.Test/`: xUnit test project

Key implementation files:

- `GenericLoggingHelper/ServiceCollectionExtensions.cs`
- `GenericLoggingHelper/LoggingOptions.cs`
- `GenericLoggingHelper/GenericLoggingProxyFactory.cs`
- `GenericLoggingHelper/IGenericLoggingProxy.cs`

## Current Behavior and Limitations

This repository is functional, but it is still a lightweight utility rather than a fully mature observability framework.

Current limitations include:

- The active proxy path is designed for interface-based services only.
- The main interceptor currently focuses on exception handling, not full request/response tracing.
- Async scenarios are not explicitly covered by the current tests.
- There is an alternative proxy implementation in the repository that does not appear to be the main path used by DI registration.

## Tests

The test project currently validates these scenarios:

- A proxied method can be called successfully.
- The global exception handler is used when no local override is provided.
- A local override replaces the global exception handler.

Run the tests with:

```bash
dotnet test
```

## Target Framework and Dependencies

- .NET 8
- Castle.Core
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.DependencyInjection in the consuming application and tests

## Status

This project is suitable as a foundation for centralized exception logging around interface-based services in .NET applications using the default Microsoft DI container.

If you want to evolve it further, the next areas to strengthen are async interception, lifecycle flexibility, richer structured logging, and broader test coverage.