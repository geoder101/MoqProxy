// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class Parser : IParser
{
    public bool TryParse(string input, out int result)
    {
        return int.TryParse(input, out result);
    }

    public bool TryParseDouble(string input, out double result)
    {
        return double.TryParse(input, out result);
    }
}