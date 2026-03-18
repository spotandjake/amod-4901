# Scope Mapping Raw Rules
+[x] No duplicate identifier is declared in the same scope
+[x] No identifier is used before it is declared
+[x] The `extends` construct must name a previously declared class; a class cannot extend itself.
+[ ] In calls to `new T()`, `T` must be the name of a class previously defined in the global scope.
# Semantic Scoping Raw Rules
+[x] The program should contain a definition for a class called `Program` with a method called `Main` that has no parameters and returns void
# Type Checking Raw Rules
+ [x] Method Call TypeChecking
  + [x] The number of arguments in a method call must be the same as the number of parameters in the method definition
  + [x] Method calls used as expressions must return a result
    + This will be implicit given void is not a valid type for an expression
  + [x] The type of the arguments in a method call must match the type of parameters
+ [x] Statement TypeChecking
  + [x] A `return` statement must not have a return value unless it appears in the body of a method that is to declared to return a value.
    + Put simply a `return` statements type must match a method's return type.
      + This rule will be implicit given `void` is not a value and therefore the only way to match it will be an empty return.
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
+ [ ]  A field of class `T1` can only be accessed outside  an operation for `t1` if the caller is an operation in class `T2` such that `T2` is derived from `T1` 
+ [ ]  The same name cannot be used for a field in two seperate classes when one class is an ancestor of the other.
+ [ ]  The type of `expr` in an assignment must be compatible with the type of the `Location`
+ [ ]  If `t2` is derived from `t1` and `<id>` is a method name defined in both `t1` and `t2` then `id` must have exactly the same signature in `t1` and `t2`

# Raw Rules
1. The program should contain a definition for a class called `Program` with a method called `Main` that has no parameters and returns void

# SubTyping
8.  A field of class `T1` can only be accessed outside  an operation for `t1` if the caller is an operation in class `T2` such that `T2` is derived from `T1` 
9.  The `extends` construct must name a previously declared class; a class cannot extend itself.
12. The same name cannot be used for a field in two seperate classes when one class is an ancestor of the other.
14. The type of `expr` in an assignment must be compatible with the type of the `Location`
15. If `t2` is derived from `t1` and `<id>` is a method name defined in both `t1` and `t2` then `id` must have exactly the same signature in `t1` and `t2`