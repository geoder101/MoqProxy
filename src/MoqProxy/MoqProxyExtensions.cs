// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.Internals;

namespace MoqProxy;

/// <summary>
/// Provides extension methods for configuring Moq mocks to act as proxies that forward calls to real implementations.
/// </summary>
public static class MoqProxyExtensions
{
    /// <summary>
    /// Sets up a mock to act as a proxy by forwarding all calls to the provided implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked. Must be a class.</typeparam>
    /// <param name="mock">The Moq mock instance to configure as a proxy.</param>
    /// <param name="impl">The implementation instance to which calls will be forwarded.</param>
    /// <remarks>
    /// This method configures the mock to:
    /// <list type="bullet">
    /// <item><description>Inject a fallback interceptor to forward unmatched calls to the implementation</description></item>
    /// <item><description>Forward property getters and setters to the implementation</description></item>
    /// <item><description>Forward method calls to the implementation</description></item>
    /// <item><description>Support indexer properties</description></item>
    /// </list>
    /// After calling this method, you can still override specific behaviors using standard Moq Setup methods.
    /// </remarks>
    public static void SetupAsProxy<T>(
        this Mock<T> mock,
        T impl)
        where T : class
    {
        // Inject the fallback interceptor first - it runs before all other setups
        if (!InterceptorSetup.TrySetupInterceptor(mock, impl))
        {
            return;
        }

        PropertySetup.SetupProperties(mock, impl);
        MethodSetup.SetupMethods(mock, impl);
    }
}