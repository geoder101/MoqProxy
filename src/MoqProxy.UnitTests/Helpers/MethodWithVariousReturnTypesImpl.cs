// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class MethodWithVariousReturnTypesImpl : IMethodWithVariousReturnTypes
{
    public bool GetBool() => true;

    public double GetDouble() => 3.14;

    public string GetString() => "test";

    public object GetObject() => new { Value = 42 };

    public List<int> GetList() => [1, 2, 3];
}