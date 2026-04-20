// TODO: Document this file
parser grammar DecafParser;

// Disable cSharp CLS compliance warnings
@parser::header {#pragma warning disable 3021}
options { tokenVocab=DecafLexer; }

// --- Code Units ---

// A program is a list of modules.
program: module_decl+;

// A module is a named collection of statements, it serves as a container for functions and variables.
module_decl: MODULE name=id_location LBRACE imports=import_stmt* stmts=statement* RBRACE SEMI?;

// An import statement is a special top level statement that represents a WebAssembly import.
import_stmt: IMPORT WASM name=id_location COLON typ=type FROM source=STRINGLIT SEMI;

// --- Statements ---
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
  
block_stmt: LBRACE statement* RBRACE SEMI?;

// Variables
// TODO: Add a mechanism for marking variables as public
var_decl_stmt: LET binds=var_bind_list SEMI;
var_bind_list: var_bind (COMMA var_bind)*;
var_bind: name=id_location (COLON typ=type)? (ASSIGN init=expr)?;

assign_stmt: location ASSIGN expr SEMI;

// Control Flow
if_stmt: IF LPAREN condition=expr RPAREN trueBranch=statement (ELSE falseBranch=statement)?;
while_stmt: WHILE LPAREN condition=expr RPAREN body=statement;

// Other
return_stmt: RETURN value=expr? SEMI;
continue_stmt: CONTINUE SEMI;
break_stmt: BREAK SEMI;
expr_stmt: expr SEMI;

// --- Expressions ---
expr
  // TODO: Test Precedence
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
  | lhs=expr op=(BAND | BOR) rhs=expr          # BinopExpr
  | lhs=expr op=(BLSHIFT | BRSHIFT) rhs=expr   # BinopExpr
  // Other expressions
  | call_expr                          # CallExpr
  | array_init_expr                    # ArrayInitExpr
  | location_expr                      # LocationExpr
  | literal_expr                       # LiteralExpr;

// NOTE: We treat `@` as a primitive callout, this way we can support callouts without needing special syntax
call_expr: callee=location LPAREN args=call_expr_args? RPAREN;
call_expr_args: expr (COMMA expr)*;

array_init_expr: NEW typ=type LBRACK size=expr RBRACK;

location_expr: location;

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

// --- Types ---
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

// --- Locations ---
location: array_location;
array_location: member_location (LBRACK index_expr=expr RBRACK)?;
member_location: root=id_location member=member_list?;
member_list: (DOT identifier)+;
id_location: identifier | PRIMID;
identifier: ID;