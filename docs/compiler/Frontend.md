# Front end

This document exists to provide a high-level overview of the frontend of the compiler. More specific documentation can be found in the source code itself, and in the documentation for the individual components of the frontend.

The frontend of the compiler is responsible for taking the source code be it from, a file, string or any other source of text and converting it into a `ParseTree`.

The `ParseTree` lives in `decaf/IR/ParseTree.cs` and is a simple representation of the structure of the source code itself. It serves to provide a contextual representation of the source code with the information useful for compiling, validation and other tasks (such as formatting) without needing to keep the original source code around.

The frontend is made up of several components (sub stages) that work together to produce a well formed and validated `ParseTree`. These components are described in detail below.

## Lexing

Our language uses [ANTLR](https://www.antlr.org/) for lexing and parsing, more specific lexer documentation can be found [here](https://github.com/antlr/antlr4/blob/master/doc/lexer-rules.md). Our lexer is defined in `decaf/Frontend/DecafLexer.g4` and is based off the provided `expresso` spec, with some modifications to make it more suitable for our purposes. (Classes switched to modules, some additional keywords).

The lexer will take the inputs source code and convert it into a stream of tokens, which can then be used by the parser to interpret the structure of the source code. The lexer is responsible for identifying the different components of the source code such as keywords, identifiers, literals and other symbols, and converting them into a format the parser can understand. While parsing isn't strictly required for the compiler to function, parsing can be done over the text input itself, it greatly simplifies the process and reduces the amount of work required to be done by the parser.

Below is a list of tokens defined by the lexer, these are the different components of the source code that the lexer will identify and convert into a format the parser can understand (This list may fall out of date the grammar is the ultimate source of truth):

* Keywords (Meaningful words within the language)
  * Code Units
    * MODULE: "module" - This is equivalent to `class` in other languages
    * IMPORT: "import" - This is used to import modules from other files
    * FROM: "from" - This is used in conjunction with `import` to specify the module to import from
    * WASM: "wasm" - This is used to specify that the import is a wasm import
      * Currently we only support wasm imports
  * Types
    * INT: "int"
    * BOOLEAN: "boolean"
    * CHAR: "char"
    * STRING: "string"
    * VOID: "void"
  * Instructions
    * BREAK: "break"
    * CONTINUE: "continue"
    * RETURN: "return"
    * NEW: "new"
    * LET: "let" - This is equivalent to `var` in other languages
  * Control Flow
    * IF: "if"
    * ELSE: "else"
    * WHILE: "while"
    * FOR: "for"
  * Values
    * TRUE: "true"
    * FALSE: "false"
  * Operators
    * Prefix
      * NOT: "!"
      * BITWISE_NOT: "~"
    * Arithmetic
      * PLUS: "+"
      * MINUS: "-"
      * MULTIPLY: "*"
      * DIVIDE: "/"
    * Relational
      * LESS_THAN: "<"
      * GREATER_THAN: ">"
      * LESS_THAN_OR_EQUAL: "<="
      * GREATER_THAN_OR_EQUAL: ">="
    * Equality
      * EQUAL: "=="
      * NOT_EQUAL: "!="
    * Conditional
      * AND: "&&"
      * OR: "||"
    * Bitwise
      * BITWISE_AND: "&"
      * BITWISE_OR: "|"
      * BITWISE_SHIFT_LEFT: "<<"
      * BITWISE_SHIFT_RIGHT: ">>"
    * Punctuation
      * LPAREN: "("
      * RPAREN: ")"
      * LBRACE: "{"
      * RBRACE: "}"
      * LBRACK: "["
      * RBRACK: "]"
      * SEMI: ";"
      * COMMA: ","
      * DOT: "."
      * COLON: ":"
      * ARROW: "=>"
      * ASSIGN: "="
      * ASSIGN_ADD: "+="
      * ASSIGN_SUB: "-="
      * ASSIGN_MUL: "*="
      * ASSIGN_DIV: "/="
  * Literals
    * Integer Literals: A sequence of digits (e.g. `123`)
    * NOTE: We lex the bool tokens so we don't need a bool literal token itself
    * Character Literals: A single character enclosed in single quotes (e.g. `'a'`)
    * String Literals: A sequence of characters enclosed in double quotes (e.g. `"hello world"`)
  * Attributes (These are not tokens but they are interpreted by the lexer)
    * WSS:  ` ` or `\t` or `NEWLINE` - This is used to skip over whitespace and newlines
    * Comments: `'//' ~[\r\n]*` - This is used to skip over comments
    * Newline: `\r\n` or `\n` - This is used to skip over newlines
  
Testing for the lexer is done in `decafTests/Frontend/LexerTests.cs` and is done by providing a string input to the lexer and checking that the output tokens match the expected tokens. The tests ensure that every valid token is correctly identified, we leave invalid tokens to be tested along with the parser as there is more context and errors are more noticeable when parsing.

## Parsing

ANTLR is also used for parsing, documentation for working with ANTLR parsers can be found [here](https://github.com/antlr/antlr4/blob/master/doc/parser-rules.md). ANTLR generates a parser based on the grammar defined in `decaf/Frontend/DecafParser.g4`, this grammar is based off the provided `expresso` spec with some modifications to make it more suitable for our purposes (Classes switched to modules, drop extends, drop new classes, support string literals, etc). By default ANTLR generates a parser similar to a custom recursive descent parser based off LL-style tables. This allows us to quickly parse the program in `O(1)` time without looking ahead or backtracking, this does come off with the trade off that the grammar needs to be LL(1) in other words we cannot have left recursion in the grammar.

Antlr generates `decaf/Frontend/DecafParser.cs` from the grammar at `decaf/Frontend/DecafParser.g4`, this is a programmatic implementation of an LL parser.

After parsing we map the ANTLR parse contexts to the `ParseTree` format, so we can work with it throughout the rest of the compiler, this is done by `decaf/Frontend/ParseTreeMapper.cs`. During this mapping we also do some basic validation related to functions only being in the top scope. The benefit to doing this validation during the mapping stage rather than in the parser is our grammar can be far simpler and we can provide better error messages with more context.

We use snapshot testing to test the parser as structural testing isn't overly suitable, the downside to snapshot testing is care needs to be taken to ensure the snapshots are correct and capture the correct behavior, tests can be found in `decafTests/Frontend/ParserTests.cs` and are implemented by providing a string input to the parser and checking that the output parse tree matches the expected parse tree. We also test that invalid programs produce the correct errors.

## Semantic Analysis

Once we have a parse tree we can move on to the next stage which is validation, our parser is implemented in a way that it can parse many programs that are syntactically correct but the programs still may be semantically incorrect. Semantically incorrect could refer to many things a few examples include:
* Using a variable that hasn't been declared
* Using a variable that is out of scope
* Using `break` or `continue` outside of a loop
* Using `return` outside of a function

In order to catch these mistakes we need to traverse the program and ensure that the parseTree makes sense, this is probably the least formal part of the compiler as there is no clear 1 size fits all approach to doing this. We implement this in two stages `ScopeValidation` and `SemanticValidation`. These stages are further described below.

### Scope Validation

The first part of semantic validation is scope validation, this is implemented in `decaf/Frontend/ScopeChecker.cs` and takes advantage of our generic `Scope` table. We walk the parseTree by recursively visiting each node and keeping track of the current scope, when visiting we create a new node whenever we enter a `Program`, `Module`, `Function` or `Block` node and we add any variables from the given contexts to the scope. A scope is allowed to capture the parent scope so we can look up variables from the parent scope however the child can also shadow variables from the parent scope. During this traversal whenever we encounter a declaration we add the table to scope, if it already exists in the given scope we throw a `DuplicateDeclarationException`. Whenever we encounter a variable usage we look up the variable to validate that it exists in the current scope or any parent scope, if it doesn't exist we throw an `DeclarationNotDefinedException`. When we are adding to the scope table we also track the mutability of the variable, this allows us to provide better error messages and prevent things such as assignments to `parameters` and `functions` which are immutable by definition, if a mutation occurs we throw a `DeclarationNotMutableException`.

Tests for scope validation can be found in `decafTests/Frontend/ScopeTests.cs` and are implemented by providing a string input and checking weather the validation passes or fails with the expected error. 

### Semantic Validation

After we have done scope validation we can move onto general semantic validation this includes tests for things such as:
* Every program should contain a `Program` module which we can use as the program entry point.
* Ensure that `break` and `continue` are only used within loops.
* Ensure that `return` is only used within functions.
* Ensure that `FunctionLiterals` are only bound at the top level and used in the left hand side of the bind.
  * We validate this in multiple places such as mapping from the parse tree, semantic validation, and type checking to ensure we don't miss an edge case (This is done because we do not track the invariant in the `ParseTree` itself as that would break our type hierarchy)
  * One benefit of this approach is in the future it would be relatively easy to relax this rule and allow function literals to be used in a first class manner and defined in a first class manner (this is part of why we do it this way).
* Check for cases of `x / 0` where `x` is any expression.
  * As a note this test case is only able to catch cases where the `0` is a constant if you do `x / y` and `y` resolves to a `0` at runtime we won't be able to catch that (most compilers won't be able to catch this however).
* Ensure that arrays are not intialized with a negative size or indexed with a negative index.
  * This is similar to the divide by zero case where we can only catch constant cases.

In order to catch these mistakes we traverse the program in `decaf/Frontend/SemanticAnalysis.cs` and check pretty explicitly for these cases, if we find one we throw the corresponding error with as much context as possible. Tests for semantic validation can be found in `decafTests/Frontend/SemanticTests.cs` and are implemented by providing a string input and checking weather the validation passes or fails with the expected error.