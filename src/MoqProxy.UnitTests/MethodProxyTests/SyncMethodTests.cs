// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.MethodProxyTests;

public class SyncMethodTests
{
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
        Assert.Equal(["Method3(7) -> 8"], impl.CallLog);
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
        Assert.Equal(["Method1", "Method2(5)", "Method3(7) -> 8", "Method1", "Method3(10) -> 11"], impl.CallLog);
    }
}