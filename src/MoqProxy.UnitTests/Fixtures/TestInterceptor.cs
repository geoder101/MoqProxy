// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using IInvocation = Castle.DynamicProxy.IInvocation;

namespace MoqProxy.UnitTests.Fixtures;

public class TestInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
    }
}