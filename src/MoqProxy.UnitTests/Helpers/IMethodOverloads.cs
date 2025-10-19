// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IMethodOverloads
{
    void Process();

    void Process(int x);

    void Process(string s);

    void Process(int x, string s);
}