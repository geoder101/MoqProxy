// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public interface IMethodWithVariousReturnTypes
{
    bool GetBool();

    double GetDouble();

    string GetString();

    object GetObject();

    List<int> GetList();
}