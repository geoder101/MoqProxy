// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

// ReSharper disable CheckNamespace

using Moq;
using MoqProxy;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for integrating MoqProxy with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class MoqProxyServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Moq proxy to the service collection that wraps an existing service implementation.
    /// </summary>
    /// <typeparam name="TService">The type of service to proxy. Must be a class.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the proxy to.</param>
    /// <param name="mock">The Moq mock instance that will act as a proxy for the service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method decorates an existing service registration with a Moq proxy. The mock will forward
    /// all calls to the original implementation, allowing you to observe or modify behavior during testing.
    /// The service of type <typeparamref name="TService"/> must already be registered in the service collection.
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = new Mock&lt;IMyService&gt;();
    /// services.AddMoqProxy(mock);
    /// // Now the mock will wrap the registered IMyService implementation
    /// </code>
    /// </example>
    public static IServiceCollection AddMoqProxy<TService>(
        this IServiceCollection services,
        Mock<TService> mock)
        where TService : class
        => services.Decorate<TService>((_, impl) =>
        {
            mock.SetupAsProxy(impl);
            return mock.Object;
        });
}