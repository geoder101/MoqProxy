// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Fixtures;

public class CounterEvents : ICounterEvents
{
    private int _counter;

    public event EventHandler? OnIncrement1;
    public event CounterIncrementEventHandler? OnIncrement2;

    public void Increment()
    {
        _counter++;
        OnIncrement1?.Invoke(this, EventArgs.Empty);
        OnIncrement2?.Invoke(this, new CounterIncrementEventArgs { Counter = _counter });
    }
}