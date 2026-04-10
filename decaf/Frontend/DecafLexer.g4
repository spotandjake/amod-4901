lexer grammar DecafLexer;

// Disable cSharp CLS compliance warnings
@lexer::header {#pragma warning disable 3021}

// Keywords - Type
INT     : 'i' 'n' 't' ;
BOOLEAN : 'b' 'o' 'o' 'l' 'e' 'a' 'n' ;
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

// Keywords - other
MODULE   : 'm' 'o' 'd' 'u' 'l' 'e' ;
THIS    : 't' 'h' 'i' 's' ;
TRUE    : 't' 'r' 'u' 'e' ;
FALSE   : 'f' 'a' 'l' 's' 'e' ;
NEW     : 'n' 'e' 'w' ;
CALLOUT : 'c' 'a' 'l' 'l' 'o' 'u' 't' ;



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
// Prefix Operators
NOT         : '!' ;
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

BNOT        : '~' ;
// Assignment Operator
ASSIGN      : '=' ;


// Literals
STRINGLIT   : '"' CHAR* '"';
CHARLIT     : '\'' CHAR '\'';
INTLIT      : DECLIT | HEXLIT;

ID          : ALPHA (ALPHA | DIGIT)*;

// Helpers

fragment ALPHA : [a-zA-Z_]; // "a".."z" | "A".."Z" | "_"
fragment DIGIT : [0-9];
fragment HEXDIGIT : DIGIT | [a-f];
fragment HEXLIT : '0x' HEXDIGIT HEXDIGIT*;
fragment  DECLIT : '-'? DIGIT DIGIT*;
// Character: (\u0x20 to \u0x7E except for ' and ") | doublequote | singlequote | backslash | tab | newline
fragment CHAR  : [\u0020-\u0021]|[\u0023-\u0026]|[\u0028-\u007E]|'\\"'|'\\\''|'\\\\'|'\\t'|'\\n';
