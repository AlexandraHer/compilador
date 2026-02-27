parser grammar MyLangParser;

options { tokenVocab=MyLangLexer; }

start
  : program EOF
  ;

program
  : topLevelDecl*
  ;

topLevelDecl
  : useDecl
  | classDecl
  | functionDecl
  ;

useDecl
  : USE ID SEMI
  ;

classDecl
  : OBJECT ID LBRACE classMember* RBRACE
  ;

classMember
  : varDecl
  | methodDecl
  | statement
  ;

methodDecl
  : ENTRY? FUNC ID LPAREN paramList? RPAREN COLON typeRef block
  ;

functionDecl
  : ENTRY? FUNC ID LPAREN paramList? RPAREN COLON typeRef block
  ;

paramList
  : param (COMMA param)*
  ;

param
  : ID COLON typeRef
  ;

typeRef
  : baseType QMARK? arraySize?
  ;

baseType
  : INT_TYPE
  | FLOAT_TYPE
  | BOOL_TYPE
  | STRING_TYPE
  | ID
  ;

arraySize
  : LBRACK INT RBRACK
  ;

block
  : LBRACE statement* RBRACE
  ;

statement
  : varDecl
  | assignStmt
  | ifStmt
  | loopStmt
  | repeatStmt
  | returnStmt
  | exprStmt
  ;

varDecl
  : DECLARE ID COLON typeRef (ASSIGN initializer)? SEMI
  ;

initializer
  : expression
  | arrayLiteral
  ;

/*
  Permite:
    x = expr;
    set x = expr;
*/
assignStmt
  : (SET)? lvalue ASSIGN expression SEMI
  ;

lvalue
  : ID (LBRACK expression RBRACK)?
  ;

ifStmt
  : CHECK LPAREN condition RPAREN block (OTHERWISE block)?
  ;

repeatStmt
  : REPEAT LPAREN condition RPAREN block
  ;

loopStmt
  : LOOP LPAREN loopInit? SEMI condition SEMI loopAction? RPAREN block
  ;

loopInit
  : varDeclNoSemi
  | assignNoSemi
  ;

loopAction
  : assignNoSemi
  | expression
  ;

varDeclNoSemi
  : DECLARE ID COLON typeRef (ASSIGN initializer)?
  ;

assignNoSemi
  : (SET)? lvalue ASSIGN expression
  ;

returnStmt
  : GIVES expression SEMI
  ;

exprStmt
  : expression SEMI
  ;

/* ===================== EXPRESSIONS ===================== */

condition
  : orExpr
  ;

expression
  : orExpr
  ;

orExpr
  : andExpr (OR andExpr)*
  ;

andExpr
  : notExpr (AND notExpr)*
  ;

notExpr
  : NOT? relExpr
  ;

relExpr
  : addExpr (relOp addExpr)?
  ;

relOp
  : EQ
  | NEQ
  | GT
  | LT
  | GTE
  | LTE
  ;

addExpr
  : mulExpr ((PLUS | MINUS) mulExpr)*
  ;

mulExpr
  : unaryExpr ((STAR | SLASH | PERCENT) unaryExpr)*
  ;

unaryExpr
  : MINUS? postfixExpr
  ;

/*
  postfixExpr soporta:
    - llamadas normales: foo(x)
    - llamadas a métodos: obj.suma(x, y)
    - encadenado: a.b().c(d)
*/
postfixExpr
  : primaryPostfix (postfixSuffix)*
  ;

postfixSuffix
  : LPAREN argList? RPAREN              // llamada: foo(...)
  | DOT ID LPAREN argList? RPAREN       // método: obj.suma(...)
  ;

primaryPostfix
  : ID                                  // variable base (obj)
  | literal
  | lenExpr
  | askExpr
  | convertExpr
  | showExpr
  | readFileExpr
  | writeFileExpr
  | arrayAccess
  | arrayLiteral
  | LPAREN expression RPAREN
  ;

arrayAccess
  : ID LBRACK expression RBRACK
  ;

argList
  : expression (COMMA expression)*
  ;

lenExpr
  : LEN LPAREN ID RPAREN
  ;

askExpr
  : ASK LPAREN ID RPAREN
  ;

convertExpr
  : (CONV_INT | CONV_FLOAT | CONV_BOOL) LPAREN expression RPAREN
  ;

showExpr
  : SHOW LPAREN expression RPAREN
  ;

readFileExpr
  : READFILE LPAREN expression RPAREN
  ;

writeFileExpr
  : WRITEFILE LPAREN expression COMMA expression RPAREN
  ;

arrayLiteral
  : LBRACK (expression (COMMA expression)*)? RBRACK
  ;

literal
  : INT
  | FLOAT
  | STRING
  | TRUE
  | FALSE
  | NULL
  ;