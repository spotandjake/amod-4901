# Frontend

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

## Semantic Analysis

### Scope Validation
### Semantic Validation