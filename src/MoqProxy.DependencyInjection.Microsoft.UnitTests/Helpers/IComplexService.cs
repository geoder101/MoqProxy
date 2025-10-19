// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

public interface IComplexService
{
    Task<string> GetDataAsync();

    IEnumerable<int> GetNumbers();

    void ProcessData(List<string> items);
}