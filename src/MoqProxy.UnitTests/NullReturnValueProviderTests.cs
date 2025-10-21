// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy.UnitTests.Helpers;

namespace MoqProxy.UnitTests;

public class NullReturnValueProviderTests
{
    [Fact]
    public void SetupAsProxy_ShouldSetDefaultValueProviderToSingletonInstance()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);

        /* Assert */

        Assert.NotNull(mock.DefaultValueProvider);
        Assert.Equal("NullReturnValueProvider", mock.DefaultValueProvider.GetType().Name);
    }

    [Fact]
    public void SetupAsProxy_MultipleMocks_ShouldUseSameSingletonInstance()
    {
        /* Arrange */

        var impl1 = new Implementation();
        var mock1 = new Mock<IImplementation>();

        var impl2 = new Implementation();
        var mock2 = new Mock<IImplementation>();

        /* Act */

        mock1.SetupAsProxy(impl1);
        mock2.SetupAsProxy(impl2);

        /* Assert */

        Assert.NotNull(mock1.DefaultValueProvider);
        Assert.NotNull(mock2.DefaultValueProvider);
        Assert.Same(mock1.DefaultValueProvider, mock2.DefaultValueProvider);
    }

    [Fact]
    public void SetupAsProxy_DefaultValueProvider_ShouldBeSetBeforeInterceptorSetup()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();

        /* Act */

        mock.SetupAsProxy(impl);

        // Call a method to ensure the DefaultValueProvider is used
        var result = mock.Object.Method3(5);

        /* Assert */

        Assert.Equal(6, result);
        Assert.NotNull(mock.DefaultValueProvider);
    }
}