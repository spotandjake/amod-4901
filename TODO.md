# TODO

This document contains a list of all the major tasks that need to be done before we consider assignment 5 complete. These tasks have been broken down into a few categories based on their importance.

### Critical
These are things directly related to deliverables or causing codegen issues that we must fix.

* WasmTree
  * Cleanup the wasm tree itself to ensure the ir is proper and matches wasm (We currently have some weird adaptions)
  * Implement a `ToWasm`
  * Cleanup our `ToWat`
* ANF
  * Differentiate between a `call` and `call_ref`
    * Instead of doing this we could probably just track this at the symbol level???
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
  * MiddleEnd
    * We should rewrite the type checking tests to be more specific and less snapshot based
  * Backend
    * Write tests for codegen (likely just snapshot tests)
  * End to End
    * Everything