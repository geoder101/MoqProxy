// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Fixtures;

public interface IMultipleProperties
{
    int Prop1 { get; set; }

    string Prop2 { get; set; }

    bool Prop3 { get; set; }

    double Prop4 { get; set; }
}