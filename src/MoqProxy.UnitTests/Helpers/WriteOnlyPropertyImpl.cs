// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class WriteOnlyPropertyImpl : IWriteOnlyProperty
{
    public int InternalValue { get; private set; }

    public int WriteOnlyProp
    {
        set => InternalValue = value;
    }
}