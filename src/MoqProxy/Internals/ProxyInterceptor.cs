// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Castle.DynamicProxy;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace MoqProxy.Internals;

/// <summary>
/// Castle.DynamicProxy interceptor that forwards method calls to the real implementation when no Moq setup matches.
/// Uses a sentinel value to detect when Moq hasn't matched any setup, then invokes the method on the real implementation.
/// Properly handles ref/out parameters by copying values back after invocation.
/// </summary>
/// <typeparam name="T">The type being mocked.</typeparam>
internal class ProxyInterceptor<T> : IInterceptor
    where T : class
{
    /// <summary>
    /// Creates a new interceptor that forwards to <paramref name="impl"/> when needed.
    /// </summary>
    /// <param name="impl">The implementation instance to forward calls to.</param>
    public ProxyInterceptor(T impl) => UpstreamInstance = impl;

    /// <summary>
    /// The upstream implementation instance to which calls are forwarded when no Moq setup matches.
    /// </summary>
    public T UpstreamInstance { get; }

    /// <summary>
    /// Intercepts method calls on the mock proxy, checking if a Moq setup was matched.
    /// If no setup matched (indicated by the sentinel return value), forwards the call to the real implementation.
    /// Handles ref/out parameters by copying modified values back to the invocation.
    /// </summary>
    /// <param name="invocation">The method invocation details from Castle.DynamicProxy.</param>
    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var parameters = method.GetParameters();

        // Check if method has ref/out parameters
        var hasRefOrOutParameters = parameters.Any(p => p.ParameterType.IsByRef);

        // Use NullReturnValue as sentinel to detect if no setup was matched
        if (method.ReturnType != typeof(void))
        {
            invocation.ReturnValue = NullReturnValue.Instance;
        }

        Exception exception1 = null!;
        Exception exception2 = null!;

        try
        {
            invocation.Proceed();
        }
        catch (Exception ex)
        {
            exception1 = ex;
        }
        finally
        {
            try
            {
                // Determine if we should forward to the implementation:
                // For methods WITH ref/out parameters:
                //   - Forward only if no Moq setup was matched (checked via sentinel for non-void)
                // For methods WITHOUT ref/out parameters:
                //   - Forward only if no Moq setup was matched (checked via sentinel for non-void)
                //   - Void methods without ref/out are handled by SetupMethod, so don't forward
                //   - EXCEPTION: Event accessors (add_/remove_) are skipped by SetupMethod, so forward them

                bool setupWasMatched;
                if (method.ReturnType != typeof(void))
                {
                    // For non-void methods, check if setup was matched via sentinel
                    setupWasMatched = invocation.ReturnValue != NullReturnValue.Instance;
                }
                else
                {
                    // For void methods, we can't easily detect if a setup matched
                    // But void methods with ref/out parameters are NOT set up by SetupMethod
                    // Also, event accessors (add_EventName, remove_EventName) are NOT set up by SetupMethod
                    // So we should forward them
                    var isEventAccessor =
                        method.IsSpecialName
                        && (method.Name.StartsWith("add_", StringComparison.Ordinal)
                            || method.Name.StartsWith("remove_", StringComparison.Ordinal));

                    setupWasMatched = !hasRefOrOutParameters && !isEventAccessor;
                }

                var shouldForward = !setupWasMatched;

                if (shouldForward)
                {
                    // Create a copy of arguments for the invocation
                    var args = invocation.Arguments.ToArray();

                    // Invoke the method on the implementation
                    var result = method.Invoke(UpstreamInstance, args);

                    // Copy back ref/out parameter values
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType.IsByRef)
                        {
                            invocation.SetArgumentValue(i, args[i]);
                        }
                    }

                    // Set return value if not void
                    if (method.ReturnType != typeof(void))
                    {
                        invocation.ReturnValue = result;
                    }
                }
            }
            catch (Exception ex)
            {
                exception2 = ex;
            }
        }

        switch (exception1, exception2)
        {
            case (not null, not null):
                throw new AggregateException(
                    "Multiple exceptions occurred during method interception.",
                    exception1, exception2);
            case (not null, null): throw exception1;
            case (null, not null): throw exception2;
        }
    }

    /// <summary>
    /// Retrieves the <see cref="ProxyInterceptor{T}"/> instance attached to the given proxy instance, if any.
    /// Uses <see cref="CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors"/> to read Castle's internal interceptors array.
    /// Returns null when the provided instance is null, not a Castle proxy, or doesn't contain a <see cref="ProxyInterceptor{T}"/>.
    /// </summary>
    /// <param name="instance">The proxy instance to inspect.</param>
    /// <returns>The matching <see cref="ProxyInterceptor{T}"/> or null if not present.</returns>
    public static ProxyInterceptor<T>? GetFrom(T? instance)
    {
        if (instance is null)
        {
            return null;
        }

        var interceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(instance);
        return interceptors?.FirstOrDefault(i => i is ProxyInterceptor<T>) as ProxyInterceptor<T>;
    }
}