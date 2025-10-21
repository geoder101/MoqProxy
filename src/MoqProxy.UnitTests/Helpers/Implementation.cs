// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class Implementation : IImplementation
{
    private int _prop = 42;

    public List<string> CallLog { get; } = new();

    public int Property
    {
        get
        {
            CallLog.Add($"Prop.get -> {_prop}");
            return _prop;
        }
        set
        {
            CallLog.Add($"Prop.set <- {value}");
            _prop = value;
        }
    }

    public void Method1()
    {
        CallLog.Add(nameof(Method1));
    }

    public void Method2(int x)
    {
        CallLog.Add($"{nameof(Method2)}({x})");
    }

    public int Method3(int x)
    {
        CallLog.Add($"{nameof(Method3)}({x})");
        return x + 1;
    }

    public async Task Method1Async()
    {
        await Task.CompletedTask;
        CallLog.Add(nameof(Method1Async));
    }

    public async Task Method2Async(int x)
    {
        await Task.CompletedTask;
        CallLog.Add($"{nameof(Method2Async)}({x})");
    }

    public async Task<int> Method3Async(int x)
    {
        await Task.CompletedTask;
        CallLog.Add($"{nameof(Method3Async)}({x})");
        return x + 1;
    }

    public T GenericMethod<T>(T value)
    {
        CallLog.Add($"{nameof(GenericMethod)}({value})");
        return value;
    }
}