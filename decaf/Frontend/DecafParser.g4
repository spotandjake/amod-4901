parser grammar DecafParser;

// Disable cSharp CLS compliance warnings
@parser::header {#pragma warning disable 3021}
options {
  tokenVocab=DecafLexer;
}

program: class_decl+;

class_decl: CLASS name=ID (EXTENDS superClassName=ID)? LBRACE var_decl* method_decl* RBRACE;

var_decl: typ=type binds=var_bind_list SEMI;
var_bind_list: var_bind (COMMA var_bind)*;
var_bind: name=ID (LBRACK INTLIT? RBRACK)?;

method_decl: returnType=type name=ID LPAREN parameters=method_decl_param_list? RPAREN body=block;
method_decl_param_list: method_decl_param (COMMA method_decl_param)*;
method_decl_param: typ=type name=ID (LBRACK RBRACK)?;

block: LBRACE var_decl* statement* RBRACE;

// TODO: Later Restrict VOID to function returns semantically
// TODO: Consider Allowing ArrayTypes - Restrict Semantically
// NOTE: Because this takes id maybe we should just restrict semantically later
type: 
  INT # IntType
  | BOOLEAN # BooleanType
  | VOID # VoidType
  | ID # CustomType
  ;

statement:
  assign_stmt # AssignStatement
  | expression_stmt # ExpressionStatement
  | if_stmt # IfStatement
  | while_stmt # WhileStatement
  | return_stmt # ReturnStatement
  | block # BlockStatement
  ;

assign_stmt: location ASSIGN expr SEMI;
expression_stmt: call_expr SEMI # CallExpressionStatement;
if_stmt: IF LPAREN condition=expr RPAREN trueBranch=block (ELSE falseBranch=block)?;
while_stmt: WHILE LPAREN condition=expr RPAREN body=block;
return_stmt: RETURN value=expr? SEMI;

// TODO: Disallow ID[expr] in semantic analysis
call_expr: 
  method_call # MethodCallExpr
  | prim_callout # PrimCalloutExpr
  ;
method_call: methodPath=location LPAREN args=method_call_args? RPAREN;
method_call_args: expr (COMMA expr)*;
prim_callout: CALLOUT LPAREN primId=STRINGLIT args=prim_callout_args RPAREN;
prim_callout_args: (COMMA (expr | STRINGLIT))*;

expr:
  simple_expr # SimpleExpr
  | NEW ID LPAREN RPAREN # NewObjectExpr
  | NEW type (expr)? # NewArrayExpr
  | literal # LiteralExpr
  | op=NOT operand=expr # NotExpr // TODO: Generalize this to a prefix expr
  | lhs=expr op=bin_op rhs=expr # BinaryOpExpr // TODO: Generalize this into a binop expression
  | LPAREN expr RPAREN # ParenExpr
  ;

simple_expr:
  location # LocationExpr
  | THIS # ThisExpr // TODO: Consider handling this as a regular ID
  | call_expr # CallExpr
  ;

location: root=ID (path=location_path | indexExpr=location_array_index)?;
location_path: DOT ID; // TODO: Allow paths like root.path1.path2 -- Restrict Semantically
location_array_index: LBRACK expr RBRACK; // TODO: Allow nested array paths -- Restrict Semantically

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