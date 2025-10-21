// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Reflection;
using Castle.DynamicProxy;
using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

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

        var interceptors = GetInterceptors(mock.Object);
        Assert.NotNull(interceptors);
        Assert.NotEmpty(interceptors);

        // Should have at least one interceptor (our custom one plus Moq's)
        var hasFallbackInterceptor = interceptors.Any(i =>
            i.GetType().Name.Contains("FallbackMethodProxyInterceptor"));
        Assert.True(hasFallbackInterceptor, "Expected FallbackMethodProxyInterceptor to be present");
    }

    [Fact]
    public void SetupAsProxy_CalledTwice_ShouldOnlyInjectInterceptorOnce()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);
        var interceptorsAfterFirstCall = GetInterceptors(mock.Object);
        var countAfterFirstCall = CountFallbackInterceptors(interceptorsAfterFirstCall);

        mock.SetupAsProxy(impl);
        var interceptorsAfterSecondCall = GetInterceptors(mock.Object);
        var countAfterSecondCall = CountFallbackInterceptors(interceptorsAfterSecondCall);

        /* Assert */

        Assert.Equal(1, countAfterFirstCall);
        Assert.Equal(1, countAfterSecondCall);
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

        var interceptors = GetInterceptors(mock.Object);
        var count = CountFallbackInterceptors(interceptors);

        /* Assert */

        Assert.Equal(1, count);
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

        var interceptors = GetInterceptors(mock.Object);

        /* Assert */

        Assert.NotNull(interceptors);
        Assert.NotEmpty(interceptors);

        // The fallback interceptor should be first in the chain
        var firstInterceptor = interceptors[0];
        Assert.True(
            firstInterceptor.GetType().Name.Contains("FallbackMethodProxyInterceptor"),
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

        var interceptors = GetInterceptors(mock.Object);
        var count = CountFallbackInterceptors(interceptors);

        /* Assert */

        Assert.Equal(1, count);

        // Verify explicit setup still works (overrides proxy)
        Assert.Equal(999, mock.Object.Method3(5));

        // Verify proxy still works for other methods
        mock.Object.Method2(42);
        Assert.Contains("Method2(42)", impl.CallLog);
    }

    /// <summary>
    /// Helper method to get the interceptors array from a Castle.DynamicProxy proxy object.
    /// </summary>
    private static IInterceptor[] GetInterceptors(object proxyObject)
    {
        if (proxyObject is not IProxyTargetAccessor)
        {
            throw new InvalidOperationException("Object is not a Castle.DynamicProxy proxy");
        }

        var proxyType = proxyObject.GetType();
        var interceptorsField = proxyType.GetField("__interceptors", BindingFlags.NonPublic | BindingFlags.Instance);

        if (interceptorsField == null)
        {
            throw new InvalidOperationException("Could not find __interceptors field");
        }

        return (IInterceptor[])interceptorsField.GetValue(proxyObject)!;
    }

    /// <summary>
    /// Helper method to count the number of FallbackMethodProxyInterceptor instances in the interceptors array.
    /// </summary>
    private static int CountFallbackInterceptors(IInterceptor[] interceptors)
    {
        return interceptors.Count(i => i.GetType().Name.Contains("FallbackMethodProxyInterceptor"));
    }
}