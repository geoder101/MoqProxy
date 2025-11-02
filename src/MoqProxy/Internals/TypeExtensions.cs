// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MoqProxy.Internals;

/// <summary>
/// Provides extension methods for type manipulation and generic parameter erasure.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Replaces any open generic parameters in a <see cref="Type"/> with <see cref="object"/>.
    /// This produces an "erased" type that can be used when building reflection-based
    /// expressions or constructing closed generic types for mocks.
    /// </summary>
    /// <param name="type">The type to erase generic parameters from.</param>
    /// <returns>
    /// A type with all generic parameters replaced by <see cref="object"/>, preserving array, pointer, and by-ref shapes.
    /// </returns>
    /// <remarks>
    /// The method:
    /// <list type="bullet">
    /// <item><description>Maps generic type parameters to <see cref="object"/></description></item>
    /// <item><description>Preserves arrays, pointers and by-ref shapes while erasing their element types</description></item>
    /// <item><description>Rebuilds generic types using erased type arguments</description></item>
    /// </list>
    /// </remarks>
    internal static Type EraseGenericParameters(this Type type)
    {
        // If the type itself is a generic parameter (e.g. T) use object.
        if (type.IsGenericParameter
            || type.IsGenericMethodParameter)
        {
            return typeof(object);
        }

        // Handle arrays: erase the element type then re-create the array with the original rank.
        if (type.IsArray)
        {
            var elem = type.GetElementType()!.EraseGenericParameters();
            var rank = type.GetArrayRank();
            // Use MakeArrayType() for single-dimensional zero-based arrays, MakeArrayType(rank) for multi-dimensional
            return rank == 1 ? elem.MakeArrayType() : elem.MakeArrayType(rank);
        }

        // Handle by-ref types (ref/out): erase the element and return a by-ref of it.
        if (type.IsByRef)
        {
            var elem = type.GetElementType()!.EraseGenericParameters();
            return elem.MakeByRefType();
        }

        // Handle pointers similarly.
        if (type.IsPointer)
        {
            var elem = type.GetElementType()!.EraseGenericParameters();
            return elem.MakePointerType();
        }

        // Non-generic types are returned as-is.
        if (!type.IsGenericType)
        {
            return type;
        }

        var def = type.GetGenericTypeDefinition();

        // Special case: Nullable<T> where T is a generic parameter
        // Nullable<T> has a struct constraint, so we can't make Nullable<object>
        // Check this BEFORE erasing the generic arguments to avoid attempting invalid construction
        if (def == typeof(Nullable<>))
        {
            var arg = type.GetGenericArguments()[0];
            if (arg.IsGenericParameter || arg.IsGenericMethodParameter)
            {
                return typeof(object);
            }
        }

        // For generic types, erase each generic argument and construct the generic type definition
        // with the erased arguments.
        var newArgs = type.GetGenericArguments()
            .Select(a => a.EraseGenericParameters())
            .ToArray();

        // After erasure, check if we would create Nullable<object>
        if (def == typeof(Nullable<>) && newArgs.Length == 1 && newArgs[0] == typeof(object))
        {
            return typeof(object);
        }

        // Try to construct the generic type, but if it fails due to constraint violations,
        // just return object as a fallback
        try
        {
            return def.MakeGenericType(newArgs);
        }
        catch (ArgumentException)
        {
            // Constraints prevent construction (e.g., other constrained generics)
            return typeof(object);
        }
    }

    /// <summary>
    /// Attempts to construct a concrete <see cref="MethodInfo"/> from a generic method definition by substituting
    /// all method generic parameters with <see cref="object"/>.
    /// </summary>
    /// <param name="method">The method to attempt to make concrete.</param>
    /// <param name="concreteMethod">When this method returns true, contains the concrete method; otherwise, null.</param>
    /// <returns>
    /// <c>true</c> if the method is a generic method definition and a concrete method could be constructed;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Returns false if the method is not a generic method definition or if generic parameter constraints
    /// prevent construction. This is used by the proxy setup to try to create a usable non-generic method
    /// for expression construction; failure is expected for some generic methods and is handled by skipping setup.
    /// </remarks>
    internal static bool TryEraseGenericParameters(
        this MethodInfo method,
        [NotNullWhen(true)] out MethodInfo? concreteMethod)
    {
        // Build generic type arguments: replace any method/type generic parameters with `object`.
        var genericParams = method.GetGenericArguments();
        var genericTypeArgs = genericParams
            .Select(gp =>
                gp.IsGenericParameter || gp.IsGenericMethodParameter
                    ? typeof(object)
                    : gp)
            .ToArray();

        // Try to construct the generic method. If constraints prevent construction, skip this method.
        try
        {
            if (method.IsGenericMethodDefinition)
            {
                concreteMethod = method.MakeGenericMethod(genericTypeArgs);
                return true;
            }

            concreteMethod = null;
            return false;
        }
        catch (ArgumentException)
        {
            // Can't satisfy generic parameter constraints with the chosen types -> skip setup
            concreteMethod = null;
            return false;
        }
    }
}