// TODO: Clean up the order in this file
// TODO: Document this file fully
lexer grammar DecafLexer;

// Disable cSharp CLS compliance warnings
@lexer::header {#pragma warning disable 3021}

// Keywords - Type
INT     : 'i' 'n' 't' ;
BOOLEAN : 'b' 'o' 'o' 'l' 'e' 'a' 'n' ;
CHAR    : 'c' 'h' 'a' 'r' ;
STRING  : 's' 't' 'r' 'i' 'n' 'g' ;
VOID    : 'v' 'o' 'i' 'd' ;

// Keywords - Instructions
BREAK   : 'b' 'r' 'e' 'a' 'k' ;
CONTINUE: 'c' 'o' 'n' 't' 'i' 'n' 'u' 'e';
RETURN  : 'r' 'e' 't' 'u' 'r' 'n';

// Keywords - Control Flow
IF      : 'i' 'f' ;
ELSE    : 'e' 'l' 's' 'e' ;
WHILE   : 'w' 'h' 'i' 'l' 'e';

// Keywords - Values
TRUE     : 't' 'r' 'u' 'e' ;
FALSE    : 'f' 'a' 'l' 's' 'e' ;

// Keywords - other
MODULE   : 'm' 'o' 'd' 'u' 'l' 'e' ;
LET      : 'l' 'e' 't' ;
NEW      : 'n' 'e' 'w' ;

// Attributes
WSS      : (' ' | '\t' | NEWLINE)+ -> skip;
COMMENTS : '/' '/' ~[\r\n]* -> skip; // Collects anything after '//' until the end of the line
NEWLINE  : ('\r'? '\n')+ -> skip;

// Operators and Punctuation
LPAREN      : '(' ;
RPAREN      : ')' ;
LBRACE      : '{' ;
RBRACE      : '}' ;
LBRACK      : '[' ;
RBRACK      : ']' ;
SEMI        : ';' ;
COMMA       : ',' ;
DOT         : '.' ;
COLON       : ':' ;
ARROW       : '=>' ;
// Prefix Operators
NOT         : '!' ;
BNOT        : '~' ;
// Arithmetic Operators
PLUS        : '+' ;
MINUS       : '-' ;
MULT        : '*' ;
DIV         : '/' ;
// Relational Operators
LEQ          : '<=' ;
GEQ          : '>=' ;
GT          : '>' ;
LT          : '<' ;
// Equality Operators
EQ          : '==' ;
NEQ         : '!=' ;
// Conditional Operators
AND         : '&&' ;
OR          : '||' ;
// Bitwise Operators
BAND        : '&' ;
BOR         : '|' ;
BLSHIFT     : '<<' ;
BRSHIFT     : '>>' ;
// Assignment Operator
ASSIGN      : '=' ;


// Literals
STRINGLIT   : '"' RAWCHAR* '"';
CHARLIT     : '\'' RAWCHAR '\'';
INTLIT      : DECLIT | HEXLIT;

PRIMID      : '@' ID; // Primitives are marked with a leading `@`
ID          : ALPHA (ALPHA | DIGIT)*;

// Helpers

fragment ALPHA : [a-zA-Z_]; // "a".."z" | "A".."Z" | "_"
fragment DIGIT : [0-9];
fragment HEXDIGIT : DIGIT | [a-f];
fragment HEXLIT : '0x' HEXDIGIT HEXDIGIT*;
fragment  DECLIT : '-'? DIGIT DIGIT*;
// Character: (\u0x20 to \u0x7E except for ' and ") | doublequote | singlequote | backslash | tab | newline
fragment RAWCHAR  : [\u0020-\u0021]|[\u0023-\u0026]|[\u0028-\u007E]|'\\"'|'\\\''|'\\\\'|'\\t'|'\\n';
