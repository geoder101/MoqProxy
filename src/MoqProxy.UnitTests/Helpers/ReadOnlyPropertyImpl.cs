// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public class ReadOnlyPropertyImpl : IReadOnlyProperty
{
    private int _value = 100;

    public int ReadOnlyProp => _value;

    public void SetValue(int value) => _value = value;
}