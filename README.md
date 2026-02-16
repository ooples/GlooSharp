# GlooSharp

.NET bindings for the [Gloo](https://github.com/facebookincubator/gloo) collective communications library from Facebook/Meta.

GlooSharp provides P/Invoke wrappers for native Gloo C++ operations, enabling high-performance distributed training from .NET applications.

## Features

- **AllReduce** - Combine data from all processes (gradient averaging)
- **Broadcast** - Distribute data from one process to all others
- **AllGather** - Gather data from all processes
- **Barrier** - Synchronize all processes
- **ReduceScatter** - Reduce and scatter in one operation
- **TCP Transport** - Works on any network
- **InfiniBand Transport** - RDMA support for HPC clusters

## Installation

```bash
dotnet add package GlooSharp
```

> **Note:** The native `gloo_native` library must be built separately. See [Building the Native Library](#building-the-native-library) below.

## Quick Start

```csharp
using GlooSharp;

// Create context for this process
using var ctx = new GlooContext(rank: 0, worldSize: 4);

// Set up TCP transport
GlooTransport.CreateTCP(ctx, "localhost", 29500);

// AllReduce: sum gradients across all processes
var gradients = new float[] { 1.0f, 2.0f, 3.0f };
GlooCollectives.AllReduce(ctx, gradients, GlooReduceOp.Sum);
// gradients now contains the sum from all 4 processes
```

## Supported Data Types

| C# Type | GlooDataType |
|---------|-------------|
| `float[]` | Float32 |
| `double[]` | Float64 |
| `int[]` | Int32 |
| `long[]` | Int64 |

## Building the Native Library

The managed bindings require a native C shim library (`gloo_native`) that wraps the Gloo C++ library.

### Prerequisites

- CMake 3.14+
- C++17 compiler
- [Gloo library](https://github.com/facebookincubator/gloo) installed
- Optional: libibverbs for InfiniBand support

### Build Steps

```bash
cd src/GlooSharp/native
mkdir build && cd build

# TCP only
cmake .. -DCMAKE_BUILD_TYPE=Release

# With InfiniBand support
cmake .. -DCMAKE_BUILD_TYPE=Release -DGLOO_USE_IBVERBS=ON

cmake --build . --config Release
```

### Native Library Locations

Place the built library in the appropriate runtime directory for NuGet packaging:

```
src/GlooSharp/runtimes/
  win-x64/native/gloo_native.dll
  linux-x64/native/libgloo_native.so
  osx-arm64/native/libgloo_native.dylib
```

## Architecture

```
GlooSharp (Managed .NET)
    |
    | P/Invoke [DllImport("gloo_native")]
    v
gloo_native (C shim, extern "C")
    |
    | C++ calls
    v
Gloo C++ Library (Facebook)
    |
    v
TCP / InfiniBand Transport
```

## Integration with AiDotNet

GlooSharp is designed to be used by `AiDotNet.DistributedTraining.GlooCommunicationBackend<T>` for native Gloo support. When the GlooSharp NuGet package is installed, the backend automatically detects it and uses native collective operations instead of the built-in TCP fallback.

## License

Apache-2.0. See [LICENSE](LICENSE) for details.
