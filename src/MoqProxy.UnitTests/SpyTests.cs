// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests;

/// <summary>
/// Tests for <see cref="MockProxyExtensions.Spy{T, TResult}"/> and <see cref="MockProxyExtensions.Spy{T}"/> methods.
/// </summary>
public class SpyTests
{
    #region Spy with Return Value Tests

    [Fact]
    public void Spy_WithReturnValue_ShouldForwardToImplementation()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy(m => m.Method3(It.IsAny<int>()), () => { });
        var result = mock.Object.Method3(5);

        /* Assert */

        Assert.Equal(6, result); // Implementation returns input + 1
        Assert.Single(impl.CallLog);
        Assert.Equal("Method3(5) -> 6", impl.CallLog[0]);
    }

    [Fact]
    public void Spy_WithReturnValue_ShouldTrackCallsInMoq()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy(m => m.Method3(It.IsAny<int>()), () => { });
        mock.Object.Method3(10);
        mock.Object.Method3(20);

        /* Assert */

        mock.Verify(m => m.Method3(10), Times.Once);
        mock.Verify(m => m.Method3(20), Times.Once);
        mock.Verify(m => m.Method3(It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    public void Spy_WithReturnValueAndCallback_ShouldInvokeCallback()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var callbackInvoked = false;

        /* Act */

        mock.Spy(
            m => m.Method3(It.IsAny<int>()),
            () => callbackInvoked = true);

        var result = mock.Object.Method3(5);

        /* Assert */

        Assert.True(callbackInvoked);
        Assert.Equal(6, result);
        Assert.Single(impl.CallLog);
    }

    [Fact]
    public void Spy_WithReturnValue_WithoutProxy_ShouldThrow()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();

        /* Act & Assert */

        var ex = Assert.Throws<InvalidOperationException>(() => mock.Spy(m => m.Method3(It.IsAny<int>()), () => { }));

        Assert.Contains("SetupAsProxy", ex.Message);
    }

    [Fact]
    public void Spy_WithReturnValue_NonMethodExpression_ShouldThrow()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act & Assert */

        var ex = Assert.Throws<ArgumentException>(() => mock.Spy(m => m.Property, () => { }));

        Assert.Contains("method call", ex.Message);
    }

    [Fact]
    public void Spy_WithReturnValue_GenericMethod_ShouldWork()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy<IImplementation, string>(m => m.GenericMethod(It.IsAny<string>()), () => { });
        var result = mock.Object.GenericMethod("test");

        /* Assert */

        Assert.Equal("test", result);
        Assert.Single(impl.CallLog);
        Assert.Equal("GenericMethod(test)", impl.CallLog[0]);
        mock.Verify(m => m.GenericMethod("test"), Times.Once);
    }

    #endregion

    #region Spy Void Method Tests

    [Fact]
    public void Spy_VoidMethod_ShouldForwardToImplementation()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy(m => m.Method1(), () => { });
        mock.Object.Method1();

        /* Assert */

        Assert.Single(impl.CallLog);
        Assert.Equal("Method1", impl.CallLog[0]);
    }

    [Fact]
    public void Spy_VoidMethod_ShouldTrackCallsInMoq()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy(m => m.Method1(), () => { });
        mock.Object.Method1();
        mock.Object.Method1();

        /* Assert */

        mock.Verify(m => m.Method1(), Times.Exactly(2));
        Assert.Equal(2, impl.CallLog.Count);
    }

    [Fact]
    public void Spy_VoidMethodWithParameters_ShouldWork()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy(m => m.Method2(It.IsAny<int>()), () => { });
        mock.Object.Method2(42);

        /* Assert */

        Assert.Single(impl.CallLog);
        Assert.Equal("Method2(42)", impl.CallLog[0]);
        mock.Verify(m => m.Method2(42), Times.Once);
    }

    [Fact]
    public void Spy_VoidMethodWithCallback_ShouldInvokeCallback()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var callbackCount = 0;

        /* Act */

        mock.Spy(
            m => m.Method1(),
            () => callbackCount++);

        mock.Object.Method1();

        /* Assert */

        // The callback should be invoked
        Assert.Equal(1, callbackCount);
        // The implementation should also be called
        Assert.Single(impl.CallLog);
        Assert.Equal("Method1", impl.CallLog[0]);
    }

    [Fact]
    public void Spy_VoidMethod_WithoutProxy_ShouldThrow()
    {
        /* Arrange */

        var mock = new Mock<IImplementation>();

        /* Act & Assert */

        var ex = Assert.Throws<InvalidOperationException>(() => mock.Spy(m => m.Method1(), () => { }));

        Assert.Contains("SetupAsProxy", ex.Message);
    }

    [Fact]
    public void Spy_VoidMethod_NonMethodExpression_ShouldThrow()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act & Assert */

        // Use a property getter (not a method call) to test invalid expression
        var ex = Assert.Throws<ArgumentException>(() => mock.Spy(m => m.Property, () => { }));

        Assert.Contains("method call", ex.Message);
    }

    #endregion

    #region Spy with Parameters in Callback Tests

    [Fact]
    public void Spy_WithReturnValueAndCallbackWithParams_ShouldPassParameters()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var capturedParam = 0;

        /* Act */

        mock.Spy(
            m => m.Method3(It.IsAny<int>()),
            (int x) => capturedParam = x);

        var result = mock.Object.Method3(42);

        /* Assert */

        Assert.Equal(42, capturedParam);
        Assert.Equal(43, result); // Implementation returns input + 1
        Assert.Single(impl.CallLog);
    }

    [Fact]
    public void Spy_WithReturnValueAndCallbackWithParamsAndResult_ShouldPassParametersAndResult()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var capturedParam = 0;
        var capturedResult = 0;

        /* Act */

        mock.Spy(
            m => m.Method3(It.IsAny<int>()),
            (int x, int result) =>
            {
                capturedParam = x;
                capturedResult = result;
            });

        var actualResult = mock.Object.Method3(42);

        /* Assert */

        Assert.Equal(42, capturedParam);
        Assert.Equal(43, capturedResult);
        Assert.Equal(43, actualResult);
        Assert.Single(impl.CallLog);
    }

    [Fact]
    public void Spy_VoidMethodWithCallbackWithParams_ShouldPassParameters()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var capturedParam = 0;

        /* Act */

        mock.Spy(
            m => m.Method2(It.IsAny<int>()),
            (int x) => capturedParam = x);

        mock.Object.Method2(99);

        /* Assert */

        Assert.Equal(99, capturedParam);
        Assert.Single(impl.CallLog);
        Assert.Equal("Method2(99)", impl.CallLog[0]);
    }

    [Fact]
    public void Spy_GenericMethodWithCallbackWithParams_ShouldWork()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var capturedValue = string.Empty;
        var capturedResult = string.Empty;

        /* Act */

        mock.Spy(
            m => m.GenericMethod(It.IsAny<string>()),
            (string value, string result) =>
            {
                capturedValue = value;
                capturedResult = result;
            });

        var actualResult = mock.Object.GenericMethod("hello");

        /* Assert */

        Assert.Equal("hello", capturedValue);
        Assert.Equal("hello", capturedResult);
        Assert.Equal("hello", actualResult);
        Assert.Single(impl.CallLog);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Spy_MultipleMethodsOnSameProxy_ShouldWorkIndependently()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        /* Act */

        mock.Spy(m => m.Method1(), () => { });
        mock.Spy(m => m.Method3(It.IsAny<int>()), () => { });

        mock.Object.Method1();
        var result = mock.Object.Method3(5);

        /* Assert */

        Assert.Equal(6, result);
        Assert.Equal(2, impl.CallLog.Count);
        Assert.Equal("Method1", impl.CallLog[0]);
        Assert.Equal("Method3(5) -> 6", impl.CallLog[1]);

        mock.Verify(m => m.Method1(), Times.Once);
        mock.Verify(m => m.Method3(5), Times.Once);
    }

    [Fact]
    public void Spy_AfterMoqSetup_ShouldOverrideMoqSetup()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);

        // First set up a Moq override
        mock.Setup(m => m.Method3(5)).Returns(999);

        /* Act */

        // Now spy on it - this should override the Moq setup
        mock.Spy(m => m.Method3(It.IsAny<int>()), () => { });
        var result = mock.Object.Method3(5);

        /* Assert */

        // The spy should forward to the implementation, not return 999
        Assert.Equal(6, result);
        Assert.Single(impl.CallLog);
        Assert.Equal("Method3(5) -> 6", impl.CallLog[0]);
    }

    [Fact]
    public void Spy_CallbackOrder_UserCallbackThenForward()
    {
        /* Arrange */

        var impl = new Implementation();
        var mock = new Mock<IImplementation>();
        mock.SetupAsProxy(impl);
        var callOrder = new List<string>();

        /* Act */

        mock.Spy(
            m => m.Method3(It.IsAny<int>()),
            () => callOrder.Add("callback"));

        callOrder.Add("before-call");
        mock.Object.Method3(5);
        callOrder.Add("after-call");

        /* Assert */

        Assert.Equal(3, callOrder.Count);
        Assert.Equal("before-call", callOrder[0]);
        Assert.Equal("callback", callOrder[1]);
        Assert.Equal("after-call", callOrder[2]);
    }

    #endregion
}