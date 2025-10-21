// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

public class ProxyExtensionTests
{
    [Fact]
    public void SetupAsProxy_ShouldForwardPropertyGet()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        var value = mock.Object.Property;

        /* Assert */

        Assert.Equal(42, value);
        Assert.Equal(["Prop.get -> 42"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardPropertySet()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Property = 100;

        /* Assert */

        Assert.Equal(100, impl.Property);
        Assert.Equal(100, mock.Object.Property);
        Assert.Equal(["Prop.set <- 100", "Prop.get -> 100", "Prop.get -> 100"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardVoidMethodWithNoParameters()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Method1();

        /* Assert */

        mock.Verify(m => m.Method1(), Times.Once);
        Assert.Equal(["Method1"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardVoidMethodWithParameters()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Method2(5);

        /* Assert */

        mock.Verify(m => m.Method2(5), Times.Once);
        Assert.Equal(["Method2(5)"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardReturningMethod()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = mock.Object.Method3(7);

        /* Assert */

        Assert.Equal(8, result);
        Assert.Equal(["Method3(7)"], impl.CallLog);
    }

    [Fact]
    public async Task SetupAsProxy_ShouldForwardAsyncVoidMethodWithNoParameters()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        await mock.Object.Method1Async();

        /* Assert */

        mock.Verify(m => m.Method1Async(), Times.Once);
        Assert.Equal(["Method1Async"], impl.CallLog);
    }

    [Fact]
    public async Task SetupAsProxy_ShouldForwardAsyncVoidMethodWithParameters()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        await mock.Object.Method2Async(9);

        /* Assert */

        mock.Verify(m => m.Method2Async(9), Times.Once);
        Assert.Equal(["Method2Async(9)"], impl.CallLog);
    }

    [Fact]
    public async Task SetupAsProxy_ShouldForwardAsyncReturningMethod()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        var result = await mock.Object.Method3Async(11);

        /* Assert */

        Assert.Equal(12, result);
        Assert.Equal(["Method3Async(11)"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AllowsMockOverride_PropertyGet()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.SetupGet(m => m.Property).Returns(999);
        var value = mock.Object.Property;

        /* Assert */

        Assert.Equal(999, value);
        Assert.Equal(42, impl.Property); // Original implementation unchanged
        Assert.Equal(["Prop.get -> 42"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AllowsMockOverride_VoidMethod()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var callbackInvoked = false;

        /* Act */

        mock.Setup(m => m.Method1()).Callback(() => callbackInvoked = true);
        mock.Object.Method1();

        /* Assert */

        Assert.True(callbackInvoked);
        Assert.Empty(impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AllowsMockOverride_ReturningMethod()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Setup(m => m.Method3(It.IsAny<int>())).Returns<int>(x => x * 10);
        var result = mock.Object.Method3(7);

        /* Assert */

        Assert.Equal(70, result);
        Assert.Empty(impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AllowsMockOverride_GenericMethodInt()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Setup(m => m.GenericMethod(It.IsAny<int>())).Returns<int>(x => x * 10);
        var result = mock.Object.GenericMethod(7);

        /* Assert */

        Assert.Equal(70, result);
        Assert.Empty(impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AllowsMockOverride_GenericMethodString()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Setup(m => m.GenericMethod(It.IsAny<string>())).Returns<string>(x => x.ToUpper());
        var result = mock.Object.GenericMethod("hello");

        /* Assert */

        Assert.Equal("HELLO", result);
        Assert.Empty(impl.CallLog);
    }

    [Fact]
    public async Task SetupAsProxy_AllowsMockOverride_AsyncReturningMethod()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Setup(m => m.Method3Async(It.IsAny<int>())).ReturnsAsync((int x) => x * 20);
        var result = await mock.Object.Method3Async(11);

        /* Assert */

        Assert.Equal(220, result);
        Assert.Empty(impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_AfterReset_CanBeReapplied()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        mock.Setup(m => m.Method3(It.IsAny<int>())).Returns(999);

        /* Act */

        mock.Reset();
        mock.SetupAsProxy(impl);
        var result = mock.Object.Method3(7);

        /* Assert */

        Assert.Equal(8, result); // Back to forwarding behavior
        Assert.Equal(["Method3(7)"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_PropertySetAndGet_Roundtrip()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Property = 200;
        var value = mock.Object.Property;

        /* Assert */

        Assert.Equal(200, value);
        Assert.Equal(200, impl.Property);
        Assert.Equal(["Prop.set <- 200", "Prop.get -> 200", "Prop.get -> 200"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_MultipleMethodCalls_ShouldAllBeForwarded()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.Method1();
        mock.Object.Method2(5);
        var result1 = mock.Object.Method3(7);
        mock.Object.Method1();
        var result2 = mock.Object.Method3(10);

        /* Assert */

        Assert.Equal(8, result1);
        Assert.Equal(11, result2);
        mock.Verify(m => m.Method1(), Times.Exactly(2));
        mock.Verify(m => m.Method2(5), Times.Once);
        mock.Verify(m => m.Method3(7), Times.Once);
        mock.Verify(m => m.Method3(10), Times.Once);
        Assert.Equal(["Method1", "Method2(5)", "Method3(7)", "Method1", "Method3(10)"], impl.CallLog);
    }

    [Fact]
    public void SetupAsProxy_WithDifferentImplementationInstances_ShouldForwardToCorrectInstance()
    {
        /* Arrange */

        var impl1 = new Implementation();
        var impl2 = new Implementation();
        var mock1 = new Mock<IImplementation>();
        var mock2 = new Mock<IImplementation>();

        mock1.SetupAsProxy(impl1);
        mock2.SetupAsProxy(impl2);

        /* Act */

        mock1.Object.Property = 100;
        mock2.Object.Property = 200;

        /* Assert */

        Assert.Equal(100, impl1.Property);
        Assert.Equal(200, impl2.Property);
        Assert.Equal(100, mock1.Object.Property);
        Assert.Equal(200, mock2.Object.Property);
        Assert.Equal(["Prop.set <- 100", "Prop.get -> 100", "Prop.get -> 100"], impl1.CallLog);
        Assert.Equal(["Prop.set <- 200", "Prop.get -> 200", "Prop.get -> 200"], impl2.CallLog);
    }
}