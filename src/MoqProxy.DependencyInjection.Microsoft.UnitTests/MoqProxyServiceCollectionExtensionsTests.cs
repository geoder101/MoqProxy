// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

using MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests;

public class MoqProxyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMoqProxy_ShouldDecorateExistingService()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();

        /* Act */

        services.AddMoqProxy(mock);
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Assert */

        Assert.NotNull(service);
        Assert.Same(mock.Object, service);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardMethodCallsToImplementation()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        var result = service.GetMessage();

        /* Assert */

        Assert.Equal("Hello from implementation", result);
    }

    [Fact]
    public void AddMoqProxy_ShouldAllowVerificationOfCalls()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        service.GetMessage();
        service.Add(5, 3);

        /* Assert */

        mock.Verify(s => s.GetMessage(), Times.Once);
        mock.Verify(s => s.Add(5, 3), Times.Once);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardMethodCallsWithParameters()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        var result = service.Add(10, 20);

        /* Assert */

        Assert.Equal(30, result);
        mock.Verify(s => s.Add(10, 20), Times.Once);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardVoidMethodCalls()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        service.DoWork();
        service.DoWork();

        /* Assert */

        mock.Verify(s => s.DoWork(), Times.Exactly(2));
        Assert.Equal(2, impl.Counter);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardPropertyGetters()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        service.Counter = 42;
        var result = service.Counter;

        /* Assert */

        Assert.Equal(42, result);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardPropertySetters()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        service.Counter = 100;

        /* Assert */

        mock.VerifySet(s => s.Counter = 100, Times.Once);
        Assert.Equal(100, service.Counter);
    }

    [Fact]
    public void AddMoqProxy_ShouldAllowOverridingBehavior()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        mock.Setup(s => s.GetMessage()).Returns("Overridden message");

        /* Act */

        var result = service.GetMessage();

        /* Assert */

        Assert.Equal("Overridden message", result);
    }

    [Fact]
    public void AddMoqProxy_ShouldOverrideExistingMockSetup()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();
        mock.Setup(s => s.GetMessage()).Returns("Overridden message");

        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        var result = service.GetMessage();

        /* Assert */

        Assert.Equal("Hello from implementation", result);
    }

    [Fact]
    public void AddMoqProxy_ShouldReturnServiceCollectionForChaining()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock = new Mock<ITestService>();

        /* Act */

        var result = services.AddMoqProxy(mock);

        /* Assert */

        Assert.Same(services, result);
    }

    [Fact]
    public void AddMoqProxy_ShouldWorkWithTransientServices()
    {
        /* Arrange */

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestServiceImplementation>();

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();

        /* Act */

        var service1 = provider.GetRequiredService<ITestService>();
        var service2 = provider.GetRequiredService<ITestService>();

        /* Assert */

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        // Both should be the same mock object wrapping different implementations
        Assert.Same(mock.Object, service1);
        Assert.Same(mock.Object, service2);
    }

    [Fact]
    public void AddMoqProxy_ShouldWorkWithScopedServices()
    {
        /* Arrange */

        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceImplementation>();

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();

        /* Act & Assert */

        using (var scope1 = provider.CreateScope())
        {
            var service1 = scope1.ServiceProvider.GetRequiredService<ITestService>();
            Assert.Same(mock.Object, service1);
        }

        using (var scope2 = provider.CreateScope())
        {
            var service2 = scope2.ServiceProvider.GetRequiredService<ITestService>();
            Assert.Same(mock.Object, service2);
        }
    }

    [Fact]
    public async Task AddMoqProxy_ShouldForwardAsyncMethodCalls()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new ComplexServiceImplementation();
        services.AddSingleton<IComplexService>(impl);

        var mock = new Mock<IComplexService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IComplexService>();

        /* Act */

        var result = await service.GetDataAsync();

        /* Assert */

        Assert.Equal("Async data", result);
        mock.Verify(s => s.GetDataAsync(), Times.Once);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardMethodsReturningEnumerables()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new ComplexServiceImplementation();
        services.AddSingleton<IComplexService>(impl);

        var mock = new Mock<IComplexService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IComplexService>();

        /* Act */

        var result = service.GetNumbers().ToList();

        /* Assert */

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
        mock.Verify(s => s.GetNumbers(), Times.Once);
    }

    [Fact]
    public void AddMoqProxy_ShouldForwardMethodsWithComplexParameters()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new ComplexServiceImplementation();
        services.AddSingleton<IComplexService>(impl);

        var mock = new Mock<IComplexService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IComplexService>();

        var items = new List<string> { "Item1", "Item2" };

        /* Act */

        service.ProcessData(items);

        /* Assert */

        Assert.Contains("Processed", items);
        mock.Verify(s => s.ProcessData(It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public void AddMoqProxy_ShouldWorkWithGenericServices()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new GenericServiceImplementation<string>();
        services.AddSingleton<IGenericService<string>>(impl);

        var mock = new Mock<IGenericService<string>>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IGenericService<string>>();

        /* Act */

        service.SetValue("Test");
        var result = service.GetValue();

        /* Assert */

        Assert.Equal("Test", result);
        mock.Verify(s => s.SetValue("Test"), Times.Once);
        mock.Verify(s => s.GetValue(), Times.Once);
    }

    [Fact]
    public void AddMoqProxy_MultipleMocks_ShouldDecorateMultipleServices()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl1 = new TestServiceImplementation();
        var impl2 = new ComplexServiceImplementation();
        services.AddSingleton<ITestService>(impl1);
        services.AddSingleton<IComplexService>(impl2);

        var mock1 = new Mock<ITestService>();
        var mock2 = new Mock<IComplexService>();

        /* Act */

        services.AddMoqProxy(mock1);
        services.AddMoqProxy(mock2);

        var provider = services.BuildServiceProvider();
        var service1 = provider.GetRequiredService<ITestService>();
        var service2 = provider.GetRequiredService<IComplexService>();

        /* Assert */

        Assert.Same(mock1.Object, service1);
        Assert.Same(mock2.Object, service2);
    }

    [Fact]
    public void AddMoqProxy_WhenServiceNotRegistered_ShouldThrowExceptionOnResolve()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var mock = new Mock<ITestService>();

        /* Act */

        services.AddMoqProxy(mock); // Should not throw here
        var provider = services.BuildServiceProvider();

        /* Assert */

        // Should throw when trying to resolve the service
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ITestService>());
    }

    [Fact]
    public void AddMoqProxy_WhenCalledTwiceOnSameService_ShouldUseLatestMock()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var impl = new TestServiceImplementation();
        services.AddSingleton<ITestService>(impl);

        var mock1 = new Mock<ITestService>();
        var mock2 = new Mock<ITestService>();

        /* Act */

        services.AddMoqProxy(mock1);
        services.AddMoqProxy(mock2);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Assert */

        Assert.Same(mock2.Object, service);
        Assert.NotSame(mock1.Object, service);
    }

    [Fact]
    public void AddMoqProxy_WithFactoryRegistration_ShouldWrapFactoryResult()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var callCount = 0;

        services.AddSingleton<ITestService>(_ =>
        {
            callCount++;
            return new TestServiceImplementation();
        });

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();

        /* Act */

        var service1 = provider.GetRequiredService<ITestService>();
        var service2 = provider.GetRequiredService<ITestService>();

        /* Assert */

        Assert.Same(mock.Object, service1);
        Assert.Same(mock.Object, service2);

        // Factory should only be called once for singleton
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void AddMoqProxy_WithTransientFactoryRegistration_ShouldCallFactoryMultipleTimes()
    {
        /* Arrange */

        var services = new ServiceCollection();
        var callCount = 0;

        services.AddTransient<ITestService>(_ =>
        {
            callCount++;
            return new TestServiceImplementation();
        });

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();

        /* Act */

        var service1 = provider.GetRequiredService<ITestService>();
        var service2 = provider.GetRequiredService<ITestService>();

        /* Assert */

        Assert.Same(mock.Object, service1);
        Assert.Same(mock.Object, service2);

        // Factory should be called twice for transient
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void AddMoqProxy_ShouldMaintainServiceLifetime()
    {
        /* Arrange */

        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceImplementation>();

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();

        /* Act & Assert */

        using var scope = provider.CreateScope();
        var service1 = scope.ServiceProvider.GetRequiredService<ITestService>();
        var service2 = scope.ServiceProvider.GetRequiredService<ITestService>();

        // Within the same scope, should return the same mock.Object
        Assert.Same(service1, service2);
        Assert.Same(mock.Object, service1);
    }

    [Fact]
    public void AddMoqProxy_WithComplexDependencies_ShouldResolveCorrectly()
    {
        /* Arrange */

        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceImplementation>();

        var mock = new Mock<ITestService>();
        services.AddMoqProxy(mock);

        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ITestService>();

        /* Act */

        service.DoWork();
        var message = service.GetMessage();
        var sum = service.Add(3, 4);

        /* Assert */

        Assert.Equal("Hello from implementation", message);
        Assert.Equal(7, sum);
        mock.Verify(s => s.DoWork(), Times.Once);
        mock.Verify(s => s.GetMessage(), Times.Once);
        mock.Verify(s => s.Add(3, 4), Times.Once);
    }
}