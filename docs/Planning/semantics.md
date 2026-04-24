# Scope Mapping Raw Rules
+[x] No duplicate identifier is declared in the same scope
+[x] No identifier is used before it is declared
+[x] The `extends` construct must name a previously declared class; a class cannot extend itself.
+[x] In calls to `new T()`, `T` must be the name of a class previously defined in the global scope.
# Semantic Scoping Raw Rules
+ [x] The program should contain a definition for a class called `Program` with a method called `Main` that has no parameters and returns void
+ [x] No divide by zero
+ [x] No negative immediate array sizes
# Type Checking Rules
+ Method call signatures match
  + Number of params to args
  + Type of params to args
  + Method calls used as expressions must return a result
  + If a method is expecting a return value then we require a return value.
+ Statements
  + A `return` statements value must match the return type of the method it appears in
+ [x] For all method invocations of the form `<simple_Expr>.<id>()`
  + [x] The type of <simple_expr> must be some class type `T`
  + [x] <id> must name one of T`'s methods
  + [x] <id> used as a <location> must name a declared variable or field
+ [x] Every location of the form `<simple_expr>[<expr>]`
  + [x] The type of `<simple_expr>` must be an instantiation of an array
  + [x] The type of `<expr>` must be `int`
+ [x] Every location of the form `<simple_expr>.<id>`
   + [x] The type of `<simple_expr>` must be some class type `T`, or `<simple_expr>` must be `this`, (in which case its type is that of the encloding class)
   + [x] The `<id>` must name one of `T`'s fields
   + [x] The location must appear textually in an method of type `T`.
+ [x] The `<expr>` in an `if` and `while` statements must have type `boolean`
+ [x] The operands of `<arith_ops` and `rel_ops` must have type `int`
+ [x] The operands of `eq_ops` must match `(a, a) => boolean`
+ [x] The operands of `cond_ops` have the signature `(boolean, boolean) => boolean`
+ [x] The operand of a logical not must be `(boolean) => boolean`
+ [x] In calls to `new T[Size]`, size must be an integer and `T` must be either `int`, `boolean`, or the name of a class in the global scope. Note that `T` can not be an array type.