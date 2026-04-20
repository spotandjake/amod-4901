# Data Representations

This file contains a quick overview of the various runtime data representations that are emitted by the compiler, this can be thought of like an ABI for the runtime, it describes how various data types are represented in memory and how they are laid out.

## Stack Primitives

A stack primitive is any primitive value that can be directly represented on the wasm stack i.e, in a local or variable. These are things that will compile to either an `i32`, `i64`, `f32` or `f64` in wasm, and they are the most basic data types that we have in our language.

### Int

The int type used to define numbers is represented directly as an int32 using two's complement. This means that `1` is represented as `(i32.const 1)`, `-1` is represented as `(i32.const -1)` and so on. The range of representable integers is from `-2^31` to `2^31 - 1`. Notably we interpret ints as signed all of our operations addition, subtraction, multiplication, division, etc. are all signed operations. However wasm makes no distinction between signed and unsigned integers, which means that we can use wasm callout instructions to perform unsigned operations if we need to, for example we can use `@wasm.i32.rem_u` to perform an unsigned remainder.

### Bool

The bool type used to define booleans is represented as an int32, with `0` representing `false` and `1` representing `true`.

### Character

The character type used to define characters is also represented as an int32, with the value being the unicode code point of the character. For example, the character `a` is represented as `(i32.const 97)` since the unicode code point for `a` is 97.

## Heap Primitives

Heap types are any types that cannot be directly represented on the wasm stack, for instance they require more than 64 bits of information to be represented. We represent these types in linear memory and they are accessed via pointers, these pointers are represented as `i32` values that point to the location in linear memory where the data is stored. We use `Runtime.malloc` to allocate memory for these types and you can use `Runtime.free(@getPointer(val))` to free the memory used by these types, however we don't currently have any sort of garbage collection implemented so you need to be careful when using `free` to not free memory that is still in use, and if you do not use `free` you will leak memory by default, you can read a bit more about memory management in the runtime documentation.

### Strings
Strings are represented using utf8, with the following memory layout:
```<byteSize>, ...characters```

It's important to note that in `utf8` the `byteSize` is not the same as the character length of the string, this is because `utf8` characters can span multiple bytes, for example the character `a` is represented as a single byte with the value `97`, however the character `€` is represented as three bytes with the values `226`, `130` and `172`. This means that when working with strings we need to be mindful of the fact that the length of the string may not match the byte length of the string, and we need to use the `byteSize` to determine how much memory to allocate for the string and how much memory to read when accessing the string. This also means that each character is not guranteed to be a single byte, and we would need to decode the string to access individual characters, however we do not currently have any built in string manipulation functions that operate on individual characters so this is not a concern for us at this time, we can just treat strings as opaque blobs of data.

### Arrays
Arrays are represented using a similar layout to strings, with the following memory layout:
```<length>, <element0>, <element1>, ...```

Where `length` is the number of elements in the array and `element0`, `element1`, etc. are the elements of the array. The elements of the array are stored contiguously in memory, which means that we can access them using pointer arithmetic, for example if we have a pointer to the start of the array we can access the `i`th element by doing `@getPointer(array) + 4 + (i * elementSize)` where `4` is the size of the length field and `elementSize` is the size of each element in the array, notably all elements are currently `i32` values which means they have a size of `4` however this could be subject to change if we were to ever add `int64` or other types of elements to our arrays in the future.

## Special Primitives

The special primitives section describes some special types that could be either stack or heap types but have some special significance in our language and are worth mentioning separately, these include functions and void.

### Functions

Functions in our language are passed around on the stack in locals and globals, using `funcref` the difference to the regular stack values is that this takes advantage of wasm gc, which means that we can treat functions as first class values that can be passed around, however beyond reference equality it's important to note that there is no way to introspect on functions or access their internal payload.

### Void

I thought it was also worth mentioning void however unlike the other types void has no representation at all, as it is a type that represents the absence of a value, it is used as a return type for functions that do not return a value. It should be impossible to have a value of type void, if you did the compiler would throw an error as it would have no way to represent it.
