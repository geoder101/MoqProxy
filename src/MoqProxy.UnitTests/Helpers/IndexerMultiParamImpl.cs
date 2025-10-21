// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class IndexerMultiParamImpl : IIndexerMultiParam
{
    private readonly Dictionary<(int, int), string> _data = new();

    public string this[int row, int col]
    {
        get => _data.TryGetValue((row, col), out var value) ? value : string.Empty;
        set => _data[(row, col)] = value;
    }
}