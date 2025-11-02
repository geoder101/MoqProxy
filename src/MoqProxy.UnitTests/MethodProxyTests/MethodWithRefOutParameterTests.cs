// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.MethodProxyTests;

/// <summary>
/// Tests for ref and out parameter support in MoqProxy.
/// </summary>
public class MethodWithRefOutParameterTests
{
    [Fact]
    public void SetupAsProxy_ShouldForwardOutParameter_IntTryParse()
    {
        /* Arrange */

        var impl = new Parser();
        var mock = new Mock<IParser>();
        mock.SetupAsProxy(impl);

        /* Act */

        var success = mock.Object.TryParse("123", out var result);

        /* Assert */

        Assert.True(success);
        Assert.Equal(123, result);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardOutParameter_FailedParse()
    {
        /* Arrange */

        var impl = new Parser();
        var mock = new Mock<IParser>();
        mock.SetupAsProxy(impl);

        /* Act */

        var success = mock.Object.TryParse("invalid", out var result);

        /* Assert */

        Assert.False(success);
        Assert.Equal(0, result); // Default value
    }

    [Fact]
    public void SetupAsProxy_ShouldVerifyOutParameterCall()
    {
        /* Arrange */

        var impl = new Parser();
        var mock = new Mock<IParser>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Object.TryParse("456", out var result);

        /* Assert */

        mock.Verify(m => m.TryParse("456", out It.Ref<int>.IsAny), Times.Once);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardOutParameter_Double()
    {
        /* Arrange */

        var impl = new Parser();
        var mock = new Mock<IParser>();
        mock.SetupAsProxy(impl);

        /* Act */

        var success = mock.Object.TryParseDouble("3.14", out var result);

        /* Assert */

        Assert.True(success);
        Assert.Equal(3.14, result, precision: 2);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardRefParameter_Increment()
    {
        /* Arrange */

        var impl = new RefOperations();
        var mock = new Mock<IRefOperations>();
        mock.SetupAsProxy(impl);

        /* Act */

        int value = 5;
        mock.Object.Increment(ref value);

        /* Assert */

        Assert.Equal(6, value);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardRefParameter_Swap()
    {
        /* Arrange */

        var impl = new RefOperations();
        var mock = new Mock<IRefOperations>();
        mock.SetupAsProxy(impl);

        /* Act */

        int a = 10;
        int b = 20;
        mock.Object.Swap(ref a, ref b);

        /* Assert */

        Assert.Equal(20, a);
        Assert.Equal(10, b);
    }

    [Fact]
    public void SetupAsProxy_ShouldForwardRefParameter_WithReturnValue()
    {
        /* Arrange */

        var impl = new RefOperations();
        var mock = new Mock<IRefOperations>();
        mock.SetupAsProxy(impl);

        /* Act */

        int value = 10;
        var result = mock.Object.IncrementAndReturn(ref value);

        /* Assert */

        Assert.Equal(11, value);
        Assert.Equal(11, result);
    }

    [Fact]
    public void SetupAsProxy_ShouldVerifyRefParameterCall()
    {
        /* Arrange */

        var impl = new RefOperations();
        var mock = new Mock<IRefOperations>();
        mock.SetupAsProxy(impl);

        /* Act */

        int value = 5;
        mock.Object.Increment(ref value);

        /* Assert */

        mock.Verify(m => m.Increment(ref It.Ref<int>.IsAny), Times.Once);
    }

    [Fact]
    public void SetupAsProxy_ShouldAllowOverrideOfOutParameterMethod()
    {
        /* Arrange */

        var impl = new Parser();
        var mock = new Mock<IParser>();
        mock.SetupAsProxy(impl);

        // Override specific behavior
        mock.Setup(m => m.TryParse("override", out It.Ref<int>.IsAny))
            .Returns(new TryParseCallback((string s, out int r) =>
            {
                r = 999;
                return true;
            }));

        /* Act */

        var success1 = mock.Object.TryParse("override", out var result1);
        var success2 = mock.Object.TryParse("123", out var result2);

        /* Assert */

        Assert.True(success1);
        Assert.Equal(999, result1); // Override value

        Assert.True(success2);
        Assert.Equal(123, result2); // Forwarded to implementation
    }

    private delegate bool TryParseCallback(string input, out int result);
}