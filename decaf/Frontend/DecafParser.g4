parser grammar DecafParser;

// Disable cSharp CLS compliance warnings
@parser::header {#pragma warning disable 3021}
options { tokenVocab=DecafLexer; }

program: module_decl+;

module_decl: MODULE name=ID LBRACE var_decl* method_decl* RBRACE;

var_decl: typ=type binds=var_bind_list SEMI;
var_bind_list: var_bind (COMMA var_bind)*;
var_bind: name=ID (LBRACK INTLIT? RBRACK)?;

method_decl: returnType=type name=ID LPAREN parameters=method_decl_param_list? RPAREN body=block;
method_decl_param_list: method_decl_param (COMMA method_decl_param)*;
method_decl_param: typ=type name=ID (LBRACK RBRACK)?;

block: LBRACE var_decl* statement* RBRACE;

type: 
  INT # IntType
  | BOOLEAN # BooleanType
  | VOID # VoidType
  | STRING # StringType
  ;

statement:
  assign_stmt # AssignStatement
  | expression_stmt # ExpressionStatement
  | if_stmt # IfStatement
  | while_stmt # WhileStatement
  | return_stmt # ReturnStatement
  | continue_stmt # ContinueStatement
  | break_stmt # BreakStatement
  | return_stmt # ReturnStatement
  | block # BlockStatement
  ;

assign_stmt: location ASSIGN expr SEMI;
expression_stmt: call_expr SEMI # CallExpressionStatement;
if_stmt: IF LPAREN condition=expr RPAREN trueBranch=block (ELSE falseBranch=block)?;
while_stmt: WHILE LPAREN condition=expr RPAREN body=block;
continue_stmt: CONTINUE SEMI;
break_stmt: BREAK SEMI;
return_stmt: RETURN value=expr? SEMI;

call_expr: 
  method_call # MethodCallExpr
  | prim_callout # PrimCalloutExpr
  ;
method_call: methodPath=location LPAREN args=method_call_args? RPAREN;
method_call_args: expr (COMMA expr)*;
prim_callout: CALLOUT LPAREN primId=STRINGLIT args=prim_callout_args RPAREN;
prim_callout_args: (COMMA (expr))*;

expr:
  simple_expr # SimpleExpr
  | NEW ID LPAREN RPAREN # NewObjectExpr
  | NEW type LBRACK expr RBRACK # NewArrayExpr
  | literal # LiteralExpr
  | op=prefix_op operand=expr # PrefixOpExpr
  | lhs=expr op=bin_op rhs=expr # BinaryOpExpr
  | LPAREN expr RPAREN # ParenExpr
  ;


simple_expr:
  location # LocationExpr
  | call_expr # CallExpr
  ;

// TODO: make root an expr
location: root=location_root (path=location_path | indexExpr=location_array_index)?;
location_root: ID;
location_path: DOT ID;
location_array_index: LBRACK expr RBRACK;

bin_op:
  arith_op | rel_op | eq_op | cond_op | bitwise_op;

arith_op: PLUS | MINUS | MULT | DIV;

rel_op: LT | GT | LEQ | GEQ;

eq_op: EQ | NEQ;

cond_op: AND | OR;

bitwise_op: BAND | BOR | BLSHIFT | BRSHIFT;

prefix_op: NOT | BNOT;

// Literals
literal:
  INTLIT # IntLit
  | CHARLIT # CharLit
  | (TRUE | FALSE) # BoolLit
  | STRINGLIT # StringLit
  ;