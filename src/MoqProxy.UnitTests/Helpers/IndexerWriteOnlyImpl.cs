// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class IndexerWriteOnlyImpl : IIndexerWriteOnly
{
    public Dictionary<string, int> Data { get; } = new();

    public int this[string key]
    {
        set => Data[key] = value;
    }
}