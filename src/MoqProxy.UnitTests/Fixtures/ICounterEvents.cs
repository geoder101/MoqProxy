// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Fixtures;

public interface ICounterEvents
{
    public event EventHandler OnIncrement1;

    public event CounterIncrementEventHandler OnIncrement2;

    void Increment();
}