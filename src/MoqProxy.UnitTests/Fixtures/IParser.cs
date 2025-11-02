// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Fixtures;

public interface IParser
{
    bool TryParse(string input, out int result);
    bool TryParseDouble(string input, out double result);
}