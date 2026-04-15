# Runtime

The decaf runtime is a minimal library that provides higher level APIs for common operations used by the compiler. While we could implement these operations in the compiler itself, this would lead to large blocks of wasm builder code that would be difficult to maintain and optimize. By providing a runtime library we can keep the compiler code clean and focused on code generation, whenever we need to perform an operation from the runtime we generate a call to the appropriate function in the runtime library. This means that the runtime needs to be programmed with the compiler in mine and some language features are not available in the runtime because they would depend on themselves. For instance you cannot allocate arrays in the runtime because the runtime implements the allocator itself.

When compiling we parse the user code and parse the runtime as two separate programs, we then take the runtime modules and insert them into the user program before their code. This isn't the most efficient way to bundle a runtime however it provides nice file errors and allows us to implement a runtime separately without supporting linking.

## Memory Management

The Runtime exposes a very simple memory management API that allows the compiler to quickly allocate memory. Currently Decaf uses a simple bump allocator that exposes `Runtime.malloc` and `Runtime.calloc` functions. 

The bump allocator works by keeping a global pointer to the end of the allocated memory, when a new allocation is requested we simply move the pointer forward by the requested size (we round to the nearest 4 bytes to ensure proper alignment (this helps with performance)). This means that we can allocate memory very quickly, however we cannot free memory once it has been allocated. This is a tradeoff that we make for simplicity and performance but would be a major problem for long running or real programs. As this code is implemented in the runtime it would actually be rather trivial in the future to implement a more complex block based allocator that supports freeing memory and prevents fragmentation. One note here though is it may make more sense to investigate using wasmGC for managing heap data in the future so we do not also need to implement a GC in the runtime. Our current solution to GC is just to leak all data.

## Allocations

The runtime provides a rather simple API for allocating language constructs such as arrays, strings and in the future objects. The API is pretty straight forward and currently only exposes `Runtime.allocateArray(int itemCount) -> int: ptr` which allocates an array of the given item count and returns a pointer to the start of the array.