# Planning document for code generation

## Approach
This document is a very high level overview of how we plan to lower syntax constructs in our language to wasm instructions. This document treats the codegen process as rather syntactic in reality we are going to convert to a format called [ANF](https://en.wikipedia.org/wiki/A-normal_form) first, the reason for this conversion is it makes analysis extremely simple and allows us to produce less nested codegen which is easier for optimizers to work with.

### Why ANF?
I think the first question that comes up is why did we choose ANF, to start off with I should mention we could convert directly from our TypedTree to wasm however this would produce very nested code which is not ideal for optimizers or us to work with. ANF on the other hand is an extremely simple intermediate representation. Essentially statements can only take an immediate, which is either a constant or a variable as an argument. This means that something like `a + b * c` would be converted to something like:
```
let temp1 = b * c
let temp2 = a + temp1
```
This is a very simple representation that is easy to work with and allows us to produce less nested codegen which is easier for optimizers to work with.

So then why ANF in particular over something like [CPS](https://en.wikipedia.org/wiki/Continuation-passing_style) or [SSA](https://en.wikipedia.org/wiki/Static_single_assignment_form). I would like to start off by noting that there is a proof that exists that shows ANF and CPS are equivalent and SSA is a subset of ANF so in reality we could choose any of these three and be fine. CPS is a bad choice, it's normally used for functional languages and it's main benefit is it makes closures implicit. SSA on the other hand is the prime choice for object oriented languages however it is far more complex than ANF and we don't really need the benefits it provides so we are going with ANF for the simplicity and ease of implementation, under the consideration that we don't need the benefits provided by SSA and CPS.

### How does this document relate to the actual codegen process?
This document is just a high level overview more similar to compiling a parseTree directly to wasm, it does obscure a lot of the implementation details but the reason for describing things at such a high level is it simplifies the definitions and allows this document to serve as a guide. In reality converting from ANF to codegen following this guide shouldn't be too hard given our ANF is also a subset of the parse tree itself.

### Compiling itself
Instead of directly emitting wasm instructions by traversing the anf tree, we are instead going to convert the anf tree to a wasm tree this will allow us to collect module types and other things in a more organized manner.

## Program
Programs are the top level of our language and map directly onto files. They are static elements and don't have any runtime representation. We are going to convert a program directly to a wasm module this transformation would convert a file like:
```java
<program_body>
```
to a wasm module like:
```wasm
(module <program_body>)
```
in reality we are going to be generating a wasm tree and instead of outputting `wat` which is the Webassembly text format we are going to be outputting `wasm` which is the binary format, which is more compact and easier for machines to parse and work with. We are documenting the process here in `wat` for simplicity the wasm spec can be followed for the conversion.

Documentation on wasm modules can be found here: https://developer.mozilla.org/en-US/docs/WebAssembly/Guides/Understanding_the_text_format#the_simplest_module

## Classes
Classes in our language are static that means they don't have any runtime representation and cannot be instantiated at runtime. In a sense they are equivalent to namespaces or modules in other languages. As classes have no runtime component they don't really get compiled into code instead they act as a literal namespace.

When compiling a class of the form:
```java
class Program {
  int x;
  void Main() {}
}
```
we compile the properties as described in the properties section and the methods as described in the methods section. Notably we mangle the name to the class so `x` becomes something similar to `Program_x` and `Main` becomes something similar to `Program_Main` this ensures that every property and method has a unique name and we don't have to worry about name collisions.

### Properties
Class properties in our language are treated as globals given there is no instance this means the `int x` in the `Program` class above is going to become `(global $Program_x i32)` in wasm, which is a pretty simple compilation.

### Methods
Methods in our language are also pretty simple, as the are not first class and don't have closures which means they are pretty much a 1 to 1 compilation to a wasm function. 

As an example the function:
```java
int Add(int a, int b) {
  <body>
}
```
is going to compile into something like:
```wasm
(func $Program_Add (param $a i32) (param $b i32) (result i32)
  <body>
)
```

If we had first class functions things would get more complicated as we would need to build a closure which is essentially a record of all the variables used from the parent scope, we would also need to allow functions to be passed around as values which would require us to use indirect calls and function tables in wasm. This is a bit more complicated to implement however.

For more information on wasm classes see: https://developer.mozilla.org/en-US/docs/WebAssembly/Guides/Understanding_the_text_format#our_first_function_body

## Statements
Statements in our language are pretty simple to compile as most of them map pretty closely to wasm instructions.

### Assignments
Assignments are one of the slightly more complicated statements to compile as we need to consider the location which can have a few different forms.

#### Simple Variable Assignment
This is the simplest form of assignment and is pretty much a 1 to 1 compilation. Take the code:
```java
int x;
x = <expr>;
```
This would compile into something like:
```wasm
(local.set $x <expr>)
```

For more information on `local.set` see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.set

#### Property Assignment
This is also a rather simple case, given a property assignment like:
```java
Program.x = <expr>;
```
This would compile into something like:
```wasm
(global.set $Program_x <expr>)
```

For more information on `global.set` see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global.set

#### Array Assignment
This is where assignments get a tiny bit more tricky, arrays are stored in linear memory and there are a few steps to generating them.
It may be helpful to take a look at [ArrayInitialization](#ArrayInitialization) for more information on how arrays are stored in memory, in order to have enough context to understand how to compile array assignments. Given an array assignment like:
```java
arr[<index>] = <expr>;
```
This would compile into something like:
```wasm
; Load the array length
(local.set $arr_length ; Set a temporary variable to hold the array length
  (i32.load
    (local.get $arr) ; Get the base pointer of the array
    0 ; offset is the first 4 bytes of the array data structure
  )
)
; Check if index is out of bounds and trap if it is
(if
  (i32.ge_u (local.get $index) (local.get $arr_length)) ; Check if the index is out of bounds
  (then
    unreachable ; This causes wasm to trap (throw an exception) if the index is out of bounds
  )
  (else
    (i32.store
      (local.get $arr) ; Get the base pointer of the array
      (i32.add
        (i32.mul (local.get $index) 4) ; Calculate the offset for the index, each item is 4 bytes
        4 ; Add 4 to skip the length field at the start of the array data structure
      )
      <expr> ; The value to store at the index
    )
  )
)
```

For more information the [wasm spec](https://webassembly.github.io/spec/core/) is probably the best resource for understanding how these instructions work.

### Expression Statements
Expression statements are extremely easy to compile, we compile them like any other function, with one slight difference which is we don't want to leave the result no the stack so we most drop it. This means that the code:
```java
add(1, 2);
```
would compile into:
```wasm
(drop
  (call $Program_add (i32.const 1) (i32.const 2))
)
```
or more generically:
```
<expr>
```
becomes:
```wasm
(drop <expr>)
```

For more information on drop see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/drop

For more information on compiling expressions see: [Expressions](#Expressions)

### If Statements
If statements are extremely simple to compile as they have a direct wasm equivalent, given an if statement like:
```java
if (<condition>) {
  <then_body>
} else {
  <else_body>
}
```
This would compile into something like:
```wasm
(if
  <condition>
  (then
    <then_body>
  )
  (else
    <else_body>
  )
)
``` 
In the case that there is no else body we can just omit the else block.

For more information on if statements see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/if...else

### While Node
`While` loops are pretty easy to compile however there is no directly equivalent to a while loop at the webassembly level, instead we need to use `loop` and `br` instructions to create a loop. Given a while loop like:
```java
while (<condition>) {
  <body>
}
```
This would turn into something like:
```wasm
(block $break_label
  (loop $while_loop
    (if
      (i32.eqz <condition>) ; Check if the condition is false
      (then
        (br $break_label) ; If the condition is false, break out of the loop
      )
    )
    <body>
    (br $while_loop) ; Jump back to the start of the loop
  )
)
```
This is a pretty standard way for compilers to implement loops, one thing to note is that while we do a lot of the lowering to wasm when performing codegen this is actually something that we simplify during the ANF conversion process, to unify any type of loop we have in the language. This means that codegen for `while`, `for`, and `do while` is all just syntax sugar on top of a loop.

This is going to compile to a basic loop see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/loop

### ContinueNode
Continue statements are extremely easy to compile, given a continue statement `continue;` this would compile into:
```wasm
(br $while_loop) ; Jump back to the start of the loop
```

For more information on `br` see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br

### BreakNode
Break statements are also pretty easy to compile, given a break statement `break;` this would compile into:
```wasm
(br $break_label) ; Jump to the end of the loop
```

For more information on `br` see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br

### ReturnNode
Return statements are also extremely easy to compile, given the statement `return <expr>;` this would compile into:
```wasm
(return <expr>)
```

This is going to compile to a return https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/return

## Expressions

Compiling expressions is pretty simple just like statements as most of them have a pretty direct mapping to wasm instructions.

### Call Node
Call nodes in our language are pretty simple to compile as they have a direct mapping to wasm function calls, given a call like:
```java
add(1, 2);
```
This would compile into:
```wasm
(call $Program_add (i32.const 1) (i32.const 2))
```
or more generally:
```java
<function_name>(<arg1>, <arg2>, ...)
```
becomes:
```wasm
(call $<function_name> <arg1> <arg2> ...)
```

For more information on function calls see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/call

It's worth noting that this is only simple because we don't have closures or first class functions, for closures we would need to pass a closure pointer as an additional argument to the function, and for first class functions we would need to use indirect calls and function tables in wasm which is a bit more complicated to implement.

We are leaving the design for primitive callouts until we are at that point as they are going to be able to compile into a variety of different constructs from low level wasm instructions to regular function calls depending on the callout.

### Binop Node
Binary operations are also pretty simple to compile as they have direct mappings in webassembly the exact instruction we are going to use is going to depend on both the operator and the types of the operands, but an example of compiling `a + b` would be:
```wasm
(i32.add (local.get $a) (local.get $b))
```
For subtraction it may become:
```wasm
(i32.sub (local.get $a) (local.get $b))
```
or more generally, `<expr1> <operator> <expr2>` becomes:
```wasm
(<wasm_operator> <expr1> <expr2>)
```

### Prefix Node
Prefix nodes are going to be handled the exact same way as binops, given a prefix operation like: `!a` this would compile into:
```wasm
(i32.eqz (local.get $a))
```
or more generally, `<operator> <expr>` becomes:
```wasm
(<wasm_operator> <expr>)
```
As a note while we have described compilation of prefix nodes in a universal manner the only prefix operator in our language is `!` which is the logical not operator.

### New Class Node
This is an object oriented feature as such it does not get compiled. If we had object oriented features it would likely get compiled into a memory allocation or wasm gc struct, of the form `{ <classID>, <field1>, <field2>, ...}`. I would describe a bit more but given we are not implementing them it seems like a waste.

### ArrayInitialization
Array initialization is likely the hardest thing to compile in our language as it requires us to work with linear memory.
Our current plan is to ship a basic bump allocator with our runtime which is essentially just a pointer to the end of the allocated memory, and when we want to allocate something we just move the pointer forward by the size of the allocation, this isn't the most efficient and fragments fast but it works for our use cases.

Arrays are going to be stored in linear memory following the format, `<length> ...<items>` this means that:
```
ptr -> length
ptr + 4 -> item 1
ptr + 8 -> item 2
...
```

I don't show the compilation process here as it is a bit complex and will likely be lowered to use a few helper functions in a basic runtime.

### ThisNode
We don't actually compile `this` itself but instead we resolve it during compilation so the code:
```java
class Program {
  int x;
  void Main() {
    this.x = 5;
  }
}
```
is directly equivalent to:
```java
class Program {
  int x;
  void Main() {
    Program.x = 5;
  }
}
```

This only works because `this` is only valid in a static context, this approach would completely break with instance properties and methods as `this` would need to refer to the instance rather than the class, but given we don't have instance properties or methods this is a perfectly fine approach.

### LocationNode
Location nodes are probably one of the more complex things to compile as well, we already gave a brief overview of them when discussing assignments. Compiling locations is going to be handled the exact same way as compiling the left hand side of an assignment however `set` will become `get` and `store` will become `load`.

### LiteralNode
Literal nodes are rather simple to compile because they are just constants, exact compilation will depend on the type.

#### Integer
This is going to become a `(i32.const <value>)`

### Character
This is also going to become a `(i32.const <value>)` we will make no distinction between a character and an integer at runtime. The value itself is going to be the unicode scalar value of the character.

### BooleanNode
This is going to compile to a `(i32.const <value>)` with `1` for `true` and `0` for `false`.

### NullNode
`Null` is an interesting one as it is a special value it probably makes the most sense to compile it into `(i32.const 0)` as this would give it falsey semantics however there would be no distinction between `null` and `false` at runtime, which is a bit unfortunate. 

We could also make the decision to not compile this given classes are static there really isn't much use for `null` as nothing can be `null`.

### String
Strings are an interesting case, with a few ways to compile them, the first step is figuring out how we are going to represent them some precedent is:
* Null terminated strings: This is a simple and efficient representation but length retrieval is O(n) and it doesn't follow `utf-8` directly which can make ffi a bit annoying.
* Length prefixed strings: These are essentially arrays of characters with a length field at the start, this allows for O(1) length retrieval and is pretty efficient however it also doesn't follow `utf-8` directly which can make ffi a bit annoying.
* UTF-8 strings: This is the most standard representation and follows `utf-8` directly which makes ffi easy however it is a bit more complex to implement and work with as we need to handle variable length encoding and decoding.

Despite the added complexity of implementing UTF-8 strings, I think it's worth it for external interop so what we are going todo is store them as `<byteLength> <utf8Bytes>` this means that the string `hello` would be stored as:
```
ptr -> 5
ptr + 4 -> 'h'
ptr + 5 -> 'e'
ptr + 6 -> 'l'
ptr + 7 -> 'l'
ptr + 8 -> 'o'
```
This is starting to sound a bit like how we are storing arrays and when working with ascii it is, however the complexity comes out when we want to support more unicode characters like a smiley face `😀` which has a utf-8 encoding of `0xF0 0x9F 0x98 0x80` this means that the string `hi 😀` would be stored as:
```
ptr -> 7
ptr + 4 -> 'h'
ptr + 5 -> 'i'
ptr + 6 -> ' '
ptr + 7 -> 0xF0
ptr + 8 -> 0x9F
ptr + 9 -> 0x98
ptr + 10 -> 0x80
```