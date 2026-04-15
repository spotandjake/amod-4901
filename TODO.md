# TODO

This document contains a list of all the major tasks that need to be done before we consider assignment 5 complete. These tasks have been broken down into a few categories based on their importance.

### Critical
These are things directly related to deliverables or causing codegen issues that we must fix.

* Codegen
  * Modules - Compile Modules
  * Functions - Compile Functions
  * Strings - Compile Strings
* ANF
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

### Less Critical
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
* Replace snapshot tests with more specific test where possible
  * Justification: It's way to easy to just update snapshots without actually checking the output
* Testing
  * Parsing
    * We should rewrite these tests based of the grammar more accurately
    * Precedence
  * Semantic Analysis
    * Rewrite Semantic checks
    * Rewrite type checks
  * AnfTree
    * Basic optimization tests
  * CodeGen
    * Basic codegen tests
  * End to End
    * Everything