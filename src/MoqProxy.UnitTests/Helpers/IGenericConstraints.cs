// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public interface IGenericConstraints
{
    string ProcessArray<T>(T[] items);

    TResult ConvertAndCombine<TInput, TResult>(TInput input, Func<TInput, TResult> converter);

    T[] CreateArray<T>(T item, int count);

    string TransformString<T>(T value) where T : class;

    List<T> WrapInList<T>(T item);
}