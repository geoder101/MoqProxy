// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

public class EdgeCaseTests
{
    [Fact]
    public void SetupAsProxy_MethodWithThreeParameters_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithMultipleParametersImpl();
        var mock = new Mock<IMethodWithMultipleParameters>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.Add(1, 2, 3);

        /* Assert */

        Assert.Equal(6, result);
    }

    [Fact]
    public void SetupAsProxy_MethodWithFourParameters_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithMultipleParametersImpl();
        var mock = new Mock<IMethodWithMultipleParameters>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.Concat("a", "b", "c", "d");

        /* Assert */

        Assert.Equal("abcd", result);
    }

    [Fact]
    public void SetupAsProxy_BooleanReturnType_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithVariousReturnTypesImpl();
        var mock = new Mock<IMethodWithVariousReturnTypes>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.GetBool();

        /* Assert */

        Assert.True(result);
    }

    [Fact]
    public void SetupAsProxy_DoubleReturnType_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithVariousReturnTypesImpl();
        var mock = new Mock<IMethodWithVariousReturnTypes>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.GetDouble();

        /* Assert */

        Assert.Equal(3.14, result);
    }

    [Fact]
    public void SetupAsProxy_StringReturnType_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithVariousReturnTypesImpl();
        var mock = new Mock<IMethodWithVariousReturnTypes>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.GetString();

        /* Assert */

        Assert.Equal("test", result);
    }

    [Fact]
    public void SetupAsProxy_ObjectReturnType_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithVariousReturnTypesImpl();
        var mock = new Mock<IMethodWithVariousReturnTypes>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.GetObject();

        /* Assert */

        Assert.NotNull(result);
    }

    [Fact]
    public void SetupAsProxy_ListReturnType_ShouldForward()
    {
        /* Arrange */

        var impl = new MethodWithVariousReturnTypesImpl();
        var mock = new Mock<IMethodWithVariousReturnTypes>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.GetList();

        /* Assert */

        Assert.Equal(3, result.Count);
        Assert.Equal(new List<int> { 1, 2, 3 }, result);
    }

    [Fact]
    public void SetupAsProxy_MethodOverload_NoParameters()
    {
        /* Arrange */

        var impl = new MethodOverloadsImpl();
        var mock = new Mock<IMethodOverloads>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Process();

        /* Assert */

        Assert.Contains("Process()", impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_MethodOverload_IntParameter()
    {
        /* Arrange */

        var impl = new MethodOverloadsImpl();
        var mock = new Mock<IMethodOverloads>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Process(42);

        /* Assert */

        Assert.Contains("Process(42)", impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_MethodOverload_StringParameter()
    {
        /* Arrange */

        var impl = new MethodOverloadsImpl();
        var mock = new Mock<IMethodOverloads>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Process("test");

        /* Assert */

        Assert.Contains("Process(test)", impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_MethodOverload_MultipleParameters()
    {
        /* Arrange */

        var impl = new MethodOverloadsImpl();
        var mock = new Mock<IMethodOverloads>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Process(42, "test");

        /* Assert */

        Assert.Contains("Process(42, test)", impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AllOverloads_ShouldDistinguish()
    {
        /* Arrange */

        var impl = new MethodOverloadsImpl();
        var mock = new Mock<IMethodOverloads>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Process();
        mock.Object.Process(10);
        mock.Object.Process("hello");
        mock.Object.Process(20, "world");

        /* Assert */

        Assert.Equal(4, impl.CallLog.Count);
        Assert.Contains("Process()", impl.CallLog);
        Assert.Contains("Process(10)", impl.CallLog);
        Assert.Contains("Process(hello)", impl.CallLog);
        Assert.Contains("Process(20, world)", impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_IntType()
    {
        /* Arrange */

        var impl = new GenericMethodsImpl();
        var mock = new Mock<IGenericMethods>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.Identity(42);

        /* Assert */

        Assert.Equal(42, result);
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_StringType()
    {
        /* Arrange */

        var impl = new GenericMethodsImpl();
        var mock = new Mock<IGenericMethods>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.Identity("test");

        /* Assert */

        Assert.Equal("test", result);
    }

    [Fact]
    public void SetupAsProxy_GenericMethod_ReturningGenericList()
    {
        /* Arrange */

        var impl = new GenericMethodsImpl();
        var mock = new Mock<IGenericMethods>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.CreateList(42);

        /* Assert */

        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }
}