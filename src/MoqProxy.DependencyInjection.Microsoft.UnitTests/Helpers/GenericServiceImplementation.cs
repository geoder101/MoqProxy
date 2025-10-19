// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

public class GenericServiceImplementation<T> : IGenericService<T>
{
    private T _value = default!;

    public T GetValue() => _value;

    public void SetValue(T value) => _value = value;
}