// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

public class ComplexServiceImplementation : IComplexService
{
    public async Task<string> GetDataAsync()
    {
        await Task.Delay(1);
        return "Async data";
    }

    public IEnumerable<int> GetNumbers()
    {
        return new[] { 1, 2, 3, 4, 5 };
    }

    public void ProcessData(List<string> items)
    {
        items.Add("Processed");
    }
}