// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class RefOperations : IRefOperations
{
    public void Increment(ref int value)
    {
        value++;
    }

    public void Swap(ref int a, ref int b)
    {
        (a, b) = (b, a);
    }

    public int IncrementAndReturn(ref int value)
    {
        value++;
        return value;
    }
}