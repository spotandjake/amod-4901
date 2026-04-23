# Language Reference

This file serves to provide a very high level overview of the Decaf language. For more details, please refer to the [DecafParser.g4](../decaf/Frontend/DecafParser.g4) file, which contains the full grammar of the language.


### Code Units
Code units are the top level constructs in Decaf. They include `programs` and `modules`.

#### Programs

A program is the main entry point of a decaf application, it's really just any decaf file that contains code. At it's heart a program is just a collection of modules.

```antlr
program: module_decl+;
```

#### Modules

A module is the main organization unit in decaf. You can think of it as similar to a namespace or static class in c#. At their core modules are really just static classes that can contain functions, variables and any other statement.

The grammar of a module is below:
```antlr
module_decl: MODULE name=id_location LBRACE imports=import_stmt* stmts=statement* RBRACE SEMI?;
```

Modules are made up of a name, optional imports placed at the top of the module and a body made up of statements. The name of the module is used to reference it from other modules, it can also be used internally to reference itself. The imports are used to import things from the wasm sandbox as opposed to other decaf modules (we do not support linking decaf modules), and the body is where all the code of the module goes.

An example module is below:
```decaf
module Program {
  Runtime.print("Hello World!\n");
}
```

Any bind within the top level of the module is automatically considered public and exposed to other modules. This means that if you have a function or variable defined at the top level of the module, it can be accessed from other modules by referencing the module name followed by the bind name. For example, if we had a function called `foo` defined at the top level of the `Program` module, we could access it from another module like this: `Program.foo()`. Additionally even though functions are considered literals they are semantically restricted to only be defined at the top level of a module, you can read more in the function literal section below.

##### Imports

Imports can only appear at the top of the module, and are used to import things from the wasm host, you can read more about [wasm imports here](https://chicory.dev/docs/usage/host-functions/). The grammar for imports is below:
```antlr
import_stmt: IMPORT WASM name=id_location COLON typ=type FROM source=STRINGLIT SEMI;
```

We use the `WASM` keyword here so in the future we could allow linking decaf files in a non breaking manner however it's not supported at the moment an example of imports is also below:
```decaf
module Program {
  import wasm print: (string) -> void from "env";
}
```
This would import `env.print` from the host and make it available in the `Program` module as `print`, so we could call it like this: `print("Hello World!\n")` or `Program.print("Hello World!\n")`.

### Statements

A statement in our language is anything that goes inside the body of a module, as a rule of thumb statements themselves are operations that always return void, and they can be used to change the state of the program or perform some side effect, additionally we define an expression statement which allows any expression to be used as a statement. The grammar for statements is below:
```antlr
statement
  : block_stmt # BlockStmt
  // Variables
  | var_decl_stmt # VarDeclStmt
  | assign_stmt # AssignStmt
  // Control Flow
  | if_stmt # IfStmt
  | while_stmt # WhileStmt
  // Other statements
  | return_stmt # ReturnStmt
  | continue_stmt # ContinueStmt
  | break_stmt # BreakStmt
  | expr_stmt # ExprStmt;
```

#### Block Statements

A block statement is defined as a sequence of statements enclosed in curly braces, they are used to group statements together and create a new scope for variables. The grammar for block statements is below:
```antlr
block_stmt: LBRACE stmts=statement* RBRACE;
```
The `*` indicates that block statements are allowed to be empty, so you can have an empty block statement like this: `{}`. Block statements are also used in control flow statements like if statements and while statements, you can read more about that in the control flow section below.

#### Variable Statements
A variable statement is really just a category of statement, that indicates the statement is interacting with variable in some way.

##### Variable Declarations

A variable declaration statement, is a variable statement used to introduce new variables into the given scope. The grammar for variable declarations is below:
```antlr
var_decl_stmt: LET binds=var_bind_list SEMI;
var_bind_list: var_bind (COMMA var_bind)*;
var_bind: name=id_location (COLON typ=type)? (ASSIGN init=expr)?;
```

In practice a few syntax examples are given below:
```decaf
let x: int = 1;

let a: int = 1, b: int = 2, c: int = 3;
```

This would declare a variable `x` of type `int` and initialize it to `1`, and then declare three variables `a`, `b` and `c` of type `int` and initialize them to `1`, `2` and `3` respectively. Note even though the type annotation is optional, it's semantically required for anything but a function declaration.

##### Assignments

Assignments work very similarly to variable declarations, however instead of introducing new varables into the scope, they are used to change the value of an existing variable. The grammar for assignments is below:
```antlr
assign_stmt: location op=assign_stmt_op expr SEMI;
assign_stmt_op
  : ASSIGN # Assign
  | ASSIGN_ADD # AssignAdd
  | ASSIGN_SUB # AssignSub
  | ASSIGN_MUL # AssignMul
  | ASSIGN_DIV # AssignDiv
  ;
```

There are multiple different types of assignment statements, the most basic one is the simple assignment which uses the `=` operator and is used to assign a new value to a variable, for example:
```decaf
x = 2;
```
This would change the value of `x` to `2`. The other types of assignment statements are compound assignment statements, which are used to perform an operation on the variable and then assign the result back to the variable, for example:
```decaf
x += 1; // This is equivalent to x = x + 1;
```
This would increment the value of `x` by `1`. The other compound assignment statements are `-=`, `*=`, and `/=`, which are used to perform subtraction, multiplication and division respectively.

#### Control Flow

Control flow statements are used to change the flow of execution of the program, they are used to create loops and conditional statements. Similar to variable statements, control flow statements are really just a category of statements that indicate the statement is used to control the flow of the program.

##### If Statements

If statements are the primary way to create conditional statements in decaf, they are used to execute a block of code only if a certain condition is true. The grammar for if statements is below:
```antlr
if_stmt: IF LPAREN condition=expr RPAREN trueBranch=statement (ELSE falseBranch=statement)?;
```

Syntactically if statements come in two forms, the first form is the simple if statement which only has a true branch and no false branch, for example:
```decaf
if (x > 0) {
  print("x is positive\n");
}
```
This would print "x is positive" if `x` is greater than `0`. The second form of if statements is the if-else statement which has both a true branch and a false branch, for example:
```decaf
if (x > 0) {
  print("x is positive\n");
} else {
  print("x is not positive\n");
}
```
This would print "x is positive" if `x` is greater than `0`, and "x is not positive" otherwise.

It is important to note that the choice to use block statements as the body of the if statement is optional, so you could also write the above example like this:
```decaf
if (x > 0) print("x is positive\n");
else print("x is not positive\n");
```

##### While Statements

While statements are used to create loops in decaf, very similar to how they are used in any other language. The grammar for while statements is below:
```antlr
while_stmt: WHILE LPAREN condition=expr RPAREN body=statement;
```

In practice while statements look like this:
```decaf
while (x > 0) {
  print("x is positive\n");
  x -= 1;
}
```
This would print "x is positive" and decrement `x` by `1` as long as `x` is greater than `0`. Similar to if statements the choice to use a block statement as the body of the while statement is optional.

#### Return Statements

Return statements are used to return a value or just return from a function, they are only valid inside function bodies and will cause a compilation error if used outside of a function body. The grammar for return statements is below:
```antlr
return_stmt: RETURN expr? SEMI;
```

A function that returns a value expects the return statement to have an expression, just as a function returning void expects the return statement to not have an expression.

#### Continue Statements
Continue statements are used to skip the rest of the current iteration of a loop and move on to the next iteration, they are only valid inside loops and will cause a compilation error if used outside of a loop. The grammar for continue statements is below:
```antlr
continue_stmt: CONTINUE SEMI;
```

Syntactically continue statements are very simple, they just consist of the `continue` keyword followed by a semicolon. An example below:
```decaf
while (x > 0) {
  x -= 1;
  if (x % 2 == 0) continue;
  print("x is odd\n");
}
```
This would print "x is odd" for every odd value of `x` that is greater than `0`, and it would skip the print statement for every even value of `x`.

#### Break Statements

Break statements are the complement to continue statements, they are used to exit a loop early, they are also only valid inside loops and will cause a compilation error if used outside of a loop. The grammar for break statements is below:
```antlr
break_stmt: BREAK SEMI;
```

Syntactically break statements are also very simple, they just consist of the `break` keyword followed by a semicolon. An example below:
```decaf
while (x > 0) {
  if (x == 5) break;
  print("x is less than 5\n");
  x -= 1;
}
```
This would print "x is greater than 5" for every value of `x` that is greater than `5`, and it would exit the loop when `x` is equal to `5`.

#### Expression Statements

Expression statements are used as a trampoline to allow any expression to be used as a statement, they are used to allow function calls and other expressions that have side effects to be used as statements. You can also perform expressions that do not have side effects however doing something like `1 + 2;` wouldn't be very useful given that the result goes nowhere. The grammar for expression statements is below:
```antlr
expr_stmt: expr SEMI;
```

### Expressions

Expressions are the primary way to perform computations and produce values in decaf, they are used to create new values, call functions, access variables and perform operations. The grammar for expressions is below:
```antlr
  // Parenthesized expressions
  : LPAREN expr RPAREN                 # ParenExpr // NOTE: This is only for grouping, not in the IR
  // Prefix Expr
  | op=NOT operand=expr                # PrefixExpr
  | op=BNOT operand=expr               # PrefixExpr
  // Binary expressions - Arithmetic
  | lhs=expr op=(MULT | DIV) rhs=expr          # BinopExpr
  | lhs=expr op=(PLUS | MINUS) rhs=expr        # BinopExpr
  // Binary expressions - Relational
  | lhs=expr op=(LT | GT | LEQ | GEQ) rhs=expr # BinopExpr
  // Binary expressions - Equality
  | lhs=expr op=(EQ | NEQ) rhs=expr            # BinopExpr
  // Binary expressions - Conditional
  | lhs=expr op=AND rhs=expr                   # BinopExpr
  | lhs=expr op=OR rhs=expr                    # BinopExpr
  // Binary expressions - Bitwise
  | lhs=expr op=(BLSHIFT | BRSHIFT) rhs=expr   # BinopExpr
  | lhs=expr op=(BAND | BOR) rhs=expr          # BinopExpr
  // Other expressions
  | call_expr                          # CallExpr
  | array_init_expr                    # ArrayInitExpr
  | location_expr                      # LocationExpr
  | literal_expr                       # LiteralExpr;
``` 

I know the expression grammar is a bit dense but we can clean it up a little into:
```antlr
  // Parenthesized expressions
  : LPAREN expr RPAREN                 # ParenExpr // NOTE: This is only for grouping, not in the IR
  // Prefix Expr
  | prefix_expr                        # PrefixExpr
  // Binop Expressions
  | binop_expr                         # BinopExpr
  // Other expressions
  | call_expr                          # CallExpr
  | array_init_expr                    # ArrayInitExpr
  | location_expr                      # LocationExpr
  | literal_expr                       # LiteralExpr;
```

The main reason for writing the more explicit grammar is it makes precedence parsing easier, as antlr will match expression in descending precedence order. We will cover this in more detail when discussing binops.

#### Parenthesized Expressions

The first type of expression is the parenthesized expression, which is used to group expressions together and override the default precedence of operators. The grammar for parenthesized expressions is below:
```antlr
expr: LPAREN expr RPAREN # ParenExpr;
```

And in practice it looks like this:
```decaf
(1 + 2) * 3;
```
This would evaluate `1 + 2` first and then multiply the result by `3`, resulting in `9`. Without the parentheses, the expression would be evaluated as `1 + (2 * 3)`, resulting in `7`.

#### Prefix Expressions

Prefix expressions are used to perform operations on a single operand, prefix indicates that the operator comes before the operand. The grammar for prefix expressions is below:
```antlr
expr: op=NOT operand=expr # PrefixExpr
    | op=BNOT operand=expr # PrefixExpr;
```

There are two types of prefix expression in decaf, the first one is the logical not operator which uses the `!` symbol and is used to negate a boolean value, for example:
```decaf
!true; // This would evaluate to false
```
The second type of prefix expression is the bitwise not operator which uses the `~` symbol and is used to perform a bitwise not operation on an integer value, for example:
```decaf
~0b1010; // This would evaluate to 0b0101
```

Prefix expressions are right associative, which means that if you have multiple prefix operators in a row, they will be evaluated from right to left. For example:
```decaf
!!true; // This would evaluate to true
```
In this example, the inner `!true` is evaluated first, resulting in `false`, and then the outer `!false` is evaluated, resulting in `true`.

#### Binary Expressions

Binary expressions are used to perform operations on two operands, the operator comes between the two operands. The grammar for binary expressions is below:
```antlr
expr: lhs=expr op=(MULT | DIV) rhs=expr          # BinopExpr
    | lhs=expr op=(PLUS | MINUS) rhs=expr        # BinopExpr
    | lhs=expr op=(LT | GT | LEQ | GEQ) rhs=expr # BinopExpr
    | lhs=expr op=(EQ | NEQ) rhs=expr            # BinopExpr
    | lhs=expr op=AND rhs=expr                   # BinopExpr
    | lhs=expr op=OR rhs=expr                    # BinopExpr
    | lhs=expr op=(BLSHIFT | BRSHIFT) rhs=expr   # BinopExpr
    | lhs=expr op=(BAND | BOR) rhs=expr          # BinopExpr;
```

The precedence of binary operators in decaf is determined by the order in which they are defined in the grammar, with operators defined earlier having higher precedence than operators defined later. The precedence of binary operators from highest to lowest is as follows:
1. Multiplication and Division (`*`, `/`)
2. Addition and Subtraction (`+`, `-`)
3. Relational Operators (`<`, `>`, `<=`, `>=`)
4. Equality Operators (`==`, `!=`)
5. Logical AND (`&&`)
6. Logical OR (`||`)
7. Bitwise Shift Operators (`<<`, `>>`)
8. Bitwise AND and OR (`&`, `|`)

We don't quite have time to go through all the different binary operators here however googling the symbol along with the name of the operator should give you a good idea of what it does as they work similarly to any other language.

I will however show a simple example of math in decaf using binary operators:
```decaf
(1 + 2) * 3 - 4 / 2;
```
This would evaluate to `7`, as the expression is evaluated as follows:
1. `1 + 2` is evaluated first, resulting in `3`.
2. `3 * 3` is evaluated next, resulting in `9`.
3. `4 / 2` is evaluated next, resulting in `2`.
4. Finally, `9 - 2` is evaluated, resulting in `7`.

#### Function Calls
Function calls are probably the most useful type of expression in decaf, they are used to call functions and pass arguments to them. The grammar for function calls is below:
```antlr
// NOTE: We treat `@` as a primitive callout, this way we can support callouts without needing special syntax
call_expr: callee=location LPAREN args=call_expr_args? RPAREN;
call_expr_args: expr (COMMA expr)*;
```

Function calls consist of a callee, which is the function being called, and a list of arguments, which are the values being passed to the function. The callee is represented as a location expression, which means that you can call functions that are stored in variables or accessed through modules. For example:
```decaf
Runtime.print("Hello World!\n");
```
This would call the `print` function that is defined in the `Runtime` module and pass the string "Hello World!\n" as an argument. You can also call functions that are stored in variables, for example:
```decaf
let printFunc: (string) => void = Runtime.print;
printFunc("Hello World!\n");
```
This would achieve the same result as the previous example, but it demonstrates that you can store functions in variables and call them through those variables.

There is a second type of function call in decaf which is the callout, callouts are used to call intrinsics, or functions that are defined by the compiler, they are represented as a function call with a callee that starts with the `@` symbol. For example:
```decaf
@wasm.memory.size();
```

As a user of the language these won't be used much however they are used heavily throughout the runtime and standard library to implement various higher level features, we use intrinsics specifically to expose wasm instructions that don't have a natural representation in decaf, for example the `memory.size` intrinsic is used to get the current size of the wasm memory, which is something that doesn't have a natural representation in decaf.

#### Array Initializers

Array initializers are used to create new arrays and initialize them with values, the grammar for array initializers is below:
```antlr
array_init_expr: NEW typ=type LBRACK size=expr RBRACK;
```

When you initalize a new array you need to specify the type of the elements in the array and the size of the array, for example:
```decaf
let arr: int[] = new int[5];
```
This would create a new array of integers with a size of `5` and store it in the variable `arr`. The elements of the array would be initialized to the default value for the type, which is `0` for integers, in general the default value for a type is `0` for numeric types, `false` for booleans, `'\0'` for characters and currently arrays of reference types are not directly supported but if they were they would be initialized to `null` pointers (note decaf doesn't define `null`).

It's important to note that arrays in decaf are heap allocated, read the `./dataRepresentation.md` file for more details on how arrays are represented in memory. However the side effect is that unless `Runtime.free(@getPointer(arr))` is called, the memory used by the array will not be freed, which can lead to memory leaks if not handled properly, this is something to be aware of when using arrays in decaf, especially if you are creating a lot of them in a loop or in a long running function.

#### Location Expressions
Location expressions are used to access variables and functions, they are very similar to the concept of expression statements where they just serve as a trampoline to allow locations to be used as expressions, the grammar for location expressions is below:
```antlr
location_expr: location;
```

#### Literal Expressions

Literal expressions are the heart and soul of any programming language, they are used to represent constant values in the code. The grammar for literal expressions is below:
```antlr
literal_expr: literal;

// --- Literals ---
literal
  : int_literal # IntLit
  | bool_literal # BoolLit
  | char_literal # CharLit
  | string_literal # StringLit
  | func_literal # FuncLit;
int_literal: INTLIT;
bool_literal: TRUE | FALSE;
char_literal: CHARLIT;
string_literal: STRINGLIT;

func_literal: func_literal_params COLON returnType=type ARROW block_stmt;
func_literal_params: LPAREN func_param_list? RPAREN;
func_param_list: func_param (COMMA func_param)*;
func_param: name=id_location COLON typ=type;
```

There are currently 5 types of literals in decaf, integer literals, boolean literals, character literals, string literals and function literals. Integer literals are used to represent constant integer values, boolean literals are used to represent constant boolean values, character literals are used to represent constant character values, string literals are used to represent constant string values and function literals are used to represent anonymous functions.

##### Integer Literals

Integer literals are represented as a sequence of digits, and must be written in base 10. For example:
```decaf
let x: int = 123;
```
This would create a new integer variable `x` and initialize it with the value `123`.

Integers are 32-bit signed values in decaf, which means they can represent values from `-2,147,483,648` to `2,147,483,647`. It's important to note that while the grammar allows you to write integer literals that are outside of this range, doing so would result in undefined behavior very likely an underflow or overflow. Wasm by default does not interpret integers as signed or unsigned with the only way to tell being the signedness of the operations you perform on them, for example if you perform a right shift on an integer it will be treated as a signed integer, while if you perform an unsigned right shift it will be treated as an unsigned integer. In decaf we treat all integers as signed by default, and we do not currently have any syntax for unsigned integers.

##### Boolean Literals
Boolean literals are represented by the keywords `true` and `false`, for example:
```decaf
let isTrue: bool = true;
```
This would create a new boolean variable `isTrue` and initialize it with the value `true`.

##### Character Literals
Character literals are represented by a single character enclosed in single quotes, for example:
```decaf
let charA: char = 'a';
```

##### String Literals
String literals are represented by a sequence of characters enclosed in double quotes, for example:
```decaf
let greeting: string = "Hello, World!";
```
It's important to note that string literals are heap allocated, read the `./dataRepresentation.md` file for more details on how strings are represented in memory. However the side effect is that unless `Runtime.free(@getPointer(greeting))` is called, the memory used by the string literal will not be freed, which can lead to memory leaks if not handled properly, this is something to be aware of when using string literals in decaf, especially if you are creating a lot of them in a loop or in a long running function.


##### Function Literals
Function literals are the heart and soul of programming, they are used to define functions, the grammar for function literals is below:
```antlr
func_literal: func_literal_params COLON returnType=type ARROW block_stmt;
func_literal_params: LPAREN func_param_list? RPAREN;
func_param_list: func_param (COMMA func_param)*;
func_param: name=id_location COLON typ=type;
```

Notably, while function literals are parsed like any other literal or expression, they are semantically restricted to only be defined at the top level of a module, this is because while functions in decaf are first class citizens, they do not have closures and can only capture variables from the global scope. This means that if you were to define a function literal inside another function or inside a block statement, it would not be able to capture any variables from the enclosing scope, which would make it pretty much useless. For example, the following code would result in a compilation error:
```decaf
module Program {
  let x: int = 1;
  {
    // This is a semantic error because we are inside of another function
    let func = (y: int): int => {
      return x + y;
    };
  }
}
```

However the following code would be perfectly valid:
```decaf
module Program {
  let x: int = 1;
  let func = (y: int): int => {
    return x + y;
  };
}
```

Functions in decaf are first class citizens, which means that they can be stored in variables, passed as arguments to other functions and returned from functions. However they do not have closures, which means that they cannot capture variables from the enclosing scope, they can only capture variables from the global scope. This is a design choice that was made to keep the language simple and to avoid the complexities of implementing closures, however it does mean that function literals are somewhat limited in their capabilities compared to languages that do support closures.

### Types

In decaf types are used to specify the type of a variable, the return type of a function and the type of an expression. The grammar for types is below:
```antlr
type: simple_type (LBRACK RBRACK)?;
simple_type
  : INT # IntType
  | BOOLEAN # BooleanType
  | CHAR # CharType
  | STRING # StringType
  | VOID # VoidType
  | func_type # FuncType;
func_type: LPAREN paramTypes=type_list* RPAREN ARROW returnType=type;
type_list: type (COMMA type)*;
```

#### Array Types
Array types are represented by a simple type followed by square brackets, for example:
```decaf
let arr: int[] = new int[5];
```
This would create a new array of integers with a size of `5` and store it in the variable `arr`. The type of `arr` is `int[]`, which indicates that it is an array of integers.

While we parse arrays of any type, currently we only support arrays on `int`, `bool` and `char`.

#### Simple Types
Simple types are the basic types in decaf, they include `int`, `bool`, `char`, `string` and `void`. They are used to specify the type of the variable and each one has a matching primitive with the exception of void which has no value representation and is used to indicate the absence of a value, for example in functions that do not return anything.

#### Function Types
Function types are used to specify the type of a function, they consist of a list of parameter types and a return type, for example:
```decaf
let func: (int, bool) => string = (x: int, y: bool): string => {
  return "Hello, World!";
};
```

Function types are primarily useful for specifying the types of callbacks and imports in decaf, they can be used to specify the type of a variable that defines a function like above however it's worth noting that decaf can infer the type from the function itself to avoid redundancy.

### Locations

Locations in decaf are used to access variables and functions, there are three primary types of locations in decaf, `array locations`, `member locations`, and `identifier locations`. The grammar for locations is below:
```antlr
location: array_location;
array_location: member_location (LBRACK index_expr=expr RBRACK)?;
member_location: root=id_location member=member_list?;
member_list: (DOT identifier)+;
id_location: identifier | PRIMID;
identifier: ID;
```

The most common type of location is the identifier location, which is used to access variables and functions by their name, for example:
```decaf
let x: int = 1;
let y: int = x + 1;
```
In this example, `x` and `y` are both identifier locations that are used to access the variables `x` and `y` respectively.

The second most common type of location is the member location, which is used to access members of a module or members of a struct (note structs are not currently supported but will be in the future), for example:
```decaf
Runtime.print("Hello, World!\n");
```
In this example, `Runtime.print` is a member location that is used to access the `print` function that is defined in the `Runtime` module.

The final type of location is the array location, which is used to access elements of an array, for example:
```decaf
let arr: int[] = new int[5];
arr[0] = 1;
arr[1] = 2;
```
In this example, `arr[0]` and `arr[1]` are array locations that are used to access the first and second elements of the array `arr` respectively.
