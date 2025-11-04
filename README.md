# MoqProxy

[![NuGet](https://img.shields.io/nuget/v/geoder101.MoqProxy.svg)](https://www.nuget.org/packages/geoder101.MoqProxy/)

A powerful extension for [Moq](https://github.com/devlooped/moq) that enables **proxy pattern mocking** - forward calls
from a mock to a real implementation while maintaining full verification capabilities.

## Why MoqProxy?

MoqProxy bridges the gap between full mocking and real implementations, giving you the best of both worlds:

- **Verify interactions** - Use Moq's `Verify()` to assert method calls on the real implementation
- **Selective overrides** - Override specific methods/properties while forwarding everything else
- **Integration testing** - Test decorators and wrappers with real dependencies
- **Spy pattern** - Observe and verify behavior without changing it
- **Works with interfaces AND classes** - Unlike `CallBase`, works seamlessly with interface mocks

## How is this different from `CallBase = true`?

| Feature                      | MoqProxy (`SetupAsProxy`)                  | `CallBase = true`                             |
|------------------------------|--------------------------------------------|-----------------------------------------------|
| **Use case**                 | ✅ Spy on existing objects, test decorators | ⚠️ Partial mocking of concrete classes        |
| **Works with interfaces**    | ✅ Yes - forwards to any implementation     | ❌ No - interfaces have no base implementation |
| **Separate implementation**  | ✅ Forwards to a different instance         | ❌ Only calls the mock's own base methods      |
| **Property synchronization** | ✅ Mock and implementation stay in sync     | ⚠️ Only if mock is the implementation         |
| **Event forwarding**         | ✅ Event subscriptions forwarded            | ⚠️ Only if mock is the implementation         |
| **Generic method support**   | ✅ Full support via custom interceptor      | ✅ Supported                                   |
| **Indexer support**          | ✅ 1-2 parameter indexers                   | ✅ Supported                                   |

**Key Difference:** `CallBase = true` only works with **abstract or virtual members of the mocked class itself**.
`SetupAsProxy` works with **interfaces** and forwards calls to a **separate implementation instance**, making it perfect
for the spy pattern and testing decorators.

### Example Comparison

```csharp
// ❌ This DOESN'T work - interface has no base implementation
var mock = new Mock<ICalculator> { CallBase = true };
mock.Object.Add(2, 3); // Throws - no implementation!

// ✅ This DOES work - forwards to real implementation
var realCalc = new Calculator();
var mock = new Mock<ICalculator>();
mock.SetupAsProxy(realCalc);
mock.Object.Add(2, 3); // Returns 5, calls realCalc.Add(2, 3)
```

## Installation

```bash
dotnet add package geoder101.MoqProxy
```

### Microsoft Dependency Injection Integration

For ASP.NET Core and Microsoft.Extensions.DependencyInjection scenarios, install the integration package:

[![NuGet](https://img.shields.io/nuget/v/geoder101.MoqProxy.DependencyInjection.Microsoft.svg)](https://www.nuget.org/packages/geoder101.MoqProxy.DependencyInjection.Microsoft/)

```bash
dotnet add package geoder101.MoqProxy.DependencyInjection.Microsoft
```

This package allows you to wrap services registered in your DI container with Moq proxies, making it easy to verify
calls and spy on real implementations in integration tests. See
the [package README](src/MoqProxy.DependencyInjection.Microsoft/README.md) for details.

## Quick Start

```csharp
using Moq;
using MoqProxy;

// Create a mock and a real implementation
var realService = new MyService();
var mock = new Mock<IMyService>();

// Set up the mock to proxy all calls to the real implementation
mock.SetupAsProxy(realService);

// Use the mock - calls are forwarded to realService
mock.Object.DoSomething();

// Verify the call was made
mock.Verify(m => m.DoSomething(), Times.Once);
```

## Features

### ✅ Properties

- Read-only properties
- Write-only properties
- Read-write properties
- Complex type properties (collections, dictionaries, etc.)
- Null value handling
- State synchronization - changes to mock properties are reflected in the implementation and vice versa

### ✅ Events

- Event subscription (`+=`) forwarding to implementation
- Event unsubscription (`-=`) forwarding to implementation
- Standard `EventHandler` and `EventHandler<TEventArgs>` patterns
- Custom delegate types
- Multiple handlers on the same event
- Events raised by implementation invoke handlers subscribed to mock

### ✅ Methods

- Void methods
- Methods with return values
- Methods with 0-4+ parameters
- Method overloads
- Generic methods - full support including type inference
- Async methods - `Task` and `Task<T>`
- Ref/out parameters - automatic forwarding with verification support
- Various return types (primitives, objects, collections, etc.)

### ✅ Indexers

- Single-parameter indexers (`this[int index]`)
- Multi-parameter indexers (`this[int x, int y]`)
- Read-only indexers
- Write-only indexers (limited support due to Moq constraints)

### ✅ Advanced Features

- Selective override - Override specific behaviors while keeping others proxied
- Mock reset - Call `mock.Reset()` then `SetupAsProxy()` again to restore proxying
- Multiple instances - Proxy multiple implementations with different mocks
- Custom interceptor - Uses Castle.DynamicProxy for edge cases

## Usage Examples

### Basic Proxying

```csharp
public interface ICalculator
{
    int Add(int x, int y);
}

public class Calculator : ICalculator
{
    public int Add(int x, int y) => x + y;
}

// Test
var impl = new Calculator();
var mock = new Mock<ICalculator>();
mock.SetupAsProxy(impl);

var result = mock.Object.Add(2, 3);
Assert.Equal(5, result);

mock.Verify(m => m.Add(2, 3), Times.Once);
```

### Selective Override

```csharp
var impl = new Calculator();
var mock = new Mock<ICalculator>();
mock.SetupAsProxy(impl);

// Override specific behavior
mock.Setup(m => m.Add(2, 3)).Returns(100);

// This call uses the override
Assert.Equal(100, mock.Object.Add(2, 3));

// Other calls are forwarded to the real implementation
Assert.Equal(7, mock.Object.Add(3, 4));
```

### Testing Decorators

This is where MoqProxy really shines - testing decorator patterns:

```csharp
public class CachingCalculatorDecorator : ICalculator
{
    private readonly ICalculator _inner;
    private readonly Dictionary<(int, int), int> _cache = new();

    public CachingCalculatorDecorator(ICalculator inner)
    {
        _inner = inner;
    }

    public int Add(int x, int y)
    {
        if (_cache.TryGetValue((x, y), out var cached))
            return cached;

        var result = _inner.Add(x, y);
        _cache[(x, y)] = result;
        return result;
    }
}

// Test
var impl = new Calculator();
var mock = new Mock<ICalculator>();
mock.SetupAsProxy(impl);

var decorator = new CachingCalculatorDecorator(mock.Object);

// First call - should call through
decorator.Add(2, 3);
mock.Verify(m => m.Add(2, 3), Times.Once);

// Second call - should be cached
decorator.Add(2, 3);
mock.Verify(m => m.Add(2, 3), Times.Once); // Still once - decorator cached it!
```

### Async Methods

```csharp
public interface IAsyncService
{
    Task<string> GetDataAsync(int id);
    Task ProcessAsync();
}

public class AsyncService : IAsyncService
{
    public async Task<string> GetDataAsync(int id)
    {
        await Task.Delay(100); // Simulate async work
        return $"Data for ID: {id}";
    }

    public async Task ProcessAsync()
    {
        await Task.Delay(100); // Simulate async processing
    }
}

var impl = new AsyncService();
var mock = new Mock<IAsyncService>();
mock.SetupAsProxy(impl);

var result = await mock.Object.GetDataAsync(42);
await mock.Object.ProcessAsync();

mock.Verify(m => m.GetDataAsync(42), Times.Once);
mock.Verify(m => m.ProcessAsync(), Times.Once);
```

### Properties with State Synchronization

```csharp
public interface IConfig
{
    string ConnectionString { get; set; }
}

public class Config : IConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

var impl = new Config { ConnectionString = "Server=localhost" };
var mock = new Mock<IConfig>();
mock.SetupAsProxy(impl);

// Get property
Assert.Equal("Server=localhost", mock.Object.ConnectionString);

// Set property through mock
mock.Object.ConnectionString = "Server=production";

// Change is reflected in the implementation
Assert.Equal("Server=production", impl.ConnectionString);

// Both mock and impl are synchronized
Assert.Equal(impl.ConnectionString, mock.Object.ConnectionString);
```

### Indexers

```csharp
public interface IMatrix
{
    int this[int x, int y] { get; set; }
}

var impl = new Matrix();
var mock = new Mock<IMatrix>();
mock.SetupAsProxy(impl);

// Set through indexer
impl[0, 0] = 42; // Caution: `mock.Object[0, 0] = 42;` would not forward to impl due to how Moq handles indexer setters

// Get through indexer
var value = mock.Object[0, 0];

Assert.Equal(42, value);
Assert.Equal(42, impl[0, 0]); // Synchronized
```

### Events

```csharp
public class DataEventArgs : EventArgs
{
    public string Data { get; set; } = string.Empty;
}

public interface INotifier
{
    event EventHandler? StatusChanged;
    event EventHandler<DataEventArgs>? DataReceived;
    void UpdateStatus();
    void NotifyData(string data);
}

public class Notifier : INotifier
{
    public event EventHandler? StatusChanged;
    public event EventHandler<DataEventArgs>? DataReceived;

    public void UpdateStatus()
    {
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyData(string data)
    {
        DataReceived?.Invoke(this, new DataEventArgs { Data = data });
    }
}

var impl = new Notifier();
var mock = new Mock<INotifier>();
mock.SetupAsProxy(impl);

var statusChangedCount = 0;
var receivedData = new List<string>();

// Subscribe to events on the mock
mock.Object.StatusChanged += (sender, e) => statusChangedCount++;
mock.Object.DataReceived += (sender, e) => receivedData.Add(e.Data);

// When implementation raises events, handlers subscribed to mock are invoked
mock.Object.UpdateStatus(); // Raises StatusChanged

Assert.Equal(1, statusChangedCount);

// Works with custom event args
mock.Object.NotifyData("Hello"); // Raises DataReceived

Assert.Single(receivedData);
Assert.Equal("Hello", receivedData[0]);

// Verify event-related interactions if needed
mock.Verify(m => m.UpdateStatus(), Times.Once);
```

### Generic Methods

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IRepository
{
    T GetById<T>(int id) where T : class;
    void Save<T>(T entity) where T : class;
}

public class Repository : IRepository
{
    public T GetById<T>(int id) where T : class
    {
        // Simulate fetching from a data source
        return (Activator.CreateInstance(typeof(T)) as T)!;
    }

    public void Save<T>(T entity) where T : class
    {
        // Simulate saving to a data source
    }
}

var impl = new Repository();
var mock = new Mock<IRepository>();
mock.SetupAsProxy(impl);

var user = mock.Object.GetById<User>(123);
mock.Object.Save(user);

mock.Verify(m => m.GetById<User>(123), Times.Once);
mock.Verify(m => m.Save(user), Times.Once);
```

### Ref/Out Parameters

```csharp
public interface IParser
{
    bool TryParse(string input, out int result);
    void Increment(ref int value);
}

public class Parser : IParser
{
    public bool TryParse(string input, out int result)
    {
        return int.TryParse(input, out result);
    }

    public void Increment(ref int value)
    {
        value++;
    }
}

var impl = new Parser();
var mock = new Mock<IParser>();
mock.SetupAsProxy(impl);

// Out parameters are automatically forwarded
var success = mock.Object.TryParse("123", out var value);
Assert.True(success);
Assert.Equal(123, value);

// Ref parameters work too
int number = 5;
mock.Object.Increment(ref number);
Assert.Equal(6, number);

// Verify calls with It.Ref<T>.IsAny
mock.Verify(m => m.TryParse("123", out It.Ref<int>.IsAny), Times.Once);
mock.Verify(m => m.Increment(ref It.Ref<int>.IsAny), Times.Once);
```

## Advanced Scenarios

### Reset and Reapply

```csharp
var impl = new Calculator();
var mock = new Mock<ICalculator>();
mock.SetupAsProxy(impl);

// Use the mock...
mock.Object.Add(2, 3);

// Override some behavior
mock.Setup(m => m.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(999);

// Reset and reapply proxying
mock.Reset();
mock.SetupAsProxy(impl);

// Now back to forwarding to real implementation
Assert.Equal(5, mock.Object.Add(2, 3));
```

## Limitations

- **Ref/out parameters**: Methods with ref/out parameters are automatically forwarded to the implementation. You can
  verify calls using `It.Ref<T>.IsAny`, but cannot verify specific out values (Moq limitation).
- **By-ref structs** (e.g., `Span<T>`, `ReadOnlySpan<T>`): Not supported due to C# expression tree limitations
- **Indexers with 3+ parameters**: Limited support due to implementation complexity
- **Write-only indexers**: Have limited support due to Moq API constraints

## How It Works

MoqProxy uses a sophisticated approach to enable proxy mocking:

1. **Reflection & Expression Trees**: Dynamically inspects the mocked type and creates Moq setups using expression trees
   for properties, methods, and indexers
2. **Generic Method Handling**: Uses `MethodInfo.Invoke` for generic methods that can't be represented in expression
   trees
3. **Custom Interceptor**: Injects a Castle.DynamicProxy interceptor to handle edge cases and ensure all calls are
   forwarded
4. **Sentinel Pattern**: Uses a special `NullReturnValue` sentinel to detect when no explicit setup was matched,
   triggering fallback to the real implementation

The library handles complex scenarios including:

- Method overloads with different signatures
- Generic methods with type inference
- Multi-parameter indexers
- Async/await patterns
- Property state synchronization

## Requirements

- **.NET 8.0 or later** - The library targets .NET 8.0
- **Moq 4.20.72 or later** - Core mocking framework
- **Castle.Core** - Dependency of Moq, used for dynamic proxy generation

## Project Structure

```text
MoqProxy/
├── src/
│   ├── MoqProxy/                                          # Core library
│   │   ├── MoqProxyExtensions.cs                          # Main API
│   │   └── Internals/                                     # Internal implementation
│   │       ├── PropertySetup.cs                           # Property forwarding
│   │       ├── MethodSetup.cs                             # Method forwarding
│   │       ├── InterceptorSetup.cs                        # Castle.Core interceptor
│   │       └── ...
│   ├── MoqProxy.DependencyInjection.Microsoft/            # DI integration package
│   │   └── MoqProxyServiceCollectionExtensions.cs
│   ├── MoqProxy.UnitTests/                                # Unit tests
│   │   ├── MethodProxyTests/
│   │   ├── PropertyProxyTests/
│   │   ├── EventProxyTests/
│   │   └── ProxyInterceptorTests/
│   ├── MoqProxy.DependencyInjection.Microsoft.UnitTests/  # DI tests
│   └── Demo/                                              # Demo application
└── README.md
```

## Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/geoder101/MoqProxy.git
cd MoqProxy

# Restore dependencies
dotnet restore src/MoqProxy.sln

# Build the solution
dotnet build src/MoqProxy.sln

# Run tests
dotnet test src/MoqProxy.sln

# Create NuGet packages (optional)
dotnet pack --output out/nupkgs src/MoqProxy.sln
```

### Running the Demo

```bash
cd src/Demo
dotnet run
```

The demo application showcases the core functionality of MoqProxy including property synchronization, method forwarding,
generic methods, and async operations.

## Testing

The project includes comprehensive unit tests covering:

- **Property proxying** - Regular properties, read-only, write-only, state synchronization
- **Method proxying** - Sync/async methods, various parameter counts, return types
- **Event proxying** - Event subscription/unsubscription, standard and custom delegates, multiple handlers
- **Generic methods** - Type inference, multiple type parameters
- **Ref/out parameters** - Automatic forwarding and verification
- **Indexers** - Single and multi-parameter indexers
- **Edge cases** - Overrides, resets, interceptor behavior

Run all tests:

```bash
dotnet test src/MoqProxy.sln
```

Run with coverage (requires additional tooling):

```bash
dotnet test src/MoqProxy.sln /p:CollectCoverage=true
```

## Versioning

This project uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) for automatic semantic
versioning based on git history. Version numbers are automatically generated during build.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Related Projects

- [Moq](https://github.com/devlooped/moq) - The mocking library this extends
- [Castle.DynamicProxy](https://www.castleproject.org/projects/dynamicproxy/) - Used by Moq for proxy generation

---

### Co-authored with Artificial Intelligence

This repository is part of an ongoing exploration into human-AI co-creation.  
The code, comments, and structure emerged through dialogue between human intent and LLM reasoning — reviewed, refined,
and grounded in human understanding.
