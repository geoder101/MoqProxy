// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public interface IPropertyWithComplexType
{
    List<string> ListProp { get; set; }

    Dictionary<int, string> DictProp { get; set; }
}