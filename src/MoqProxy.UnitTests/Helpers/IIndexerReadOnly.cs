// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IIndexerReadOnly
{
    int this[string key] { get; }
}