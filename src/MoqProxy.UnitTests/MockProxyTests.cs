// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests;

/// <summary>
/// Tests for <see cref="MockProxy"/> accessor methods.
/// </summary>
public class MockProxyTests
{
    #region GetMock Tests

    [Fact]
    public void GetMock_WithMockInstance_ShouldReturnMock()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var instance = mock.Object;

        /* Act */

        var result = MockProxy.GetMock(instance);

        /* Assert */

        Assert.NotNull(result);
        Assert.Same(mock, result);
    }

    [Fact]
    public void GetMock_WithNonMockInstance_ShouldReturnNull()
    {
        /* Arrange */

        var instance = new Implementation();

        /* Act */

        var result = MockProxy.GetMock(instance);

        /* Assert */

        Assert.Null(result);
    }

    [Fact]
    public void GetMock_WithProxyInstance_ShouldReturnMock()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        /* Act */

        var result = MockProxy.GetMock(instance);

        /* Assert */

        Assert.NotNull(result);
        Assert.Same(mock, result);
    }

    #endregion

    #region GetUpstreamInstance Tests

    [Fact]
    public void GetUpstreamInstance_WithProxyInstance_ShouldReturnUpstreamImplementation()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        /* Act */

        var result = MockProxy.GetUpstreamInstance(instance);

        /* Assert */

        Assert.NotNull(result);
        Assert.Same(impl, result);
    }

    [Fact]
    public void GetUpstreamInstance_WithNonMockInstance_ShouldReturnNull()
    {
        /* Arrange */

        var instance = new Implementation();

        /* Act */

        var result = MockProxy.GetUpstreamInstance(instance);

        /* Assert */

        Assert.Null(result);
    }

    [Fact]
    public void GetUpstreamInstance_WithMockNotConfiguredAsProxy_ShouldReturnNull()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();
        var instance = mock.Object;

        /* Act */

        var result = MockProxy.GetUpstreamInstance(instance);

        /* Assert */

        Assert.Null(result);
    }

    [Fact]
    public void GetUpstreamInstance_WithProxyCalledMultipleTimes_ShouldReturnSameInstance()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        /* Act */

        var result1 = MockProxy.GetUpstreamInstance(instance);
        var result2 = MockProxy.GetUpstreamInstance(instance);

        /* Assert */

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Same(impl, result1);
        Assert.Same(impl, result2);
        Assert.Same(result1, result2);
    }

    [Fact]
    public void GetUpstreamInstance_AfterProxyForwardsCall_ShouldStillReturnUpstream()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        // Trigger a forwarded call
        instance.Method1();

        /* Act */

        var result = MockProxy.GetUpstreamInstance(instance);

        /* Assert */

        Assert.NotNull(result);
        Assert.Same(impl, result);
        Assert.Single(impl.CallLog);
        Assert.Equal("Method1", impl.CallLog[0]);
    }

    [Fact]
    public void GetUpstreamInstance_WithProxyAndMoqSetup_ShouldStillReturnUpstream()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        // Add a Moq setup that overrides a method
        mock.Setup(m => m.Method3(It.IsAny<int>())).Returns(999);

        var instance = mock.Object;

        /* Act */

        var result = MockProxy.GetUpstreamInstance(instance);
        var methodResult = instance.Method3(5);

        /* Assert */

        Assert.NotNull(result);
        Assert.Same(impl, result);
        Assert.Equal(999, methodResult); // Moq setup should take precedence
        Assert.Empty(impl.CallLog); // Implementation should not be called
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MockProxy_GetMockAndGetUpstream_ShouldBeConsistent()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var instance = mock.Object;

        /* Act */

        var retrievedMock = MockProxy.GetMock(instance);
        var retrievedUpstream = MockProxy.GetUpstreamInstance(instance);

        /* Assert */

        Assert.NotNull(retrievedMock);
        Assert.NotNull(retrievedUpstream);
        Assert.Same(mock, retrievedMock);
        Assert.Same(impl, retrievedUpstream);
    }

    [Fact]
    public void MockProxy_GetUpstream_WithDifferentProxies_ShouldReturnDifferentUpstreams()
    {
        /* Arrange */

        var impl1 = new Implementation();
        var impl2 = new Implementation();

        var mock1 = new Mock<IImplementation>();
        mock1.SetupAsProxy(impl1);

        var mock2 = new Mock<IImplementation>();
        mock2.SetupAsProxy(impl2);

        var instance1 = mock1.Object;
        var instance2 = mock2.Object;

        /* Act */

        var upstream1 = MockProxy.GetUpstreamInstance(instance1);
        var upstream2 = MockProxy.GetUpstreamInstance(instance2);

        /* Assert */

        Assert.NotNull(upstream1);
        Assert.NotNull(upstream2);
        Assert.Same(impl1, upstream1);
        Assert.Same(impl2, upstream2);
        Assert.NotSame(upstream1, upstream2);
    }

    #endregion
}