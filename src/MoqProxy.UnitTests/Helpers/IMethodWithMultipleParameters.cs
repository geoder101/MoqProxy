// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests;

public interface IMethodWithMultipleParameters
{
    int Add(int a, int b, int c);

    string Concat(string s1, string s2, string s3, string s4);
}