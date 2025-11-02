// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.MethodProxyTests;

public class AsyncMethodTests
{
    [Fact]
    public async Task SetupAsProxy_TaskMethods_ShouldForward()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        await mock.Object.Method1Async();
        await mock.Object.Method2Async(5);
        var result = await mock.Object.Method3Async(7);

        /* Assert */

        Assert.Equal(8, result);
        Assert.Equal(new[] { "Method1Async", "Method2Async(5)", "Method3Async(7) -> 8" }, impl.CallLog);
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
        Assert.Equal(["Method3Async(11) -> 12"], impl.CallLog);
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
}