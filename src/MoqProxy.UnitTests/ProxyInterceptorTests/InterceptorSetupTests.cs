// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.ProxyInterceptorTests;

/// <summary>
/// Tests for verifying that the fallback interceptor is set up correctly and only once.
/// </summary>
public class InterceptorSetupTests
{
    [Fact]
    public void SetupAsProxy_ShouldInjectInterceptor()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);

        /* Assert */

        var interceptors = Helpers.GetInterceptors(mock.Object);
        Assert.NotNull(interceptors);
        Assert.NotEmpty(interceptors);

        // Should have at least one interceptor (our custom one plus Moq's)
        Assert.True(
            interceptors.Length >= 1,
            "Expected at least one interceptor to be present");
        Assert.True(
            Helpers.ProxyInterceptorExists(interceptors),
            "Expected FallbackMethodProxyInterceptor to be present");
    }

    [Fact]
    public void SetupAsProxy_CalledTwice_ShouldOnlyInjectInterceptorOnce()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);
        var interceptorsAfterFirstCall = Helpers.GetInterceptors(mock.Object);
        var proxyInterceptorsCountAfterFirstCall = Helpers.CountProxyInterceptors(interceptorsAfterFirstCall);

        mock.SetupAsProxy(impl);
        var interceptorsAfterSecondCall = Helpers.GetInterceptors(mock.Object);
        var proxyInterceptorsCountAfterSecondCall = Helpers.CountProxyInterceptors(interceptorsAfterSecondCall);

        /* Assert */

        Assert.Equal(1, proxyInterceptorsCountAfterFirstCall);
        Assert.Equal(1, proxyInterceptorsCountAfterSecondCall);
        Assert.Equal(interceptorsAfterFirstCall.Length, interceptorsAfterSecondCall.Length);
    }

    [Fact]
    public void SetupAsProxy_CalledMultipleTimes_ShouldNotDuplicateInterceptor()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        // Call SetupAsProxy multiple times
        mock.SetupAsProxy(impl);
        mock.SetupAsProxy(impl);
        mock.SetupAsProxy(impl);
        mock.SetupAsProxy(impl);

        var interceptors = Helpers.GetInterceptors(mock.Object);
        var proxyInterceptorsCount = Helpers.CountProxyInterceptors(interceptors);

        /* Assert */

        Assert.Equal(1, proxyInterceptorsCount);
    }

    [Fact]
    public void SetupAsProxy_WithInterceptor_ShouldStillForwardCalls()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);
        mock.SetupAsProxy(impl); // Call twice to ensure interceptor isn't duplicated

        var result = mock.Object.Method3(5);

        /* Assert */

        Assert.Equal(6, result);
        Assert.Contains("Method3(5) -> 6", impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_InterceptorPlacement_ShouldBeFirst()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);

        var interceptors = Helpers.GetInterceptors(mock.Object);

        /* Assert */

        Assert.NotNull(interceptors);
        Assert.NotEmpty(interceptors);

        // The fallback interceptor should be first in the chain
        var firstInterceptor = interceptors[0];
        Assert.True(
            Helpers.IsProxyInterceptor(firstInterceptor),
            "Expected FallbackMethodProxyInterceptor to be the first interceptor");
    }

    [Fact]
    public void SetupAsProxy_AfterExplicitSetup_InterceptorStillOnlyOnce()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        // Set up proxy first
        mock.SetupAsProxy(impl);

        // Add explicit setup
        mock.Setup(m => m.Method3(It.IsAny<int>())).Returns(999);

        // Call SetupAsProxy again
        mock.SetupAsProxy(impl);

        var interceptors = Helpers.GetInterceptors(mock.Object);
        var proxyInterceptorsCount = Helpers.CountProxyInterceptors(interceptors);

        /* Assert */

        Assert.Equal(1, proxyInterceptorsCount);

        // Verify explicit setup still works (overrides proxy)
        Assert.Equal(999, mock.Object.Method3(5));

        // Verify proxy still works for other methods
        mock.Object.Method2(42);
        Assert.Contains("Method2(42)", impl.CallLog);
    }
}