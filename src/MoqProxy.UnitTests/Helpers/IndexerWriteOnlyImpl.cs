// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public class IndexerWriteOnlyImpl : IIndexerWriteOnly
{
    public Dictionary<string, int> Data { get; } = new();

    public int this[string key]
    {
        set => Data[key] = value;
    }
}