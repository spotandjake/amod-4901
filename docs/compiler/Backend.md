# Back end

This document exists to provide a high-level overview of the backend of the compiler. More specific documentation can be found in the source code itself, and in the documentation for the individual components of the backend.

The backend of the compiler is responsible for taking the output of the middle end, which is an `AnfTree` and converting it into a `WasmModule` which can then be executed in a WebAssembly runtime.

Unlike the other stages where tests are done on each sub pass within the stage we only have tests for the backend as a whole which can be found in `decafTests/Backend` the reason for this is that the best way to test the stages on their own would be snapshot testing however that doesn't easily capture the actual semantics themselves, so instead we do full end to end testing on the entire compiler which captures the backend and mostly relies on the backend as if the wasm we are producing is incorrect then the end to end tests will fail. An informal lists of reasons not to do snapshot testing for the backend is as follows:
* The wasm we produce is not insanely human readable which means snapshots would be hard to maintain and understand
  * Contributing factors to this are anf splitting up instructions
  * Optimizations that change the structure of the wasm
* While identifiers are deterministic they can shift around a lot in a non local manner.
  * In other words because we want every id to be unique, the ids are generated in a way where they are globally deterministic but locally they can shift around, for example adding a variable to one function can affect the ids of a variable in another function.
    * This is something I want to improve in the future however it's non trivial to do so without adding a lot of complexity or delaying the label generation to the wasm tree which is also non trivial.
* The semantics of the wasm we produce are more important than the exact structure of the wasm, and snapshot testing doesn't really capture semantics well, for example if we have an optimization pass that changes the structure of the wasm but not the semantics then our snapshot tests would fail even though our codegen is still correct.
  * In simple terms it's easier to test that `1 + 2` produces `3` than it is to ensure that we are producing `(i32.add (i32.const 1) (i32.const 2))` specifically, especially when optimizations can change the structure of the wasm without changing the semantics.
    * For example constant folding could convert `(i32.add (i32.const 1) (i32.const 2))` into `(i32.const 3)` which is a perfectly valid optimization but would make snapshots harder to understand.
* Whether we are emitting `wasm` or `wat`, semantics when running stay the same however snapshots would differ so testing the running semantics is more important than testing the exact output format.


## Codegen

Code generation is probably the second most involved part of compilation apart from type checking. On the surface it is pretty easy, we just need to lower the `AnfTree` to `wasm`, however the sheer amount of code that needs to be written and the various edge cases that can arise from lowering make it a pretty involved process.

We implement code generation in `decaf/Backend/` with the main logic being contained in `Codegen.cs` and the various other files providing either helper, utility or just existing to split out generation for a specific feature of the language.

This document will not describe every primitive and it's lowering steps as that would be a very long and tedious process that would fall out of date the moment we change something in our implementation. If you want a full understanding the best option is to read the code as it's very well documented provided you have a decent understanding of our `AnfTree`, `Wasm` and the language itself. If you want a high level overview I would recommend looking at the planning document for `codegen` as it provides a good high level overview however keep in mind that the implementation has changed a decent bit since the planning document was written so it isn't completely up to date with the implementation, it does still provide a good overview of the ideas themselves. I will however list a few of the major design decisions that we made during codegen that may not be immediately obvious from the code itself.

### Major Design Decisions

I would have enjoyed providing a more detailed explanation of codgen however it's really hard to give a high level overview while properly explaining the specifics of the implementation without just going through the code itself. Below are a few of the major design decisions that we made during codegen that may not be immediately obvious from the code itself.

#### Use of `funcref` for functions
In our languages functions are defined like values however function literals are constrained so that they can only be defined as the immediate right hand side of a bind. What this means is that while we do not directly support full first class functions like javascript, you can't just define them anywhere, we do support first class functions in the sense that they can be passed around like any other value, this is enabled by our decision to use `funcref` for functions, instead of direct calls. The decision to make this change was driven by the syntax we landed on for function literals and not wanting to have to do some weird hacky tracking to support good codegen, in other words `funcref` lowered the number of invariants we had to maintain during codegen and made it a lot easier to work with. The disadvantage of this approach as it stands is that `funcref` adds a small layer of indirection to function calls which can have a small performance disadvantage, however it would be trivial for us to add an optimization pass at the anf stage that inlines calls to functions that are not being passed around like variables, and it is also trivial for an external wasm optimizer like `wasm-opt` to inline these calls as well so the performance disadvantage is not a huge concern for us at this time and the benefits in terms of ease of implementation and flexibility are worth it.

#### Use of utf8 strings
Another place where our implementation diverged from the design doc was `strings`, in the design doc we mentioned that strings would be implemented as arrays of characters. Instead however we implemented proper utf-8 strings, our implementation still follows a very similar memory layout however of `<byteSize>, ...characters` with the biggest difference being that `byteSize` is not the same as the character length of the string due to utf8 characters being able to be multiple bytes, in ascii this isn't really a problem as each character is a single byte however outside of ascii this means that the length of the string may not match the byte length of the string. The decision to make this change was primarly driven by the use of `WASI` in the runtime for printing which uses utf-8 by default, and it being easy to implement using wasm data sections. Additionally nothing would stop a person from defining an array of character if they wanted to anyways.

#### Use of `WasmTree` as an intermediate representation
The final note though this wasn't really a place we differed from the initial design doc, was our decision to use a `WasmTree` intermediate representation in `decaf/WasmBuilder/`, more information on this can be found in the next section.

## WasmTree

At the backend of the compiler we implement a `WasmTree` intermediate representation in `decaf/WasmBuilder/`, this intermediate form is a thin mapping of the `wasm` instruction format. There are many benefits to compiling to a final IR like this, in general it separates out wasm semantics directly from our codegen which would make targeting something like `x86` or another assembly in the future quite a bit easier. Additionally and more directly impactful now it separates the exact output format from the building itself allowing us to emit either `Wasm` or `WAT` independently. 

The `WasmTree` is implemented following the folded form of wat rather than the stack semantics of wasm, i.e `(i32.add (i32.const 1) (i32.const 2))` rather than the stack approach of:
```wat
(i32.const 1)
(i32.const 2)
i32.add
```

This approach makes it easier to follow the stack semantics of wasm as each instruction clearly takes a specific number of arguments in a sane order, if we did the other approach we would have to be mindful of what is on the stack at any given time, and there is more of a chance of us accidentally leaving something on the stack that we didn't mean to, or popping something off the stack that we needed later. The downside of this approach however is that compared to taking advantage of the stack approach we do have to track a few more locals and temporary variables, however this is a small price to pay for the increased readability and ease of use of the folded form, especially when an optimizer like `wasm-opt` can easily transform our folded form into the more efficient stack semantics if it wants to.

The `WasmTree` was designed around our needs during codegen as such it is not a complete representation of all of `wasm` and is narrowly scoped to what we need this also means that it can be a little rough around the edges for any use outside of our such as using it as a general purpose `wasm` builder, however it is still a very useful tool for us and allows us to easily build up our `wasm` modules in a way that is easy to understand and work with and we could easily overhaul it in the future. However I would probably recommend we switch to using binaryen as a backend instead given the many optimization benefits it provides.
