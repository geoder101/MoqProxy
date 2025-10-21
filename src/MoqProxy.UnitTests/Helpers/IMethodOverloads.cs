// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public interface IMethodOverloads
{
    void Process();

    void Process(int x);

    void Process(string s);

    void Process(int x, string s);
}