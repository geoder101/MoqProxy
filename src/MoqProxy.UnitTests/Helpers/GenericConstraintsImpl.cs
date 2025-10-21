// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

namespace MoqProxy.UnitTests.Helpers;

public class GenericConstraintsImpl : IGenericConstraints
{
    public string ProcessArray<T>(T[] items)
    {
        return string.Join("", items);
    }

    public TResult ConvertAndCombine<TInput, TResult>(TInput input, Func<TInput, TResult> converter)
    {
        return converter(input);
    }

    public T[] CreateArray<T>(T item, int count)
    {
        var array = new T[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = item;
        }

        return array;
    }

    public string TransformString<T>(T value) where T : class
    {
        return value.ToString()!.ToUpper();
    }

    public List<T> WrapInList<T>(T item)
    {
        return new List<T> { item };
    }
}