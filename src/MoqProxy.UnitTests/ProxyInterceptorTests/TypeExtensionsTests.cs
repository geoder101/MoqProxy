// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.ProxyInterceptorTests;

/// <summary>
/// Tests for <see cref="TypeExtensions"/> class, focusing on generic parameter erasure functionality.
/// </summary>
public class TypeExtensionsTests
{
    #region EraseGenericParameters Tests

    [Fact]
    public void EraseGenericParameters_WithGenericParameter_ShouldReturnObject()
    {
        /* Arrange */

        // Get a generic parameter from a generic type
        var genericType = typeof(List<>);
        var genericParam = genericType.GetGenericArguments()[0];

        /* Act */

        var result = genericParam.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(object), result);
    }

    [Fact]
    public void EraseGenericParameters_WithGenericMethodParameter_ShouldReturnObject()
    {
        /* Arrange */

        // Get a generic parameter from a generic method
        var method =
            typeof(TypeExtensionsTests).GetMethod(nameof(GenericMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var genericParam = method.GetGenericArguments()[0];

        /* Act */

        var result = genericParam.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(object), result);
    }

    [Fact]
    public void EraseGenericParameters_WithNonGenericType_ShouldReturnSameType()
    {
        /* Arrange */

        var type = typeof(int);

        /* Act */

        var result = type.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(int), result);
    }

    [Fact]
    public void EraseGenericParameters_WithClosedGenericType_ShouldReturnSameType()
    {
        /* Arrange */

        var type = typeof(List<int>);

        /* Act */

        var result = type.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(List<int>), result);
    }

    [Fact]
    public void EraseGenericParameters_WithOpenGenericType_ShouldEraseToObject()
    {
        /* Arrange */

        // List<T> where T is unbound
        var openGenericType = typeof(List<>);
        var typeParam = openGenericType.GetGenericArguments()[0];
        var listOfT = typeof(List<>).MakeGenericType(typeParam);

        /* Act */

        var result = listOfT.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(List<object>), result);
    }

    [Fact]
    public void EraseGenericParameters_WithArrayOfGenericParameter_ShouldReturnObjectArray()
    {
        /* Arrange */

        var genericType = typeof(List<>);
        var genericParam = genericType.GetGenericArguments()[0];
        var arrayType = genericParam.MakeArrayType();

        /* Act */

        var result = arrayType.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(object[]), result);
    }

    [Fact]
    public void EraseGenericParameters_WithMultiDimensionalArrayOfGenericParameter_ShouldReturnObjectArray()
    {
        /* Arrange */

        var genericType = typeof(List<>);
        var genericParam = genericType.GetGenericArguments()[0];
        var arrayType = genericParam.MakeArrayType(2); // T[,]

        /* Act */

        var result = arrayType.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(object[,]), result);
    }

    [Fact]
    public void EraseGenericParameters_WithByRefGenericParameter_ShouldReturnObjectByRef()
    {
        /* Arrange */

        var genericType = typeof(List<>);
        var genericParam = genericType.GetGenericArguments()[0];
        var byRefType = genericParam.MakeByRefType();

        /* Act */

        var result = byRefType.EraseGenericParameters();

        /* Assert */

        Assert.True(result.IsByRef);
        Assert.Equal(typeof(object).MakeByRefType(), result);
    }

    [Fact]
    public void EraseGenericParameters_WithPointerOfGenericParameter_ShouldReturnObjectPointer()
    {
        /* Arrange */

        var genericType = typeof(List<>);
        var genericParam = genericType.GetGenericArguments()[0];
        var pointerType = genericParam.MakePointerType();

        /* Act */

        var result = pointerType.EraseGenericParameters();

        /* Assert */

        Assert.True(result.IsPointer);
        Assert.Equal(typeof(object).MakePointerType(), result);
    }

    [Fact]
    public void EraseGenericParameters_WithNullableOfGenericParameter_ShouldReturnObject()
    {
        /* Arrange */

        // We can't actually create Nullable<T> at runtime where T is a generic parameter,
        // so we need to get it from a method signature
        var method = typeof(TypeExtensionsTests).GetMethod(nameof(MethodWithNullableGenericParameter),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var nullableType = method.GetParameters()[0].ParameterType;

        // Verify we have Nullable<T> where T is a generic parameter
        Assert.True(nullableType.IsGenericType);
        Assert.Equal(typeof(Nullable<>), nullableType.GetGenericTypeDefinition());
        Assert.True(nullableType.GetGenericArguments()[0].IsGenericParameter);

        /* Act */

        var result = nullableType.EraseGenericParameters();

        /* Assert */

        // Nullable<T> with T as generic parameter should erase to object (can't make Nullable<object>)
        Assert.Equal(typeof(object), result);
    }

    [Fact]
    public void EraseGenericParameters_WithNullableOfInt_ShouldReturnNullableInt()
    {
        /* Arrange */

        var type = typeof(int?);

        /* Act */

        var result = type.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(int?), result);
    }

    [Fact]
    public void EraseGenericParameters_WithNestedGenericTypes_ShouldEraseRecursively()
    {
        /* Arrange */

        // Dictionary<T, List<T>> where T is generic parameter
        var genericType = typeof(List<>);
        var genericParam = genericType.GetGenericArguments()[0];
        var listOfT = typeof(List<>).MakeGenericType(genericParam);
        var dictType = typeof(Dictionary<,>).MakeGenericType(genericParam, listOfT);

        /* Act */

        var result = dictType.EraseGenericParameters();

        /* Assert */

        Assert.Equal(typeof(Dictionary<object, List<object>>), result);
    }

    [Fact]
    public void EraseGenericParameters_WithConstrainedGenericType_ShouldFallbackToObject()
    {
        /* Arrange */

        // Get a constrained generic parameter (e.g., from a method with where T : struct)
        var method = typeof(TypeExtensionsTests).GetMethod(nameof(ConstrainedGenericMethod),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var genericParam = method.GetGenericArguments()[0];

        // Try to create a type that would violate constraints when erased
        // For example, using the constrained generic parameter in a context that will fail
        var type = typeof(ConstrainedGenericClass<>).MakeGenericType(genericParam);

        /* Act */

        var result = type.EraseGenericParameters();

        /* Assert */

        // When constraints can't be satisfied, should fallback to object
        Assert.Equal(typeof(object), result);
    }

    #endregion

    #region TryEraseGenericParameters Tests

    [Fact]
    public void TryEraseGenericParameters_WithGenericMethodDefinition_ShouldReturnTrueAndConcreteMethod()
    {
        /* Arrange */

        var method =
            typeof(TypeExtensionsTests).GetMethod(nameof(GenericMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.True(method.IsGenericMethodDefinition);

        /* Act */

        var success = method.TryEraseGenericParameters(out var concreteMethod);

        /* Assert */

        Assert.True(success);
        Assert.NotNull(concreteMethod);
        Assert.False(concreteMethod.IsGenericMethodDefinition);
        Assert.True(concreteMethod.IsGenericMethod);

        // Verify the method was made with object
        var genericArgs = concreteMethod.GetGenericArguments();
        Assert.Single(genericArgs);
        Assert.Equal(typeof(object), genericArgs[0]);
    }

    [Fact]
    public void TryEraseGenericParameters_WithNonGenericMethod_ShouldReturnFalse()
    {
        /* Arrange */

        var method =
            typeof(TypeExtensionsTests).GetMethod(nameof(NonGenericMethod),
                BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.False(method.IsGenericMethodDefinition);

        /* Act */

        var success = method.TryEraseGenericParameters(out var concreteMethod);

        /* Assert */

        Assert.False(success);
        Assert.Null(concreteMethod);
    }

    [Fact]
    public void TryEraseGenericParameters_WithAlreadyConcreteGenericMethod_ShouldReturnFalse()
    {
        /* Arrange */

        var method =
            typeof(TypeExtensionsTests).GetMethod(nameof(GenericMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var concreteMethod = method.MakeGenericMethod(typeof(string));
        Assert.False(concreteMethod.IsGenericMethodDefinition);

        /* Act */

        var success = concreteMethod.TryEraseGenericParameters(out var result);

        /* Assert */

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryEraseGenericParameters_WithConstrainedGenericMethod_ShouldReturnFalse()
    {
        /* Arrange */

        var method = typeof(TypeExtensionsTests).GetMethod(nameof(ConstrainedGenericMethod),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.True(method.IsGenericMethodDefinition);

        /* Act */

        var success = method.TryEraseGenericParameters(out var concreteMethod);

        /* Assert */

        // Should fail because object doesn't satisfy 'where T : struct' constraint
        Assert.False(success);
        Assert.Null(concreteMethod);
    }

    [Fact]
    public void TryEraseGenericParameters_WithMultipleGenericParameters_ShouldEraseAllToObject()
    {
        /* Arrange */

        var method = typeof(TypeExtensionsTests).GetMethod(nameof(MultipleGenericParametersMethod),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.True(method.IsGenericMethodDefinition);

        /* Act */

        var success = method.TryEraseGenericParameters(out var concreteMethod);

        /* Assert */

        Assert.True(success);
        Assert.NotNull(concreteMethod);

        var genericArgs = concreteMethod.GetGenericArguments();
        Assert.Equal(3, genericArgs.Length);
        Assert.All(genericArgs, arg => Assert.Equal(typeof(object), arg));
    }

    #endregion

    #region Helper Methods for Tests

    private static void GenericMethod<T>()
    {
    }

    private static void NonGenericMethod()
    {
    }

    private static void ConstrainedGenericMethod<T>() where T : struct
    {
    }

    private static void MultipleGenericParametersMethod<T1, T2, T3>()
    {
    }

    private static void MethodWithNullableGenericParameter<T>(T? nullable) where T : struct
    {
    }

    #endregion
}