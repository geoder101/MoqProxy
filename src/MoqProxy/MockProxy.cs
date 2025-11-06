// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.Internals;

namespace MoqProxy;

/// <summary>
/// MoqProxy accessors to retrieve Moq artifacts (such as <c>Mock{T}</c>) and upstream implementations from proxy objects.
/// </summary>
public static class MockProxy
{
    /// <summary>
    /// Attempts to retrieve the <see cref="Moq.Mock{T}"/> wrapper for the provided instance.
    /// Returns null when the instance is not a Moq mock.
    /// </summary>
    /// <typeparam name="T">The mocked type.</typeparam>
    /// <param name="instance">The instance to retrieve the mock for.</param>
    /// <returns>The <see cref="Moq.Mock{T}"/> if the instance is a mock; otherwise null.</returns>
    public static Mock<T>? GetMock<T>(T instance)
        where T : class
    {
        try
        {
            return Mock.Get(instance);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves the upstream implementation instance that the mock proxy forwards to when no setup matches.
    /// Returns <c>null</c> when the provided instance is not a Moq mock or the mock has not been configured as a proxy.
    /// </summary>
    /// <typeparam name="T">The mocked type.</typeparam>
    /// <param name="instance">The proxy instance to inspect.</param>
    /// <returns>The upstream implementation instance, or <c>null</c> if not available.</returns>
    public static T? GetUpstreamInstance<T>(T instance)
        where T : class
    {
        var proxyInterceptor = ProxyInterceptor<T>.GetFrom(GetMock(instance)?.Object);
        return proxyInterceptor?.UpstreamInstance;
    }
}