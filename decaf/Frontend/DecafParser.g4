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
var_bind: name=ID (LBRACK optional_int_size RBRACK)?;

method_decl: returnType=type name=ID LPAREN parameters=method_decl_param_list? RPAREN body=block;
method_decl_param_list: method_decl_param_typ (COMMA method_decl_param_typ)*;
method_decl_param_typ: type ID (LBRACK RBRACK)?; // TODO: Consider simplifying the array part into type itself

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
  | call_stmt # CallStatement
  | if_stmt # IfStatement
  | while_stmt # WhileStatement
  | return_stmt # ReturnStatement
  | block # BlockStatement
  ;

assign_stmt: location ASSIGN expr SEMI;
call_stmt: call_expr SEMI;
if_stmt: IF LPAREN condition=expr RPAREN trueBranch=block (ELSE falseBranch=block)?;
while_stmt: WHILE LPAREN condition=expr RPAREN body=block;
return_stmt: RETURN value=expr? SEMI;

// TODO: Disallow ID[expr] in semantic analysis
call_expr: method_call | callout;
method_call: location method_call_args?;
method_call_args: LPAREN expr (COMMA expr)* RPAREN;
callout: CALLOUT LPAREN callout_args RPAREN;
callout_args: STRINGLIT (COMMA (expr | STRINGLIT))*;

// TODO: I don't love how this is structured at the moment
expr:
  simple_expr # SimpleExpr
  | NEW ID LPAREN RPAREN # NewObjectExpr // TODO: Develop a better name
  // TODO: What is the purpose of this????
  | NEW type (expr)? # NewArrayExpr // TODO: Develop a better name
  | literal # LiteralExpr
  | lhs=expr op=bin_op rhs=expr # BinaryOpExpr // TODO: Generalize this into a binop expression
  | op=NOT operand=expr # NotExpr // TODO: Generalize this to a prefix expr
  | LPAREN expr RPAREN # ParenExpr
  ;

simple_expr: location | THIS | call_expr;

location: ID ((DOT ID) | (LBRACK expr RBRACK))?;

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