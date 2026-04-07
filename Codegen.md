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
Programs map directly onto wasm modules, so we are going to compile a program into a wasm module. This means that the program node is going to be the top level. Inside a program we will directly compile the classes inside.

## Classes
Given classes are static in our language like namespaces they don't really get compiled into a code unit instead we just compile the things inside.

If classes we'rent static we have two options either compile them to a wasm gc struct or compile them to a struct in linear memory however this isn't very trivial and would affect a lot of other little details of compilation.

### Properties
Class properties can be thought of as global as they save state between function calls and are shared globally, all were going todo is compile them directly into globals so an `int x` might compile into `(global $x i32)` however we are going to need to mangle these names to avoid name collisions so lets say that the `int x` is within the `Program` class we might compile it to something like `(global $Program_x i32)` this is a pretty simple way to compile properties and it works well with the static nature of our classes.

### Methods
Methods in our language are also pretty simple, they are not first class and don't have closures which means they are pretty much a 1 to 1 compilation to a wasm function.  [function example here](https://developer.mozilla.org/en-US/docs/WebAssembly/Guides/Understanding_the_text_format#our_first_function_body).

If we had first class functions things would get more complicated we would need to compute a closure which would essentially be at the time of creation we take all the variables used from outside the functions scope and put them into a struct in linear memory this struct also contains a function reference or table index that we can use to call the function without knowing it's name (for first class functions). We would then need to add an extra parameter to the function for the closure struct and compile the function body to extract the values from the struct.

## Statements
Statements are going to be lowered within a function to wasm instructions.

### Assignments
Assignments are pretty simple we need to compile the left hand side which is the location accessor to become a `global.get` if we are working on an array we are going to need to set a memory address.

We are then either going to use ` WasmI32.store` for arrays or `local.set` for local variables or `local.set` for global variables `global.set`

### Expression Statetements
These compile pretty simply as we compile the expression like normal and just `(drop)` the result if there is one.

### If Statements
If statements are not to hard to compile they basically compile to a wasm if after we compile the expression. 

### While Node
This is going to compile to a basic loop see: https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/loop

### ContinueNode
This is going to compile pretty cleanly to a `br` back to the top of the loop

### BreakNode
This is going to compile pretty cleanly to a `br` to the end of the loop (I need to look at codegen for this a tiny bit more)

### ReturnNode
This is going to compile to a return https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/return

## Expressions

### Call Node
This is going to compile to a wasm call pretty cleanly

### Binop Node
This is going to compile pretty cleanly into a wasm instruction we are going to need to use the signature and operator to determine what instruction to use from a lookup table.

### Prefix Node
This is going to be done just like binops but with different instructions. I think for not we have a few option for compiling the simplest one is probably bitwise. It's also ussually the fastest

### New Class Node
This isn't going to be compiled if it was we would store some sort of record in linear memory that points to everything.

### New Array Node
This is going to probably use a very primtive bump allocator to compile some instructions that write a simple data structure to memory probably something like ```<arrayTypeID>, <size>, ...<values>````

### LocationNode
I think we still need todo some thoughts here but it's essentially going to be come a local.get or local.set depending on the use, this is probably going to be compiled in a somewhat context aware manner.

### ThisNode
I don't actually think we need to compile this for any real reason instead what we are probably going to be needing todo is just interpret this as a location client side, technically I guess we can assign each class an id. We may skip compiling this all together though do to the oop limiting nature.

### IdentiferNode
This is just a subpart of location nodes. Handled by that.

### LiteralNode
These are going to be compiled to either memory allocations or simple constants

#### Integer
This is going to become a `(i32.const <value>)`

### Character
This is also going to become a `(i32.const <value>)` we will make no distinction between a character and an integer at runtime.

### String
This is only going to be compiledin one place the values will go into data sections and we will copy from the data section into memory passing around the pointer.

### BooleanNode
This is going to compile to a `(i32.const <value>)` with `1` for `true` and `0` for `false`.

### NullNode
This is going to compile to a `(i32.const 0)` most likely though I need to consider this a bit more. 


#### Other Notes
* We can't modify parameters so we are going to need to not allow assignments on parameters this can be done in the typechecker.