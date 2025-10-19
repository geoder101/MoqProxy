// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

public class AsyncProxyTests
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
        Assert.Equal(new[] { "Method1Async", "Method2Async(5)", "Method3Async(7)" }, impl.CallLog);
    }
}