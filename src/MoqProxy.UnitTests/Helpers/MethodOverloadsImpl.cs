// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public class MethodOverloadsImpl : IMethodOverloads
{
    public List<string> CallLog { get; } = new();

    public void Process()
    {
        CallLog.Add("Process()");
    }

    public void Process(int x)
    {
        CallLog.Add($"Process({x})");
    }

    public void Process(string s)
    {
        CallLog.Add($"Process({s})");
    }

    public void Process(int x, string s)
    {
        CallLog.Add($"Process({x}, {s})");
    }
}