// SPDX-License-Identifier: MIT
// Copyright (c) 2025 George Dernikos <geoder101@gmail.com>

using Moq;
using MoqProxy;

Console.WriteLine("=== MoqProxy Demo ===");
Console.WriteLine("This demo showcases how MoqProxy allows you to proxy real implementations with Moq mocks.");
Console.WriteLine();

// ========================================================================================
// SECTION 1: Basic Proxying - Methods, Properties, and Generic Methods
// ========================================================================================
Console.WriteLine("--- Section 1: Basic Proxying ---");
var impl = new Implementation();
var mock = new Mock<IImplementation>();

// Wire the mock so it proxies calls and property accessors to the real impl
mock.SetupAsProxy(impl);

// Test property forwarding
Console.WriteLine($"Property forwarding - Before: impl.Property={impl.Property}, mock.Property={mock.Object.Property}");
mock.Object.Property = 100;
Console.WriteLine(
    $"Property forwarding - After set: impl.Property={impl.Property}, mock.Property={mock.Object.Property}");

// Test method forwarding
mock.Object.Method1();
mock.Object.Method2(5);
Console.WriteLine($"Method3 returned: {mock.Object.Method3(7)}");

// Test generic method forwarding
Console.WriteLine($"GenericMethod<string> returned: {mock.Object.GenericMethod("Hello")}");
Console.WriteLine($"GenericMethod<int> returned: {mock.Object.GenericMethod(42)}");

// Test async method forwarding
await mock.Object.Method1Async();
await mock.Object.Method2Async(9);
Console.WriteLine($"Method3Async returned: {await mock.Object.Method3Async(11)}");

Console.WriteLine();

// ========================================================================================
// SECTION 2: Ref/Out Parameter Support
// ========================================================================================
Console.WriteLine("--- Section 2: Ref/Out Parameter Support ---");
var parserImpl = new Parser();
var parserMock = new Mock<IParser>();
parserMock.SetupAsProxy(parserImpl);

// Test out parameter forwarding
var parseSuccess = parserMock.Object.TryParse("123", out var parsedValue);
Console.WriteLine($"TryParse('123') -> Success: {parseSuccess}, Value: {parsedValue}");

parseSuccess = parserMock.Object.TryParse("invalid", out parsedValue);
Console.WriteLine($"TryParse('invalid') -> Success: {parseSuccess}, Value: {parsedValue}");

// Test ref parameter forwarding
var refOpsImpl = new RefOperations();
var refOpsMock = new Mock<IRefOperations>();
refOpsMock.SetupAsProxy(refOpsImpl);

var refValue = 10;
refOpsMock.Object.Increment(ref refValue);
Console.WriteLine($"After Increment(ref 10): {refValue}");

var a = 5;
var b = 15;
refOpsMock.Object.Swap(ref a, ref b);
Console.WriteLine($"After Swap(ref 5, ref 15): a={a}, b={b}");

Console.WriteLine();

// ========================================================================================
// SECTION 3: Indexer Support
// ========================================================================================
Console.WriteLine("--- Section 3: Indexer Support ---");
var storageImpl = new Storage();
storageImpl[0] = "Item0";
storageImpl[1] = "Item1";
storageImpl[2] = "Item2";

var storageMock = new Mock<IStorage>();
storageMock.SetupAsProxy(storageImpl);

Console.WriteLine($"Storage[0] = {storageMock.Object[0]}");
Console.WriteLine($"Storage[1] = {storageMock.Object[1]}");
Console.WriteLine($"Storage[2] = {storageMock.Object[2]}");

Console.WriteLine();

// ========================================================================================
// SECTION 4: Override Behavior with Moq Setups
// ========================================================================================
Console.WriteLine("--- Section 4: Override Behavior with Moq Setups ---");
Console.WriteLine("You can still use standard Moq setups to override specific behaviors:");

// Override specific behaviors while keeping the rest proxied
mock.SetupGet(m => m.Property).Returns(999);
mock.Setup(m => m.Method1()).Callback(() => Console.WriteLine("  [OVERRIDDEN] Method1 called"));
mock.Setup(m => m.Method3(It.IsAny<int>())).Returns<int>(x => x * 10);
mock.Setup(m => m.GenericMethod(It.IsAny<string>())).Returns<string>(s => s.ToUpper());

Console.WriteLine($"Property (overridden): {mock.Object.Property}");
mock.Object.Method1(); // Uses the overridden callback
Console.WriteLine($"Method2(5) still forwarded:");
mock.Object.Method2(5); // Still forwarded to impl
Console.WriteLine($"Method3(7) overridden: {mock.Object.Method3(7)}"); // Overridden
Console.WriteLine($"GenericMethod<string>('hello') overridden: {mock.Object.GenericMethod("hello")}");

Console.WriteLine();

// ========================================================================================
// SECTION 5: Verification
// ========================================================================================
Console.WriteLine("--- Section 5: Verification ---");
Console.WriteLine("You can verify interactions even with proxied calls:");

// Reset and setup fresh mock
mock.Reset();
mock.SetupAsProxy(impl);

// Make some calls
mock.Object.Method1();
mock.Object.Method2(5);
mock.Object.Method2(10);
mock.Object.Method3(7);

// Verify the calls
mock.Verify(m => m.Method1(), Times.Once);
mock.Verify(m => m.Method2(5), Times.Once);
mock.Verify(m => m.Method2(10), Times.Once);
mock.Verify(m => m.Method2(It.IsAny<int>()), Times.Exactly(2));
mock.Verify(m => m.Method3(7), Times.Once);

Console.WriteLine("✓ All verifications passed!");

Console.WriteLine();

// ========================================================================================
// SECTION 6: Reset and Re-proxy
// ========================================================================================
Console.WriteLine("--- Section 6: Reset and Re-proxy ---");
mock.Reset();
mock.SetupAsProxy(impl);
Console.WriteLine("Mock reset and re-proxied. Calling Method1:");
mock.Object.Method1();

Console.WriteLine();
Console.WriteLine("=== Demo Complete ===");

#pragma warning disable CA1050
// ========================================================================================
// Interface and Implementation Definitions
// ========================================================================================

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
            Console.WriteLine($"  Impl.Property.get -> {_prop}");
            return _prop;
        }
        set
        {
            Console.WriteLine($"  Impl.Property.set <- {value}");
            _prop = value;
        }
    }

    public void Method1()
    {
        Console.WriteLine($"  Impl.{nameof(Method1)}()");
    }

    public void Method2(int x)
    {
        Console.WriteLine($"  Impl.{nameof(Method2)}({x})");
    }

    public int Method3(int x)
    {
        Console.WriteLine($"  Impl.{nameof(Method3)}({x}) -> {x + 1}");
        return x + 1;
    }

    public async Task Method1Async()
    {
        await Task.CompletedTask;
        Console.WriteLine($"  Impl.{nameof(Method1Async)}()");
    }

    public async Task Method2Async(int x)
    {
        await Task.CompletedTask;
        Console.WriteLine($"  Impl.{nameof(Method2Async)}({x})");
    }

    public async Task<int> Method3Async(int x)
    {
        await Task.CompletedTask;
        Console.WriteLine($"  Impl.{nameof(Method3Async)}({x}) -> {x + 1}");
        return x + 1;
    }

    public T GenericMethod<T>(T value)
    {
        Console.WriteLine($"  Impl.{nameof(GenericMethod)}<{typeof(T).Name}>({value}) -> {value}");
        return value;
    }
}

// ========================================================================================

public interface IParser
{
    bool TryParse(string input, out int result);
}

public class Parser : IParser
{
    public bool TryParse(string input, out int result)
    {
        var success = int.TryParse(input, out result);
        Console.WriteLine($"  Impl.TryParse('{input}') -> Success: {success}, Result: {result}");
        return success;
    }
}

// ========================================================================================

public interface IRefOperations
{
    void Increment(ref int value);
    void Swap(ref int a, ref int b);
}

public class RefOperations : IRefOperations
{
    public void Increment(ref int value)
    {
        Console.WriteLine($"  Impl.Increment(ref {value})");
        value++;
    }

    public void Swap(ref int a, ref int b)
    {
        Console.WriteLine($"  Impl.Swap(ref {a}, ref {b})");
        (a, b) = (b, a);
    }
}

// ========================================================================================

public interface IStorage
{
    string this[int index] { get; set; }
}

public class Storage : IStorage
{
    private readonly Dictionary<int, string> _storage = new();

    public string this[int index]
    {
        get
        {
            _storage.TryGetValue(index, out var value);
            Console.WriteLine($"  Impl.Storage[{index}].get -> {value}");
            return value ?? string.Empty;
        }
        set
        {
            Console.WriteLine($"  Impl.Storage[{index}].set <- {value}");
            _storage[index] = value;
        }
    }
}

#pragma warning restore CA1050