# Middle end

This document exists to provide a high-level overview of the middle end of the compiler. More specific documentation can be found in the source code itself, and in the documentation for the individual components of the middle end.

The middle end of the compiler is responsible for getting us ready to generate code given the `ParseTree` produced by the frontend. Like the frontend the middle end is made up of several components (sub stages) that work together to produce a well formed, validated and lowered `AnfTree` which is the main IR used for code generation. These components are described in detail below.

## Type Checking

[Typechecking](https://en.wikipedia.org/wiki/Type_system#Type_checking) is probably one of the most important things we do in the entire compiler, it ensures that the program not only looks valid but also makes sense, and ensures that we don't try and generate code for something that doesn't make sense such as adding two strings. The typechecker itself is implemented in `decaf/MiddleEnd/TypeChecker.cs` and `decaf/MiddleEnd/TypeCheckerCore.cs`.

The goal of the type checker is to produce a new tree called the `TypedTree`, this is very similar to the `ParseTree` with the main difference being we annotate every expression node with a type and scopes now contain signatures. In order to type check a program we do a simple traversal over the `ParseTree`, we resolve scope information just like we do in `ScopeChecker` in the frontend, but this time when we are resolving a scope we store the signature so when a variable is used we can propagate that information, we also swap all string locations to proper Symbols that are unique throughout the program. Type checking rules and implementation can range in complexity with more complex type checkers having to deal with things like type inference, subtyping, etc. These features make type checkers extremely formal and complex, but for our simple language we can get away with a much simpler implementation. When we encounter an expression we use constants to check if the types of sub expressions make sense when applied. We ensure that method calls match their definitions and that we aren't trying to call something that isn't a method, we check that array indexing is done on arrays, and that definitions match the type of their initializers, etc. If we encounter any type errors we throw an exception and stop compilation.

We can later use this type information during compilation to ensure that we generate the correct code and for example `1.5 + 1.5` should emit an `F32.add` whereas `1 + 1` should emit an `I32.add`. 

The typechecker itself is split into two parts, `decaf/MiddleEnd/TypeChecker.cs` which is responsible for the traversal and building the `TypedTree` and `decaf/MiddleEnd/TypeCheckerCore.cs` which is responsible for the actual type checking rules and logic. This allows us to keep the traversal logic separate from the type checking logic which makes it easier to read and maintain.

Tests for the type checker can be found in `decafTests/MiddleEnd/TypeCheckerTests.cs`.

## Anf Tree

After we have done type checking we could lower to wasm directly however there are still a ton of things that would make it hard todo, we also do not have a very optimized program this is where [ANF](https://en.wikipedia.org/wiki/A-normal_form) comes in. ANF is a simple intermediate representation that makes it easier to generate code and perform optimizations on the program. ANF isn't the only IR that we could have used here with `SSA` or `CPS` being other popular choices however in comparison `ANF` is much simpler to implement and provably provides the same benefits when it comes to code generation and optimizations. The main idea behind ANF is that we want to ensure that every expression is broken down into simple steps, for example instead of having the expression `1 + 2 + 3` we would break it down into something like:
```
let tmp1 = 1 + 2
let tmp2 = tmp1 + 3
```
This makes it easier to analyze as we only have to worry about the left and right hand sides of expressions rather than the compound expression as a whole. 

ANF itself isn't a tree as much as a set of constraints implemented on a tree as such we define a brand new ir called the `AnfTree` which lives in `decaf/IR/AnfTree.cs`, this IR is very similar to the `TypedTree` with the main difference being that `Expressions` become `SimpleExpressions` which can only take `Immediates` (Constants or Variables) as arguments, and functions are hoisted to the module level from being part of the module body itself (this is more similar to wasm). We also use this transformation to begin lowering some of the higher level constructs such as `while loops` into more basic concepts such as `loops` this is closer to wasm and the benefit of lowering at this stage is now when we implement something like `for` loop in the future we already benefit from the same optimizations and code generations patterns as `while` loops.

In order to convert to `ANF` we implement a simple traversal in `decaf/MiddleEnd/AnfMapper.cs` which takes in a `TypedTree` and produces an `AnfTree`. During this traversal we split out complex expressions into simple expressions by introducing temporary variables. Along with performing the other transformations mentioned above such as hoisting functions and lowering while loops.

Tests for the `AnfMapper` can be found in `decafTests/MiddleEnd/AnfTest.cs`.

### Optimizations

Currently we do not implement a lot of optimizations in the middle end however we do implement some simple ones.

#### Dead Code Elimination

The core optimization we have so far is dead code elimination, this is a simple optimization that applies the following rules:
* Anything after a `return` statement is dead code and can be removed.
* Anything after a `continue` statement is dead code and can be removed.
* Anything after a `break` statement is dead code and can be removed.
* `if (true)` can be simplified to just the body of the true branch of the if statement as we know the condition will always be true.
* `if (false)` can be simplified to just the body of the false branch of the if statement as we know the condition will always be false.
* A block with a single statement can be simplified to just that statement.
* A loop that only contains a single `break` statement can be removed as it does not do anything.

This simple set of rules combines to remove a lot of dead code for example lets walk through one of the cooler emergent optimizations we get from this, consider the following code:
```
module Program {
  while (false) {
    print("Hello World");
  }
}
```
The code above gets converted to the following `AnfTree` by our mapper:
```
module Program {
  loop {
    if (false) {
      print("Hello World");
    } else {
      break;
    }
  }
}
```
Now we can apply our `if (false)` rule to simplify the if statement:
```
module Program {
  loop {
    {
      break;
    }
  }
}
```
Now we can apply our block with a single statement rule to simplify the block:
```
module Program {
  loop {
    break;
  }
}
```
Now we can apply our loop with a single break rule to remove the loop:
```
module Program {
}
```

These simple set of rules just combined to remove an entire loop that did not do anything, while being generic enough to handle a wide variety of cases in between along with more complex cases. Combined with future optimizations passes such as constant folding and propagation we can get a lot of optimizations for free just from this simple dead code elimination pass, this is the magic of lowering to an IR like `ANF` as it allows us to apply simple optimizations that have a big impact on the generated code.

Tests for dead code elimination can be found along side the anf tests.
