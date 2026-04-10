# TODO

* Codegen
* Make `<class>.Main` the entry point of a class
  * Document semantics
    * `<class>.Main` is called on every class in the order they are defined in the program
  * `<class>.Main` is called with zero arguments
  * `<class>.Main` must return void
  * `<class>.Main` is optional, if not defined, the class is still valid
    * `<class>.Main` must be defined if the class is instantiated
* Cleanup
  * Better variables
    * Allow declarations to appear anywhere in the program (instead of top of block)
    * Make binds be the form of `<type> <id> = <expr>`.
    * Allow properties to hold declarations (make properties more like top level declarations)
  * Make classes more like modules
    * Properties are now just top level binds
    * Top level code becomes a special `<class>.Main` function that is called on every class
    * Methods are just special top level properties
  * Optimizations
    * Constant folding / Constant Propagation
    * Dead code elimination / detection
    * Unused variable detection
    * Unused function detection
  * Convert primitive calls to use `@` identifiers
    * i.e `callout("@wasm.memory.size")` becomes `@wasm.memory.size()`
  * Add an enum to represent operations
    * Alternatively we can convert operations to be primitive calls high up