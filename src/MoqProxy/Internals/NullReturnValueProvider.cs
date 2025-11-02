// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;

namespace MoqProxy.Internals;

/// <summary>
/// Custom Moq <see cref="DefaultValueProvider"/> that returns the <see cref="NullReturnValue"/> sentinel
/// for all unmatched method calls. This allows the interceptor to detect when no setup was matched
/// and forward the call to the real implementation.
/// </summary>
internal class NullReturnValueProvider : DefaultValueProvider
{
    /// <summary>
    /// Singleton instance of <see cref="NullReturnValueProvider"/>.
    /// </summary>
    public static readonly NullReturnValueProvider Instance = new();

    private NullReturnValueProvider()
    {
    }

    /// <summary>
    /// Returns the <see cref="NullReturnValue.Instance"/> sentinel for any type.
    /// </summary>
    /// <param name="type">The return type of the method (unused).</param>
    /// <param name="mock">The mock instance (unused).</param>
    /// <returns>The <see cref="NullReturnValue.Instance"/> sentinel.</returns>
    protected override object GetDefaultValue(Type type, Mock mock)
        => NullReturnValue.Instance;
}