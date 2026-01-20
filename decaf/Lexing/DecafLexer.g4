lexer grammar DecafLexer;

// Disable cSharp CLS compliance warnings
@lexer::header {#pragma warning disable 3021}

// Helpers

fragment A          : ('A'|'a') ;
fragment S          : ('S'|'s') ;
fragment Y          : ('Y'|'y') ;

fragment LOWERCASE  : [a-z] ;
fragment UPPERCASE  : [A-Z] ;

// Rules

SAYS                : S A Y S ;
WORD                : (LOWERCASE | UPPERCASE)+ ;
TEXT                : '"' .*? '"' ;
WHITESPACE          : (' '|'\t')+ -> skip ;
NEWLINE             : ('\r'? '\n' | '\r')+ ;