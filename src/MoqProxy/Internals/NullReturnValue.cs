// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.Internals;

/// <summary>
/// Sentinel type used to detect when Moq has not matched any setup for a method call.
/// This singleton value is returned by the custom <see cref="NullReturnValueProvider"/> and checked
/// by the <see cref="ProxyInterceptor{T}"/> to determine whether to forward the call to the real implementation.
/// </summary>
internal sealed class NullReturnValue
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullReturnValue"/>.
    /// </summary>
    public static readonly NullReturnValue Instance = new();

    private NullReturnValue()
    {
    }

    public static bool operator ==(NullReturnValue? left, NullReturnValue? right)
        => ReferenceEquals(left, right) || (left is not null && right is not null);

    public static bool operator !=(NullReturnValue? left, NullReturnValue? right)
        => !(left == right);

    public static bool operator ==(NullReturnValue? left, object? right)
        => right is null || (left is not null && ReferenceEquals(left, right));

    public static bool operator !=(NullReturnValue? left, object? right)
        => !(left == right);

    public static bool operator ==(object? left, NullReturnValue? right)
        => left is null || (right is not null && ReferenceEquals(left, right));

    public static bool operator !=(object? left, NullReturnValue? right)
        => !(left == right);

    public override bool Equals(object? obj)
        => obj is null or NullReturnValue;

    public override int GetHashCode() => 0;
}