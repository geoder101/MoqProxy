// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

/// <summary>
/// Tests for the EraseGenericParameters functionality.
/// These tests verify that generic methods with complex parameter types work correctly.
/// </summary>
public class GenericMethodConstraintsTests
{
    [Fact]
    public void SetupAsProxy_GenericMethod_WithArrayParameter_ShouldForward()
    {
        /* Arrange */

        var impl = new GenericConstraintsImpl();
        var mock = new Mock<IGenericConstraints>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.ProcessArray(new[] { "a", "b", "c" });

        /* Assert */

        Assert.Equal("abc", result);
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_WithMultipleGenericParameters_ShouldForward()
    {
        /* Arrange */

        var impl = new GenericConstraintsImpl();
        var mock = new Mock<IGenericConstraints>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.ConvertAndCombine(42, x => x.ToString());

        /* Assert */

        Assert.Equal("42", result);
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_ReturningGenericArray_ShouldForward()
    {
        /* Arrange */

        var impl = new GenericConstraintsImpl();
        var mock = new Mock<IGenericConstraints>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.CreateArray("test", 3);

        /* Assert */

        Assert.Equal(3, result.Length);
        Assert.All(result, item => Assert.Equal("test", item));
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_WithClassConstraint_ShouldForward()
    {
        /* Arrange */

        var impl = new GenericConstraintsImpl();
        var mock = new Mock<IGenericConstraints>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.TransformString("test");

        /* Assert */

        Assert.Equal("TEST", result);
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_ReturningReferenceType_ShouldForward()
    {
        /* Arrange */

        var impl = new GenericConstraintsImpl();
        var mock = new Mock<IGenericConstraints>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.WrapInList(42);

        /* Assert */

        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }
}