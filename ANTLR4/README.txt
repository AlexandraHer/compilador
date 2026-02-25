This folder contains the ANTLR tool used to generate the parser files.

Version used:
ANTLR 4.13.1

To regenerate parser:
java -jar antlr-4.13.1-complete.jar -Dlanguage=CSharp -visitor -o ../Generated ../Definition/MyLang.g4
