// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;

namespace MoqProxy.Internals;

/// <summary>
/// Provides methods for setting up the fallback interceptor on mock instances.
/// </summary>
internal static class InterceptorSetup
{
    /// <summary>
    /// Attempts to inject a custom interceptor into the mock's proxy that forwards unmatched calls to the implementation.
    /// This interceptor runs before all Moq setups, detecting when no setup was matched and forwarding the call to the real implementation.
    /// </summary>
    /// <typeparam name="T">The type being mocked.</typeparam>
    /// <param name="mock">The mock instance to configure.</param>
    /// <param name="impl">The implementation instance to forward calls to.</param>
    /// <remarks>
    /// The interceptor is only added once. If it's already present, this method does nothing.
    /// Uses Castle.DynamicProxy's internal __interceptors field to inject the custom interceptor at the beginning of the chain.
    /// </remarks>
    internal static bool TrySetupInterceptor<T>(
        Mock<T> mock,
        T impl)
        where T : class
    {
        // Get current interceptors using reflection
        var currentInterceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(mock.Object);
        if (currentInterceptors == null)
        {
            return false;
        }

        // Check if our interceptor is already added - skip if it is
        if (currentInterceptors.Any(i => i is FallbackMethodProxyInterceptor<T>))
        {
            return false;
        }

        // Create our custom interceptor
        var fallbackProxyInterceptor = new FallbackMethodProxyInterceptor<T>(impl);
        // Prepend our interceptor to the beginning of the chain so it runs first
        var newInterceptors = new[] { fallbackProxyInterceptor }.Concat(currentInterceptors).ToArray();

        // Set the new interceptors array
        if (!CastleDynamicProxyInterceptorsFieldAccessor.TrySetInterceptors(mock.Object, newInterceptors))
        {
            return false;
        }

        // Set up custom default value provider to return NullReturnValue sentinel
        mock.DefaultValueProvider = NullReturnValueProvider.Instance;
        return true;
    }
}