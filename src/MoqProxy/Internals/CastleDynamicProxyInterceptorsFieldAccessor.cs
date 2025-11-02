// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Reflection;
using Castle.DynamicProxy;

namespace MoqProxy.Internals;

/// <summary>
/// Provides helper methods to access the Castle.DynamicProxy __interceptors field.
/// </summary>
internal static class CastleDynamicProxyInterceptorsFieldAccessor
{
    /// <summary>
    /// Gets the __interceptors FieldInfo for the given proxy object's type.
    /// </summary>
    /// <param name="proxyObject">The proxy object to get the interceptors field for.</param>
    /// <returns>The FieldInfo for the __interceptors field, or null if not found.</returns>
    private static FieldInfo? GetInterceptorsField(object proxyObject)
    {
        if (proxyObject is not IProxyTargetAccessor)
        {
            return null;
        }

        var proxyType = proxyObject.GetType();
        return proxyType.GetField("__interceptors", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    /// <summary>
    /// Gets the interceptors array from a Castle proxy object.
    /// </summary>
    /// <param name="proxyObject">The proxy object to get interceptors from.</param>
    /// <returns>The array of interceptors, or null if the field could not be accessed.</returns>
    internal static IInterceptor[]? GetInterceptors(object proxyObject)
    {
        var field = GetInterceptorsField(proxyObject);
        return field?.GetValue(proxyObject) as IInterceptor[];
    }

    /// <summary>
    /// Sets the interceptors array on a Castle proxy object.
    /// </summary>
    /// <param name="proxyObject">The proxy object to set interceptors on.</param>
    /// <param name="interceptors">The array of interceptors to set.</param>
    /// <returns>True if the interceptors were set successfully, false otherwise.</returns>
    internal static bool TrySetInterceptors(object proxyObject, IInterceptor[] interceptors)
    {
        var field = GetInterceptorsField(proxyObject);
        if (field == null)
        {
            return false;
        }

        field.SetValue(proxyObject, interceptors);
        return true;
    }
}