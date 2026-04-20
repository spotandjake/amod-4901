# TODO

This document contains a list of all the major tasks that need to be done before we consider assignment 5 complete. These tasks have been broken down into a few categories based on their importance.

### Critical
These are things directly related to deliverables or causing codegen issues that we must fix.

* WasmTree
  * Cleanup the wasm tree itself to ensure the ir is proper and matches wasm (We currently have some weird adaptions)
  * Cleanup exports
  * Cleanup the element section
* Fixes
  * If you tried using equality on a function it would cause a wasm validation error, we need ref.eq support (maybe ref casting).
  * Fix operator precedence in the parser, currently it is just left to right which is not correct.
* Testing
  * Test the compiler end to end by running the produced modules and capturing the output
* Implement the CLI
* Create some example programs
  * Hello World
  * Math
  * Looping
  * Arrays
  * Recursion
  * First Class Functions
  * Maybe something slightly more advanced like reading from a file???
  * NOTE: These examples should be used in our end to end tests as well

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
