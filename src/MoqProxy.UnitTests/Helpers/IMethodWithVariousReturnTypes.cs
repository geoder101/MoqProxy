// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IMethodWithVariousReturnTypes
{
    bool GetBool();

    double GetDouble();

    string GetString();

    object GetObject();

    List<int> GetList();
}