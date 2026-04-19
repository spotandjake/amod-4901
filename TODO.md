# TODO

This document contains a list of all the major tasks that need to be done before we consider assignment 5 complete. These tasks have been broken down into a few categories based on their importance.

### Critical
These are things directly related to deliverables or causing codegen issues that we must fix.

* Fix references
  * Currently to handle references we resolve them by name and during codegen we mangle the name.
    * We should probably assign each reference a unique id during scope checking and then only use that to refer to them from then on.
* WasmTree
  * Cleanup the wasm tree itself to ensure the ir is proper and matches wasm (We currently have some weird adaptions)
  * Implement a `ToWasm`
  * Cleanup our `ToWat`
* ANF
  * I think `ExprStatement` is causing us to generate intermediate binds to `void`s (we should correct this)
  * Differentiate between a global and local in the top level bind
    * Globals are used in other functions and need to be emitted as wasm globals
    * locals are only used in the module body itself
  * Differentiate between a `call` and `call_ref`
  * Perform symbol resolution in the anf tree
    * This means replacing `AnfTree.Location` with more specific instructions like:
      * `AnfTree.Local.Get`
      * `AnfTree.Local.Set`
      * `AnfTree.Global.Get`
      * `AnfTree.Global.Set`
      * `AnfTree.Array.Set`
      * `AnfTree.Array.Get`
* Testing
  * Test Result of CodeGen by snapshot testing the generated WasmTree
  * Test the compiler end to end by running the produced modules and capturing the output
* Documentation
  * Ensure all the docs are to date.

### Less Critical
* Anf Optimizations
  * Constant folding and propagation
    * Constant folding is literally just in cases like a binop if the left and right are constants we can just compute the result
    * Constant propagation is just checking the immediate linked by a bind is a constant and if so replace the immediate
    * NOTE: These need to be implemented together because constant folding creates new constants that can then be propagated
  * Unused variable and function detection
    * Check if a variable is ever consumed, if not remove it
    * Check if a function is ever called, if not remove it
    * NOTE: These passes iterate blocks and modules in reverse
* Testing
  * FrontEnd
    * Parsing
      * We should rewrite these to match the new grammar
      * Proper precedence tests
    * Semantic Analysis
      * We should clean up the semantic analysis tests
        * We should find each place a semantic error can occur and write a specific test targeting the branch
        * We should also have related tests for valid programs that are similar
          * i.e proper `continue`, `break` usage.
  * MiddleEnd
    * We should rewrite the type checking tests to be more specific and less snapshot based
  * Backend
    * Write tests for codegen (likely just snapshot tests)
  * End to End
    * Everything