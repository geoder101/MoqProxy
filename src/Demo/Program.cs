// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy;

var impl = new Implementation();
var mock = new Mock<IImplementation>();

// Wire the mock so it proxies calls and property accessors to the real impl
mock.SetupAsProxy(impl);

// Exercise the mock (demonstration that forwarding works)
Console.WriteLine($"Before set: impl.Prop={impl.Property}, mock.Prop={mock.Object.Property}");
mock.Object.Property = 100;
Console.WriteLine($"After set: impl.Prop={impl.Property}, mock.Prop={mock.Object.Property}");

mock.Object.Method1();
mock.Object.Method2(5);
Console.WriteLine($"M3 returned: {mock.Object.Method3(7)}");
Console.WriteLine($"GenericMethod<string> returned: {mock.Object.GenericMethod("Hello")}");
Console.WriteLine($"GenericMethod<int> returned: {mock.Object.GenericMethod(42)}");

await mock.Object.Method1Async();
await mock.Object.Method2Async(9);
Console.WriteLine($"MA3 returned: {await mock.Object.Method3Async(11)}");

Console.WriteLine();
Console.WriteLine("After mocking:");
// Now set up some mock behavior to demonstrate that it works as expected
mock.SetupGet(m => m.Property).Returns(999);
mock.Setup(m => m.Method1()).Callback(() => Console.WriteLine("Mocked Method1 called"));
mock.Setup(m => m.Method2(It.IsAny<int>())).Callback<int>(x => Console.WriteLine($"Mocked Method2 called with {x}"));
mock.Setup(m => m.Method3(It.IsAny<int>())).Returns<int>(x => x * 10);
mock.Setup(m => m.GenericMethod(It.IsAny<string>())).Returns<string>(s => s.ToUpper());
mock.Setup(m => m.GenericMethod(It.IsAny<int>())).Returns<int>(x => x * 100);
mock.Setup(m => m.Method1Async()).Callback(() => Console.WriteLine("Mocked Method1Async called"));
mock.Setup(m => m.Method2Async(It.IsAny<int>()))
    .Callback<int>(x => Console.WriteLine($"Mocked Method2Async called with {x}"));
mock.Setup(m => m.Method3Async(It.IsAny<int>())).ReturnsAsync((int x) => x * 20);

Console.WriteLine($"Before set: impl.Prop={impl.Property}, mock.Prop={mock.Object.Property}");
mock.Object.Property = 200;
Console.WriteLine($"After set: impl.Prop={impl.Property}, mock.Prop={mock.Object.Property}");

mock.Object.Method1();
mock.Object.Method2(5);
Console.WriteLine($"Method3 returned: {mock.Object.Method3(7)}");
Console.WriteLine($"GenericMethod<string> returned: {mock.Object.GenericMethod("Hello")}");
Console.WriteLine($"GenericMethod<int> returned: {mock.Object.GenericMethod(42)}");

await mock.Object.Method1Async();
await mock.Object.Method2Async(9);
Console.WriteLine($"Method3Async returned: {await mock.Object.Method3Async(11)}");

Console.WriteLine();
Console.WriteLine("After resetting mock:");
mock.Reset();
mock.SetupAsProxy(impl);
mock.Object.Method1();

#pragma warning disable CA1050
public interface IImplementation
{
    public int Property { get; set; }

    void Method1();

    void Method2(int x);

    int Method3(int x);

    T GenericMethod<T>(T value);

    Task Method1Async();

    Task Method2Async(int x);

    Task<int> Method3Async(int x);
}

public class Implementation : IImplementation
{
    private int _prop = 42;

    public int Property
    {
        get
        {
            Console.WriteLine($"Prop.get -> {_prop}");
            return _prop;
        }
        set
        {
            Console.WriteLine($"Prop.set <- {value}");
            _prop = value;
        }
    }

    public void Method1()
    {
        Console.WriteLine(nameof(Method1));
    }

    public void Method2(int x)
    {
        Console.WriteLine(nameof(Method2));
        Console.WriteLine(x);
    }

    public int Method3(int x)
    {
        Console.WriteLine(nameof(Method3));
        Console.WriteLine(x);
        return x + 1;
    }

    public async Task Method1Async()
    {
        await Task.CompletedTask;
        Console.WriteLine(nameof(Method1Async));
    }

    public async Task Method2Async(int x)
    {
        await Task.CompletedTask;
        Console.WriteLine(nameof(Method2Async));
        Console.WriteLine(x);
    }

    public async Task<int> Method3Async(int x)
    {
        await Task.CompletedTask;
        Console.WriteLine(nameof(Method3Async));
        Console.WriteLine(x);
        return x + 1;
    }

    public T GenericMethod<T>(T value)
    {
        Console.WriteLine(nameof(GenericMethod));
        Console.WriteLine(value);
        return value;
    }
}
#pragma warning restore CA1050