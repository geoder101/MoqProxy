// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public interface IImplementation
{
    public int Property { get; set; }

    void Method1();

    void Method2(int x);

    int Method3(int x);

    T GenericMethod<T>(T value);

    Task Method1Async();

    Task Method2Async(int x);

    Task<int> Method3Async(int x);
}