parser grammar DecafParser;

// Disable cSharp CLS compliance warnings
@parser::header {#pragma warning disable 3021}
options {
  tokenVocab=DecafLexer;
}

program: class_decl+;

class_decl: CLASS ID (EXTENDS ID)? LBRACE var_decl* method_decl* RBRACE;

optional_int_size: INTLIT?;

method_decl: (type | VOID) ID method_decl_param block;

method_decl_param: LPAREN method_decl_param_list? RPAREN;
method_decl_param_list: method_decl_param_typ (COMMA method_decl_param_typ)*;
method_decl_param_typ: (type ID) | (type ID LBRACK RBRACK);

block: type (ID | (ID LBRACK optional_int_size RBRACK))+;

var_decl: type var_bind_list SEMI;
var_bind_list: var_bind (COMMA var_bind)*;
var_bind: ID | (ID LBRACK optional_int_size RBRACK);

type: INT | BOOLEAN | ID;

statement:
  location ASSIGN expr SEMI
  | method_call SEMI
  | IF LPAREN expr RPAREN block (ELSE block)?
  | WHILE LPAREN expr RPAREN block
  | RETURN expr? SEMI
  | block;

method_call:
  method_name LPAREN method_call_param_list? RPAREN
  | CALLOUT LPAREN STRINGLIT (COMMA callout_arg)* RPAREN;
method_call_param_list: expr (COMMA expr)*;
callout_arg: expr | STRINGLIT;

// method_name: ((simple_expr | ID) DOT)? ID;
method_name: ID;

expr:
  simple_expr
  | NEW ID LPAREN RPAREN
  | NEW type (expr)?
  | literal
  | expr bin_op expr
  | NOT expr
  | LPAREN expr RPAREN;

simple_expr: 
  // location
  | THIS
  | method_call;

location:
  ID
  | simple_expr LBRACK expr RBRACK
  | simple_expr DOT ID;

bin_op:
  arith_op | rel_op | eq_op | cond_op;

arith_op: PLUS | MINUS | MULT | DIV;

rel_op: LT | GT | LEQ | GEQ;

eq_op: EQ | NEQ;

cond_op: AND | OR;

// Literals
literal:
  INTLIT
  | CHARLIT
  | bool_literal
  | NULL;

bool_literal: TRUE | FALSE;