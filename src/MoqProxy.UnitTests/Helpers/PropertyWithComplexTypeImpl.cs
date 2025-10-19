// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos

namespace MoqProxy.UnitTests.Helpers;

public class PropertyWithComplexTypeImpl : IPropertyWithComplexType
{
    public List<string> ListProp { get; set; } = new();

    public Dictionary<int, string> DictProp { get; set; } = new();
}