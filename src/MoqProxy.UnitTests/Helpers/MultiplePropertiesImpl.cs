// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class MultiplePropertiesImpl : IMultipleProperties
{
    public int Prop1 { get; set; }

    public string Prop2 { get; set; } = string.Empty;

    public bool Prop3 { get; set; }

    public double Prop4 { get; set; }
}