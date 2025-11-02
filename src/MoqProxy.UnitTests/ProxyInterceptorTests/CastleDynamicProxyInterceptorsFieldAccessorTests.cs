// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.ProxyInterceptorTests;

public class CastleDynamicProxyInterceptorsFieldAccessorTests
{
    [Fact]
    public void GetInterceptorsField_WithValidProxy_ShouldReturnFieldInfo()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var proxy = mock.Object;

        /* Act */

        var field = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptorsField(proxy);

        /* Assert */

        Assert.NotNull(field);
        Assert.Equal("__interceptors", field.Name);
    }

    [Fact]
    public void GetInterceptors_WithValidProxy_ShouldReturnInterceptorsArray()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var proxy = mock.Object;

        /* Act */

        var interceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(proxy);

        /* Assert */

        Assert.NotNull(interceptors);
        Assert.IsType<IInterceptor[]>(interceptors, exactMatch: false);
    }

    [Fact]
    public void GetInterceptors_WithNonProxy_ShouldReturnNull()
    {
        /* Arrange */

        var notAProxy = new Implementation();

        /* Act */

        var interceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(notAProxy);

        /* Assert */

        Assert.Null(interceptors);
    }

    [Fact]
    public void SetInterceptors_WithValidProxy_ShouldSetInterceptors()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var proxy = mock.Object;
        var newInterceptors = new IInterceptor[] { new TestInterceptor() };

        /* Act */

        var result = CastleDynamicProxyInterceptorsFieldAccessor.TrySetInterceptors(proxy, newInterceptors);

        /* Assert */

        Assert.True(result);
        var actualInterceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(proxy);
        Assert.NotNull(actualInterceptors);
        Assert.Single(actualInterceptors);
        Assert.IsType<TestInterceptor>(actualInterceptors[0]);
    }

    [Fact]
    public void SetInterceptors_WithNonProxy_ShouldReturnFalse()
    {
        /* Arrange */

        var notAProxy = new Implementation();
        var newInterceptors = new IInterceptor[] { new TestInterceptor() };

        /* Act */

        var result = CastleDynamicProxyInterceptorsFieldAccessor.TrySetInterceptors(notAProxy, newInterceptors);

        /* Assert */

        Assert.False(result);
    }

    [Fact]
    public void SetInterceptors_CanReplaceExistingInterceptors()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var proxy = mock.Object;
        var originalInterceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(proxy);

        var newInterceptors = new IInterceptor[]
        {
            new TestInterceptor(),
            new TestInterceptor(),
            new TestInterceptor()
        };

        /* Act */

        var result = CastleDynamicProxyInterceptorsFieldAccessor.TrySetInterceptors(proxy, newInterceptors);

        /* Assert */

        Assert.True(result);
        var actualInterceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(proxy);
        Assert.NotNull(actualInterceptors);
        Assert.Equal(3, actualInterceptors.Length);
        Assert.NotEqual(originalInterceptors?.Length ?? 0, actualInterceptors.Length);
    }

    [Fact]
    public void SetInterceptors_WithEmptyArray_ShouldSetEmptyInterceptors()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var proxy = mock.Object;
        var emptyInterceptors = Array.Empty<IInterceptor>();

        /* Act */

        var result = CastleDynamicProxyInterceptorsFieldAccessor.TrySetInterceptors(proxy, emptyInterceptors);

        /* Assert */

        Assert.True(result);
        var actualInterceptors = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptors(proxy);
        Assert.NotNull(actualInterceptors);
        Assert.Empty(actualInterceptors);
    }

    [Fact]
    public void GetInterceptorsField_WithNonProxy_ShouldReturnNull()
    {
        /* Arrange */

        var notAProxy = new Implementation();

        /* Act */

        var field = CastleDynamicProxyInterceptorsFieldAccessor.GetInterceptorsField(notAProxy);

        /* Assert */

        Assert.Null(field);
    }
}