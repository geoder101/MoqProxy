// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IIndexerMultiParam
{
    string this[int row, int col] { get; set; }
}