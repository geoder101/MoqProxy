// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public class GenericMethodsImpl : IGenericMethods
{
    public T Identity<T>(T value) => value;

    public List<T> CreateList<T>(T item) => new List<T> { item };
}