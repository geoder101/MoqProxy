// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.ProxyInterceptorTests;

/// <summary>
/// Provides helper methods for working with Castle.DynamicProxy interceptors in unit tests.
/// </summary>
internal static class Helpers
{
    /// <summary>
    /// Helper method to get the interceptors array from a Castle.DynamicProxy proxy object.
    /// </summary>
    internal static IInterceptor[] GetInterceptors(object proxyObject)
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
    /// Helper method to check if an interceptor is a <see cref="FallbackMethodProxyInterceptor{T}"/>.
    /// </summary>
    internal static bool IsProxyInterceptor(IInterceptor interceptor)
    {
        var type = interceptor.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FallbackMethodProxyInterceptor<>);
    }

    /// <summary>
    /// Helper method to count the number of <see cref="FallbackMethodProxyInterceptor{T}"/> instances in the interceptors array.
    /// </summary>
    internal static int CountProxyInterceptors(IInterceptor[] interceptors)
        => interceptors.Count(IsProxyInterceptor);

    /// <summary>
    /// Helper method to check if any <see cref="FallbackMethodProxyInterceptor{T}"/> instances exist in the interceptors array.
    /// </summary>
    internal static bool ProxyInterceptorExists(IInterceptor[] interceptors)
        => CountProxyInterceptors(interceptors) > 0;
}