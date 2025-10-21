// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class MethodWithMultipleParametersImpl : IMethodWithMultipleParameters
{
    public int Add(int a, int b, int c) => a + b + c;

    public string Concat(string s1, string s2, string s3, string s4) => s1 + s2 + s3 + s4;
}