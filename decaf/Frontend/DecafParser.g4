parser grammar DecafParser;

// Disable cSharp CLS compliance warnings
@parser::header {#pragma warning disable 3021}
options {
  tokenVocab=DecafLexer;
}

program: class_decl+;

class_decl: CLASS ID (EXTENDS ID)? LBRACE var_decl* method_decl* RBRACE;

method_decl: method_return_type ID method_decl_param block;
method_return_type: type | VOID;

method_decl_param: LPAREN method_decl_param_list? RPAREN;
method_decl_param_list: method_decl_param_typ (COMMA method_decl_param_typ)*;
method_decl_param_typ: type ID (LBRACK RBRACK)?; // TODO: Consider simplifying the array part into type itself

block: LBRACE var_decl* statement* RBRACE;

var_decl: type var_bind_list SEMI;
var_bind_list: var_bind (COMMA var_bind)*;
var_bind: ID (LBRACK optional_int_size RBRACK)?;

// TODO: Consider Having Void as a Type - Restrict Semantically
// TODO: Consider Allowing ArrayTypes - Restrict Semantically
// NOTE: Because this takes id maybe we should just restrict semantically later
type: 
  INT # IntType
  | BOOLEAN # BooleanType
  | ID # CustomType
  ;

statement:
  assign_stmt # AssignStatement
  | method_call SEMI # MethodCallStatement
  | if_stmt # IfStatement
  | while_stmt # WhileStatement
  | return_stmt # ReturnStatement
  | block # BlockStatement
  ;

assign_stmt: location ASSIGN expr SEMI;
if_stmt: IF LPAREN expr RPAREN block (ELSE block)?;
while_stmt: WHILE LPAREN expr RPAREN block;
return_stmt: RETURN expr? SEMI;

// TODO: How do we feel about combining method calls and callouts and just making a callout an id of like `@id`
// TODO: We parse ID[expr] but this isn't valid so we should add an error later
method_call: (location method_call_args?) | callout;
method_call_args: LPAREN expr (COMMA expr)* RPAREN;
callout: CALLOUT callout_args;
callout_args: LPAREN STRINGLIT (COMMA (expr | STRINGLIT))* RPAREN;

expr:
  simple_expr # SimpleExpr
  | NEW ID LPAREN RPAREN # NewObjectExpr // TODO: Develop a better name
  | NEW type (expr)? # NewArrayExpr // TODO: Develop a better name
  | literal # LiteralExpr
  | expr bin_op expr # BinaryOpExpr // TODO: Generalize this into a binop expression
  | NOT expr # NotExpr // TODO: Generalize this to a prefix expr
  | LPAREN expr RPAREN # ParenExpr
  ;

simple_expr: location | THIS | method_call;

location: ID ((DOT ID) | (LBRACK expr RBRACK))?;

// TODO: Figure out precedence
bin_op:
  arith_op | rel_op | eq_op | cond_op;

arith_op: PLUS | MINUS | MULT | DIV;

rel_op: LT | GT | LEQ | GEQ;

eq_op: EQ | NEQ;

cond_op: AND | OR;

// Literals
// TODO: Consider Making String a generic literal value
literal:
  INTLIT # IntLit
  | CHARLIT # CharLit
  | bool_literal # BoolLit
  | NULL # NullLit
  ;

bool_literal: TRUE | FALSE;

// Helpers
optional_int_size: INTLIT?;