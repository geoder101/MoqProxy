# MoqProxy.DependencyInjection.Microsoft

Microsoft.Extensions.DependencyInjection integration for [MoqProxy](https://github.com/geoder101/MoqProxy) - enabling proxy pattern mocking within ASP.NET Core and other dependency injection scenarios.

## What is this?

This library provides extension methods to seamlessly integrate MoqProxy with Microsoft's dependency injection container. It allows you to **wrap existing service registrations with Moq proxies** for testing, enabling you to:

- **Verify calls** to services registered in your DI container
- **Spy on real implementations** without changing production code
- **Test integration scenarios** with partial mocking
- **Observe service interactions** in complex dependency graphs

## Installation

```bash
dotnet add package geoder101.MoqProxy.DependencyInjection.Microsoft
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Moq;

// Set up your services as usual
var services = new ServiceCollection();
services.AddSingleton<IEmailService, EmailService>();
services.AddSingleton<IUserService, UserService>();

// Wrap a service with a Moq proxy for testing
var emailMock = new Mock<IEmailService>();
services.AddMoqProxy(emailMock);

// Build and use the container
var provider = services.BuildServiceProvider();
var userService = provider.GetRequiredService<IUserService>();

// The UserService will receive the proxied EmailService
userService.RegisterUser("john@example.com");

// Verify the real EmailService was called through the proxy
emailMock.Verify(e => e.SendWelcomeEmail("john@example.com"), Times.Once);
```

## How It Works

`AddMoqProxy<TService>()` decorates an existing service registration with a Moq proxy using the [decorator pattern](https://en.wikipedia.org/wiki/Decorator_pattern):

1. Resolves the original service implementation from the container
2. Sets up the mock as a proxy that forwards all calls to the original implementation
3. Replaces the service registration with the mock object

This means:
- ✅ The real implementation runs normally
- ✅ You can verify all interactions via Moq
- ✅ You can override specific behaviors if needed
- ✅ Works with interfaces and classes

## Usage Examples

### Basic Service Proxying

```csharp
var services = new ServiceCollection();
services.AddSingleton<ICalculator, Calculator>();

var mock = new Mock<ICalculator>();
services.AddMoqProxy(mock);

var provider = services.BuildServiceProvider();
var calculator = provider.GetRequiredService<ICalculator>();

// Calls are forwarded to the real Calculator implementation
var result = calculator.Add(2, 3); // Returns 5

// But you can still verify the call
mock.Verify(c => c.Add(2, 3), Times.Once);
```

### Testing Service Dependencies

```csharp
var services = new ServiceCollection();
services.AddSingleton<IRepository, Repository>();
services.AddSingleton<IBusinessLogic, BusinessLogic>(); // Depends on IRepository

// Proxy the repository to observe calls from BusinessLogic
var repoMock = new Mock<IRepository>();
services.AddMoqProxy(repoMock);

var provider = services.BuildServiceProvider();
var businessLogic = provider.GetRequiredService<IBusinessLogic>();

// Execute business logic that uses the repository
businessLogic.ProcessOrder(orderId: 123);

// Verify the repository was called correctly
repoMock.Verify(r => r.GetOrder(123), Times.Once);
repoMock.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Once);
```

### Selective Behavior Override

```csharp
var services = new ServiceCollection();
services.AddSingleton<IPaymentGateway, StripePaymentGateway>();

var mock = new Mock<IPaymentGateway>();
services.AddMoqProxy(mock);

// Override specific behavior for testing
mock.Setup(p => p.IsAvailable()).Returns(false);

var provider = services.BuildServiceProvider();
var gateway = provider.GetRequiredService<IPaymentGateway>();

// Overridden behavior
Assert.False(gateway.IsAvailable()); // Returns false from setup

// Other calls still forward to StripePaymentGateway
gateway.ProcessPayment(amount: 100); // Calls real implementation
```

### Integration Testing with ASP.NET Core

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Registration_ShouldSendEmail()
    {
        // Arrange
        var emailMock = new Mock<IEmailService>();

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Proxy the email service
                    services.AddMoqProxy(emailMock);
                });
            });

        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/users/register", new { Email = "test@example.com" });

        // Assert
        response.EnsureSuccessStatusCode();
        emailMock.Verify(e => e.SendWelcomeEmail("test@example.com"), Times.Once);
    }
}
```

### Multiple Service Proxying

```csharp
var services = new ServiceCollection();
services.AddSingleton<ILogger, ConsoleLogger>();
services.AddSingleton<ICache, RedisCache>();
services.AddSingleton<IApi, ExternalApi>();

// Proxy multiple services
var loggerMock = new Mock<ILogger>();
var cacheMock = new Mock<ICache>();
var apiMock = new Mock<IApi>();

services.AddMoqProxy(loggerMock);
services.AddMoqProxy(cacheMock);
services.AddMoqProxy(apiMock);

var provider = services.BuildServiceProvider();

// All services are now proxied and verifiable
// ... run your test scenario ...

// Verify all interactions
loggerMock.Verify(l => l.Log(It.IsAny<string>()), Times.AtLeastOnce);
cacheMock.Verify(c => c.Get(It.IsAny<string>()), Times.Once);
apiMock.Verify(a => a.FetchData(), Times.Once);
```

## Requirements

- The service of type `TService` must already be registered in the `IServiceCollection`
- Requires [geoder101.MoqProxy](https://www.nuget.org/packages/geoder101.MoqProxy/) package (installed as a dependency)
- Requires [geoder101.Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/geoder101.Microsoft.Extensions.DependencyInjection/) for decorator support

## API Reference

### AddMoqProxy&lt;TService&gt;

```csharp
public static IServiceCollection AddMoqProxy<TService>(
    this IServiceCollection services,
    Mock<TService> mock)
    where TService : class
```

**Parameters:**
- `services` - The service collection containing the service to proxy
- `mock` - The Moq mock instance that will wrap the original implementation

**Returns:** The `IServiceCollection` for method chaining

**Remarks:**
- The service must be registered before calling `AddMoqProxy`
- The mock is automatically configured to proxy all calls using `SetupAsProxy()`
- The original service lifetime (Singleton, Scoped, Transient) is preserved

## Related Projects

- [MoqProxy](https://github.com/geoder101/MoqProxy) - The core proxy pattern extension for Moq
- [Moq](https://github.com/devlooped/moq) - The popular .NET mocking library

## License

This project is licensed under the MIT License - see the [LICENSE.txt](../../LICENSE.txt) file for details.
