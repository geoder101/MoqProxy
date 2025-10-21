// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class ReadOnlyPropertyImpl : IReadOnlyProperty
{
    private int _value = 100;

    public int ReadOnlyProp => _value;

    public void SetValue(int value) => _value = value;
}