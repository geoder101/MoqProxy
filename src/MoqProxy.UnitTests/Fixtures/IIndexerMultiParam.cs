// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Fixtures;

public interface IIndexerMultiParam
{
    string this[int row, int col] { get; set; }
}