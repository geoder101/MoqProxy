// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public class IndexerSingleParamImpl : IIndexerSingleParam
{
    private readonly Dictionary<int, int> _data = new();

    public int this[int index]
    {
        get => _data.TryGetValue(index, out var value) ? value : 0;
        set => _data[index] = value;
    }
}