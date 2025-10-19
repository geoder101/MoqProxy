// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IIndexerWriteOnly
{
    int this[string key] { set; }
}