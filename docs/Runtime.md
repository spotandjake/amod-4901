# Runtime

The decaf runtime is a minimal library that provides higher level APIs for common operations used by the compiler. While we could implement these operations in the compiler itself, this would lead to large blocks of wasm builder code that would be difficult to maintain and optimize. By providing a runtime library we can keep the compiler code clean and focused on code generation, whenever we need to perform an operation from the runtime we generate a call to the appropriate function in the runtime library. This means that the runtime needs to be programmed with the compiler in mind and some language features are not available in the runtime because they would depend on themselves. For instance you cannot allocate arrays in the runtime because the runtime implements the allocator itself.

When compiling we parse the user code and parse the runtime as two separate programs, we then take the runtime modules and insert them into the user program before their code. This isn't the most efficient way to bundle a runtime however it provides nice file errors and allows us to implement a runtime separately without supporting linking.

## Memory Management

The runtime exposes a very simple memory management API, that allows the compiler to quickly allocate blocks of memory. The current implementation is a [free list](https://en.wikipedia.org/wiki/Free_list) allocator. The basic premise is that we have a linked list of free blocks in memory.

A block looks like: `<size> | <next ptr> | ...data`

Where the size is the byteSize of the block, the next pointer, points to the next free block in the list and the data is the actual payload.

We provide three functions for interacting with the allocator:

### `Runtime.malloc`
Signature: `Runtime.malloc(int byteSize) -> int: ptr`
This function takes in a byte size and returns a pointer to a block of memory of at least that size (at least as we align to 4 bytes for performance reasons). The allocator works by iterating through the free list until it finds a block that is large enough to satisfy the request or it reaches the end of the list. If it finds a block that is large enough it removes it from the free list and returns a pointer to the start of the payload section of the block. If it reaches the end of the list without finding a block that is large enough it will allocate a new block of memory from the system and return a pointer to it.

### `Runtime.calloc`
Signature: `Runtime.calloc(int byteSize) -> int: ptr`
This function works exactly the same as `Runtime.malloc`, in fact it uses it under the hood, the only difference is that after allocating the block it will zero out the payload section of the block before returning a pointer to it. This is useful for things like array initialization or creating clean buffers.

### `Runtime.free`
Signature: `Runtime.free(int ptr)`
This function takes in a pointer to a block of memory that was previously allocated by `Runtime.malloc` or `Runtime.calloc` and adds it back to the free list. The function works by first calculating the address of the start of the block by subtracting the size of the header (8 bytes) from the pointer, it then reads the size of the block from the header and adds the block back to the free list by setting the next pointer of the block to the current head of the free list and then updating the head of the free list to point to the newly freed block.

### Limitations
While this allocation scheme gives us a basic `malloc` and `free` it does have some major limitations, which is why it is rarely used in production environments. The main issue is fragmentation, as we allocate and free blocks of memory the free list can become fragmented with many small blocks of memory that are too small to satisfy larger allocation requests. This can lead to a situation where we have a lot of free memory but we are unable to allocate large blocks of memory because the free list is too fragmented. Additionally this allocator does not support coalescing of free blocks, which means that if we free two adjacent blocks of memory they will not be merged into a single larger block, this can further exacerbate fragmentation issues. Despite these limitations this allocator is sufficient for our needs and simple to implement requiring little extra code to manage the free list and no complex data structures. One of the nice things about implementing the allocator in the language itself is that we can easily modify and optimize it as needed without having to worry about interfacing with the compiler or dealing with complex data structures in the compiler code.

## Allocations

The runtime provides a rather simple API for allocating language constructs such as arrays, strings and in the future objects.

## General

We also provide a few general functions such as:
* `Runtime.itoa(num: int) -> string` - Converts an integer to a string.
* `Runtime.print(str: string)` - Prints a string to the console.
* `Runtime.printInt(num: int)` - Prints an integer to the console.