// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.ProxyInterceptorTests;

/// <summary>
/// Tests for <see cref="ProxyInterceptor{T}.GetFrom"/> method.
/// </summary>
public class ProxyInterceptorGetFromTests
{
    [Fact]
    public void GetFrom_WithProxyInstance_ShouldReturnInterceptor()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        /* Act */

        var result = ProxyInterceptor<IImplementation>.GetFrom(instance);

        /* Assert */

        Assert.NotNull(result);
        Assert.Same(impl, result.UpstreamInstance);
    }

    [Fact]
    public void GetFrom_WithNonMockInstance_ShouldReturnNull()
    {
        /* Arrange */

        var instance = new Implementation();

        /* Act */

        var result = ProxyInterceptor<IImplementation>.GetFrom(instance);

        /* Assert */

        Assert.Null(result);
    }

    [Fact]
    public void GetFrom_WithMockNotConfiguredAsProxy_ShouldReturnNull()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var instance = mock.Object;

        /* Act */

        var result = ProxyInterceptor<IImplementation>.GetFrom(instance);

        /* Assert */

        Assert.Null(result);
    }

    [Fact]
    public void GetFrom_WithNullInstance_ShouldReturnNull()
    {
        /* Arrange */

        IImplementation? instance = null;

        /* Act */

        var result = ProxyInterceptor<IImplementation>.GetFrom(instance);

        /* Assert */

        Assert.Null(result);
    }

    [Fact]
    public void UpstreamInstance_ShouldMatchOriginalImplementation()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        /* Act */

        var interceptor = ProxyInterceptor<IImplementation>.GetFrom(instance);

        /* Assert */

        Assert.NotNull(interceptor);
        Assert.NotNull(interceptor.UpstreamInstance);
        Assert.Same(impl, interceptor.UpstreamInstance);
    }

    [Fact]
    public void UpstreamInstance_ShouldBeAccessibleAfterMethodCall()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        // Call a method that forwards to impl
        var methodResult = instance.Method3(10);

        /* Act */

        var interceptor = ProxyInterceptor<IImplementation>.GetFrom(instance);

        /* Assert */

        Assert.NotNull(interceptor);
        Assert.Same(impl, interceptor.UpstreamInstance);
        Assert.Equal(11, methodResult);
        Assert.Single(impl.CallLog);
    }
}