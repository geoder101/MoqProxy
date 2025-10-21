// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.DependencyInjection.Microsoft.UnitTests.Helpers;

public interface IGenericService<T>
{
    T GetValue();

    void SetValue(T value);
}