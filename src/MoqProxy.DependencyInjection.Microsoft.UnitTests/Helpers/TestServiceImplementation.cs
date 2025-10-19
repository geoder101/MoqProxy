// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

public class TestServiceImplementation : ITestService
{
    private int _counter;

    public int Counter
    {
        get => _counter;
        set => _counter = value;
    }

    public string GetMessage() => "Hello from implementation";

    public int Add(int a, int b) => a + b;

    public void DoWork()
    {
        _counter++;
    }
}