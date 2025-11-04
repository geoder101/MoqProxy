// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.EventProxyTests;

public class EventTests
{
    [Fact]
    public void SetupAsProxy_EventHandlers_ShouldForward()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        var handler1Called = false;
        var handler2Called = false;
        var capturedCounter = 0;
        object? capturedSender1 = null;
        object? capturedSender2 = null;

        mock.Object.OnIncrement1 += (sender, _) =>
        {
            handler1Called = true;
            capturedSender1 = sender;
        };

        mock.Object.OnIncrement2 += (sender, args) =>
        {
            handler2Called = true;
            capturedCounter = args.Counter;
            capturedSender2 = sender;
        };

        /* Act */

        mock.Object.Increment();

        /* Assert */

        Assert.True(handler1Called, "OnIncrement1 handler should have been invoked");
        Assert.True(handler2Called, "OnIncrement2 handler should have been invoked");
        Assert.Equal(1, capturedCounter);
        Assert.Same(impl, capturedSender1);
        Assert.Same(impl, capturedSender2);
    }

    [Fact]
    public void SetupAsProxy_MultipleEventHandlers_ShouldForwardAll()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        var handler1Called = 0;
        var handler2Called = 0;
        var handler3Called = 0;

        EventHandler handler1 = (_, _) => handler1Called++;
        EventHandler handler2 = (_, _) => handler2Called++;
        EventHandler handler3 = (_, _) => handler3Called++;

        // Subscribe multiple handlers
        mock.Object.OnIncrement1 += handler1;
        mock.Object.OnIncrement1 += handler2;
        mock.Object.OnIncrement1 += handler3;

        /* Act */

        mock.Object.Increment();

        /* Assert */

        Assert.Equal(1, handler1Called);
        Assert.Equal(1, handler2Called);
        Assert.Equal(1, handler3Called);
    }

    [Fact]
    public void SetupAsProxy_SameHandlerMultipleTimes_ShouldInvokeMultipleTimes()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        var callCount = 0;
        EventHandler handler = (_, _) => callCount++;

        mock.Object.OnIncrement1 += handler;
        mock.Object.OnIncrement1 += handler;

        /* Act */

        mock.Object.Increment();

        /* Assert */

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void SetupAsProxy_UnsubscribeNonExistentHandler_ShouldNotThrow()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        EventHandler handler = (_, _) => { };

        /* Act & Assert */

        // Unsubscribing a handler that was never subscribed should not throw
        var exception = Record.Exception(() => mock.Object.OnIncrement1 -= handler);

        Assert.Null(exception);
    }

    [Fact]
    public void SetupAsProxy_EventWithNoSubscribers_ShouldNotThrow()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        /* Act & Assert */

        // Calling a method that raises events with no subscribers should not throw
        var exception = Record.Exception(() => mock.Object.Increment());

        Assert.Null(exception);
    }

    [Fact]
    public void SetupAsProxy_CustomEventDelegate_ShouldForward()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        var callCount = 0;
        var capturedCounter = 0;
        object? capturedSender = null;

        mock.Object.OnIncrement2 += (sender, args) =>
        {
            callCount++;
            capturedCounter = args.Counter;
            capturedSender = sender;
        };

        /* Act */

        mock.Object.Increment();
        mock.Object.Increment();

        /* Assert */

        Assert.Equal(2, callCount);
        Assert.Equal(2, capturedCounter);
        Assert.Equal(impl, capturedSender);
    }

    [Fact]
    public void SetupAsProxy_PartialUnsubscribe_ShouldOnlyRemoveSpecificHandler()
    {
        /* Arrange */

        var impl = new CounterEvents();
        var mock = new Mock<ICounterEvents>();
        mock.SetupAsProxy(impl);

        var handler1Called = 0;
        var handler2Called = 0;

        EventHandler handler1 = (_, _) => handler1Called++;
        EventHandler handler2 = (_, _) => handler2Called++;

        mock.Object.OnIncrement1 += handler1;
        mock.Object.OnIncrement1 += handler2;

        /* Act */

        mock.Object.Increment();
        Assert.Equal(1, handler1Called);
        Assert.Equal(1, handler2Called);

        // Unsubscribe only handler1
        mock.Object.OnIncrement1 -= handler1;
        mock.Object.Increment();

        /* Assert */

        Assert.Equal(1, handler1Called); // Should not increment
        Assert.Equal(2, handler2Called); // Should increment
    }
}