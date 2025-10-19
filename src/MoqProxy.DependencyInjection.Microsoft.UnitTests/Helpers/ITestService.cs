// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

public interface ITestService
{
    int Counter { get; set; }

    string GetMessage();

    int Add(int a, int b);

    void DoWork();
}