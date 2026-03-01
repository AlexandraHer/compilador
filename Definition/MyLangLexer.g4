lexer grammar MyLangLexer;

/* ========== KEYWORDS ========== */

USE        : 'use';
OBJECT     : 'object';
FUNC       : 'func';
ENTRY      : 'entry';
DECLARE    : 'declare';
SET        : 'set';
CHECK      : 'check';
OTHERWISE  : 'otherwise';
REPEAT     : 'repeat';
LOOP       : 'loop';
GIVES      : 'gives';

OR         : 'or';
AND        : 'and';
NOT        : 'not';

LEN        : 'len';
ASK        : 'ask';
SHOW       : 'show';
READFILE   : 'readfile';
WRITEFILE  : 'writefile';

CONV_INT   : 'convertToInt';
CONV_FLOAT : 'convertToFloat';
CONV_BOOL  : 'convertToBoolean';

/* ========== BUILT-IN TYPES ========== */
/* Profe pide: i, f, b, s (dejamos también int/float/bool/string por compatibilidad) */

INT_TYPE    : 'i' | 'int';
FLOAT_TYPE  : 'f' | 'float';
BOOL_TYPE   : 'b' | 'bool';
STRING_TYPE : 's' | 'string';

/* ========== LITERALS ========== */

TRUE  : 'true';
FALSE : 'false';
NULL  : 'null';

FLOAT  : DIGIT+ '.' DIGIT+ ;
INT    : DIGIT+ ;

STRING : '"' ( ESC | ~["\\] )* '"' ;
fragment ESC : '\\' ( ['"\\nrt] ) ;

/* ========== OPERATORS ========== */

EQ   : '==';
NEQ  : '!=';
GTE  : '>=';
LTE  : '<=';
GT   : '>';
LT   : '<';

PLUS    : '+';
MINUS   : '-';
STAR    : '*';
SLASH   : '/';
PERCENT : '%';

ASSIGN  : '=';

QMARK   : '?';

/* ========== PUNCTUATION / DELIMITERS ========== */

DOT    : '.';

LPAREN : '(';
RPAREN : ')';
LBRACE : '{';
RBRACE : '}';
LBRACK : '[';
RBRACK : ']';

COLON  : ':';
COMMA  : ',';
SEMI   : ';';

/* ========== IDENTIFIERS ========== */

ID : LETTER (LETTER | DIGIT | '_')* ;

/* ========== FRAGMENTS ========== */

fragment LETTER : [a-zA-Z];
fragment DIGIT  : [0-9];

/* ========== WHITESPACE ========== */

WS : [ \t\r\n]+ -> skip ;