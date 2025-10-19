// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IGenericMethods
{
    T Identity<T>(T value);

    List<T> CreateList<T>(T item);
}