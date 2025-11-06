// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Linq.Expressions;
using Moq;
using MoqProxy.Internals;

namespace MoqProxy;

/// <summary>
/// Provides extension methods for configuring Moq mocks to act as proxies that forward calls to real implementations.
/// </summary>
public static class MockProxyExtensions
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
    /// <item><description>Forward event subscriptions (add/remove) to the implementation</description></item>
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

    /// <summary>
    /// Sets up a spy on a specific void method, forwarding calls to the upstream implementation while tracking them with Moq.
    /// This allows you to verify that the method was called while still executing the real implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock proxy instance.</param>
    /// <param name="expression">Expression specifying the method to spy on.</param>
    /// <param name="callback">Callback invoked when the method is called. Can be Action, Action&lt;T1&gt;, Action&lt;T1, T2&gt;, etc. matching the method signature.</param>
    /// <remarks>
    /// The mock must have been configured as a proxy using <see cref="SetupAsProxy{T}"/> before calling this method.
    /// </remarks>
    public static void Spy<T>(
        this Mock<T> mock,
        Expression<Action<T>> expression,
        Delegate callback)
        where T : class
        => MethodSpySetup.SetupVoidMethodSpy(mock, expression, callback);

    /// <summary>
    /// Sets up a spy on a specific method that has a return value, forwarding calls to the upstream implementation while tracking them with Moq.
    /// This allows you to verify that the method was called while still executing the real implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <typeparam name="TResult">The return type of the method.</typeparam>
    /// <param name="mock">The mock proxy instance.</param>
    /// <param name="expression">Expression specifying the method to spy on.</param>
    /// <param name="callback">Callback invoked before the real implementation is called. Can be Action, Action&lt;T1&gt;, Action&lt;T1, TResult&gt;, etc.</param>
    /// <remarks>
    /// The mock must have been configured as a proxy using <see cref="SetupAsProxy{T}"/> before calling this method.
    /// </remarks>
    public static void Spy<T, TResult>(
        this Mock<T> mock,
        Expression<Func<T, TResult>> expression,
        Delegate callback)
        where T : class
        => MethodSpySetup.SetupReturningMethodSpy(mock, expression, callback);
}