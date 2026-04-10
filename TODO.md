# TODO

## Critical
* Codegen
  * Modules
  * Functions
  * Strings
* ANF
  * We should resolve symbols in the anf tree
    * This means removing `Location` in place of more specific instructions
  * A void expr call needs no drop (Should not be assigned to an imm)
  * We should lower runtime stuff here maybe???
* WasmTree
  * Validate `ToWat`
  * Implement `ToWasm`
* Documentation
  * Document behavior of `<class>.Main`
    * NOTE: We could drop these if we make the module changes in cleanup
    * `<class>.Main` is called on every class in the order they are defined in the program
    * `<class>.Main` is called with zero arguments
    * `<class>.Main` must return void
    * `<class>.Main` is optional, if not defined, the class is still valid
  * Polish a proper compiler walkthrough
    * Break the readme into seperate files under `./docs/compiler_walkthrough`
    * Give a detailed walkthrough of the entire compiler in `./docs/compiler_walkthrough/README.md`
  * Document the runtime
    * Document the runtime API in `./docs/runtime_api.md`
* Testing
  * Test WasmTree on it's own
  * Test Codegen by snapshot testing the generated WasmTree
  * Test the entire compiler by testing the runtime output of the generated wasm module
    * NOTE: We don't test snapshots of the wasm itself because it's not human readable and its a bad artifact
    * NOTE: We may inspect the generated wasm in some cases to ensure certain optimizations are being applied
      * This will normally be done on the WasmTree rather than the output of the wasm tree 

## Cleanup 
* We should rename `class` to `module`
  * Justification: We don't implement OOP so module is more reflective
  * Drop `new <class>()` syntax for instantiation
  * Rename `class` to `module` in the source language
* Drop declaration restrictions
  * Justification: It's more ergonomic to allow declarations to appear anywhere and doesn't really add complexity
* Implement a statement similar to allow wasm imports to be declared in the source language
  * Justification: If we do this and provide callouts for wasm instructions then users can compile any code they want, meaning the compiler is fully featured and could be used for real world programming.
* Better Variables
  * We should just allow declarations anywhere in the form of `<type> <id> = <expr>` instead of the current declaration concept.
  * Properties in this scenario can just become regular binds at the top level.
* Anf Optimizations
  * Constant folding and propagation
    * Constant folding is litterally just in cases like a binop if the left and right are constants we can just compute the result
    * Constant propagation is just checking the immediate linked by a bind is a constant and if so replace the immediate
    * NOTE: These need to be implemented together because constant folding creates new constants that can then be propagated
  * Unused variable and function detection
    * Check if a variable is ever consumed, if not remove it
    * Check if a function is ever called, if not remove it
    * NOTE: These passes iterate blocks and modules in reverse
* Convert primitive callouts to a format like `@<identifier>(params)`
  * i.e `callout("@wasm.memory.size")` becomes `@wasm.memory.size()`
  * This makes primitives just like regular function calls making them a bit more ergonomic
* Investigate using wasm gc
  * We should do some simple investigation on how hard it would be to use wasm gc
* Add an enum to represent operations or use primitive calls