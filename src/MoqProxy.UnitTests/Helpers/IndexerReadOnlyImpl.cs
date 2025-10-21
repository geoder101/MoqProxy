// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class IndexerReadOnlyImpl : IIndexerReadOnly
{
    private readonly Dictionary<string, int> _data = new()
    {
        ["key1"] = 100,
        ["key2"] = 200
    };

    public int this[string key] => _data.TryGetValue(key, out var value) ? value : -1;
}