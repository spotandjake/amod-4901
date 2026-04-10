# TODO

This document contains a list of all the major tasks that need to be done before we consider assignment 5 complete. These tasks have been broken down into a few categories based on their importance.

### Critical
These are things directly related to deliverables or causing codegen issues that we must fix.

* Codegen
  * Modules - Compile Modules
  * Functions - Compile Functions
  * Strings - Compile Strings
* ANF
  * NOTE: Turns out there are a few small oversights in the anf tree that will cause codegen issues
  * We should merge `BindNode` with `ImmediateNode`
    * We should introduce a `DeclNode` which is just a `BindNode` but it's the first instance of one.
  * We only want to emit a drop for an `ExprNode` if it's not a void call.
    * This requires refactoring to how we map expressions to an imm
  * Perform symbol resolution in the anf tree
    * This means replacing `AnfTree.Location` with more specific instructions like:
      * `AnfTree.Local.Get`
      * `AnfTree.Local.Set`
      * `AnfTree.Global.Get`
      * `AnfTree.Global.Set`
      * `AnfTree.Array.Set`
      * `AnfTree.Array.Get`
* WasmTree
  * Implement `ToWat`
  * Implement `ToWasm`
* Testing
  * Test Result of CodeGen by snapshot testing the generated WasmTree
  * Test the compiler end to end by running the produced modules and capturing the output
* Documentation
  * Split README docs into separate files under `./docs/`
  * Ensure all the docs are to date.
  * Document the runtime API in `./docs/runtime_api.md`
  * Document the behavior of `<module>.Main` in `./docs/compiler_walkthrough/README.md`

### Less Critical
* Allow code at the top level
* Make declarations regular statements instead of having them in a separate context
  * NOTE: We should also change declarations to the form of `<type> <id> = <expr>` instead of the current concept
  * NOTE: Combined with the previous point, this would make properties just regular binds at the top level
* Add a statement for defining wasm imports
  * NOTE: This should be semantically restricted to the top level
* Anf Optimizations
  * Constant folding and propagation
    * Constant folding is litterally just in cases like a binop if the left and right are constants we can just compute the result
    * Constant propagation is just checking the immediate linked by a bind is a constant and if so replace the immediate
    * NOTE: These need to be implemented together because constant folding creates new constants that can then be propagated
  * Unused variable and function detection
    * Check if a variable is ever consumed, if not remove it
    * Check if a function is ever called, if not remove it
    * NOTE: These passes iterate blocks and modules in reverse
* Investigate WASM GC
  * It would be nice if we could switch to using wasm gc for our arrays and strings over the current approach of linear memory
* Convert Primitives to the form of `@wasm.memory.size()`
* Add an enum representing the binops this ensures we handle them throughout the compiler
* Replace snapshot tests with more specific test where possible
  * Justification: It's way to easy to just update snapshots without actually checking the output