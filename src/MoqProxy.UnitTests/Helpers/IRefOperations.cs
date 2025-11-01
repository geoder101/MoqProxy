// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public interface IRefOperations
{
    void Increment(ref int value);
    void Swap(ref int a, ref int b);
    int IncrementAndReturn(ref int value);
}