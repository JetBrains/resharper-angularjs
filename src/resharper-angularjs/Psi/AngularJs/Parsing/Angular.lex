using System;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;
using JetBrains.Text;
using JetBrains.Util;

%%

%unicode

%init{
    currTokenType = null;
%init}

%namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Parsing
%class AngularJsLexerGenerated
%implements IIncrementalLexer
%function _locateToken
%virtual
%public
%type TokenNodeType

%eofval{
  currTokenType = null; return currTokenType;
%eofval}

LINE_TAB_CHAR = \u000B
NEXT_LINE_CHAR = \u0085
LINE_SEPARATOR_CHAR = \u2028
PARAGRAPH_SEPARATOR_CHAR = \u2029
NON_BREAKING_SPACE_CHAR = \u00A0
BACKSLASH_CHAR = \\
SINGLE_QUOTE_CHAR = \'
DOUBLE_QUOTE_CHAR = \"
CARRIAGE_RETURN_CHAR = \u000D
LINE_FEED_CHAR = \u000A

WHITE_SPACE = ([ \t\n\r{LINE_TAB_CHAR}{NON_BREAKING_SPACE_CHAR}]|{BACKSLASH_CHAR}\n)+

NEW_LINE_PAIR = ({CARRIAGE_RETURN_CHAR}?{LINE_FEED_CHAR}|{CARRIAGE_RETURN_CHAR}|({NEXT_LINE_CHAR})|({LINE_SEPARATOR_CHAR})|({PARAGRAPH_SEPARATOR_CHAR}))

DIGIT = [0-9]
HEX_DIGIT = ({DIGIT}|[A-Fa-f])
NUMBER = ({DIGIT}+)|({FP_LITERAL1})|({FP_LITERAL2})|({FP_LITERAL3})|({FP_LITERAL4})
FP_LITERAL1 = ({DIGIT})+"."({DIGIT})*({EXPONENT_PART})?
FP_LITERAL2 = "."({DIGIT})+({EXPONENT_PART})?
FP_LITERAL3 = ({DIGIT})+({EXPONENT_PART})
FP_LITERAL4 = ({DIGIT})+
EXPONENT_PART = [Ee]["+""-"]?({DIGIT})*

QUOTE = [{SINGLE_QUOTE_CHAR}{DOUBLE_QUOTE_CHAR}]

IDENT = [_$a-zA-Z][0-9a-zA-Z]*

COMMON_STRING_LITERAL_CHARACTER_INNER = {BACKSLASH_CHAR}{NEXT_LINE_CHAR}{LINE_SEPARATOR_CHAR}{PARAGRAPH_SEPARATOR_CHAR}{CARRIAGE_RETURN_CHAR}{LINE_FEED_CHAR}
DOUBLE_STRING_LITERAL_CHARACTER_INNER = [^{DOUBLE_QUOTE_CHAR}{COMMON_STRING_LITERAL_CHARACTER_INNER}]
SINGLE_STRING_LITERAL_CHARACTER_INNER = [^{SINGLE_QUOTE_CHAR}{COMMON_STRING_LITERAL_CHARACTER_INNER}]

HEXADECIMAL_ESCAPE_SEQUENCE = (\\x{HEX_DIGIT}({HEX_DIGIT}|{HEX_DIGIT}{HEX_DIGIT}|{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT})?)
UNICODE_ESCAPE_SEQUENCE = ((\\u{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT})|({BACKSLASH_CHAR}U{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}{HEX_DIGIT}))
SIMPLE_ESCAPE_SEQUENCE = (\\[^{NEXT_LINE_CHAR}{LINE_SEPARATOR_CHAR}{PARAGRAPH_SEPARATOR_CHAR}{CARRIAGE_RETURN_CHAR}{LINE_FEED_CHAR}])

LINE_CONTINUATOR = (\\{NEW_LINE_PAIR})

COMMON_STRING_LITERAL_CHARACTER = {HEXADECIMAL_ESCAPE_SEQUENCE}|{UNICODE_ESCAPE_SEQUENCE}|{SIMPLE_ESCAPE_SEQUENCE}|{LINE_CONTINUATOR}
DOUBLE_STRING_LITERAL_CHARACTER = ({DOUBLE_STRING_LITERAL_CHARACTER_INNER}|{COMMON_STRING_LITERAL_CHARACTER})
SINGLE_STRING_LITERAL_CHARACTER = ({SINGLE_STRING_LITERAL_CHARACTER_INNER}|{COMMON_STRING_LITERAL_CHARACTER})

DOUBLE_STRING_LITERAL = (\"{DOUBLE_STRING_LITERAL_CHARACTER}*\")
SINGLE_STRING_LITERAL = (\'{SINGLE_STRING_LITERAL_CHARACTER}*\')

STRING_LITERAL = ({DOUBLE_STRING_LITERAL}|{SINGLE_STRING_LITERAL})

BAD_ESCAPE_SEQUENCE = (\\)

DOUBLE_STRING_LITERAL_ERROR = (\"{DOUBLE_STRING_LITERAL_CHARACTER}*|{BAD_ESCAPE_SEQUENCE})
SINGLE_STRING_LITERAL_ERROR = (\'{SINGLE_STRING_LITERAL_CHARACTER}*|{BAD_ESCAPE_SEQUENCE})

STRING_LITERAL_ERROR = ({DOUBLE_STRING_LITERAL_ERROR}|{SINGLE_STRING_LITERAL_ERROR})

%%

<YYINITIAL>	{STRING_LITERAL}	{ currTokenType = makeToken(JavaScriptTokenType.STRING_LITERAL); return currTokenType; }
<YYINITIAL> {STRING_LITERAL_ERROR}	{ currTokenType = makeToken(JavaScriptTokenType.STRING_LITERAL); return currTokenType; }

<YYINITIAL>	{NUMBER}			{ currTokenType = makeToken(JavaScriptTokenType.NUMERIC_LITERAL); return currTokenType; }
<YYINITIAL>	{WHITE_SPACE}		{ currTokenType = makeToken(JavaScriptTokenType.WHITE_SPACE); return currTokenType; }

<YYINITIAL>	"true"				{ currTokenType = makeToken(JavaScriptTokenType.TRUE_KEYWORD); return currTokenType; }
<YYINITIAL>	"false"				{ currTokenType = makeToken(JavaScriptTokenType.FALSE_KEYWORD); return currTokenType; }
<YYINITIAL>	"null"				{ currTokenType = makeToken(JavaScriptTokenType.NULL_KEYWORD); return currTokenType; }
<YYINITIAL>	"undefined"			{ currTokenType = makeToken(AngularJsTokenType.UNDEFINED_KEYWORD); return currTokenType; }
<YYINITIAL>	"in"				{ currTokenType = makeToken(JavaScriptTokenType.IN_KEYWORD); return currTokenType; }
<YYINITIAL>	"track by"			{ currTokenType = makeToken(AngularJsTokenType.TRACK_BY_KEYWORD); return currTokenType; }

<YYINITIAL>	{IDENT}				{ currTokenType = makeToken(JavaScriptTokenType.IDENTIFIER); return currTokenType; }

<YYINITIAL> "+"					{ currTokenType = makeToken(JavaScriptTokenType.PLUS); return currTokenType; }
<YYINITIAL> "-"					{ currTokenType = makeToken(JavaScriptTokenType.MINUS); return currTokenType; }
<YYINITIAL> "*"					{ currTokenType = makeToken(JavaScriptTokenType.ASTERISK); return currTokenType; }
<YYINITIAL> "/"					{ currTokenType = makeToken(JavaScriptTokenType.DIVIDE); return currTokenType; }
<YYINITIAL> "%"					{ currTokenType = makeToken(JavaScriptTokenType.PERCENT); return currTokenType; }
<YYINITIAL> "^"					{ currTokenType = makeToken(JavaScriptTokenType.CAROT); return currTokenType; }
<YYINITIAL> "="					{ currTokenType = makeToken(JavaScriptTokenType.EQ); return currTokenType; }
<YYINITIAL> "==="				{ currTokenType = makeToken(JavaScriptTokenType.EQ3); return currTokenType; }
<YYINITIAL> "!=="				{ currTokenType = makeToken(JavaScriptTokenType.NOTEQ2); return currTokenType; }
<YYINITIAL> "=="				{ currTokenType = makeToken(JavaScriptTokenType.EQ2); return currTokenType; }
<YYINITIAL> "!="				{ currTokenType = makeToken(JavaScriptTokenType.NOTEQ); return currTokenType; }
<YYINITIAL> "<"					{ currTokenType = makeToken(JavaScriptTokenType.LT); return currTokenType; }
<YYINITIAL> ">"					{ currTokenType = makeToken(JavaScriptTokenType.GT); return currTokenType; }
<YYINITIAL> "<="				{ currTokenType = makeToken(JavaScriptTokenType.LTEQ); return currTokenType; }
<YYINITIAL> ">="				{ currTokenType = makeToken(JavaScriptTokenType.GTEQ); return currTokenType; }
<YYINITIAL> "&&"				{ currTokenType = makeToken(JavaScriptTokenType.AMPER2); return currTokenType; }
<YYINITIAL> "||"				{ currTokenType = makeToken(JavaScriptTokenType.PIPE2); return currTokenType; }
<YYINITIAL> "&"					{ currTokenType = makeToken(JavaScriptTokenType.AMPER); return currTokenType; }
<YYINITIAL> "|"					{ currTokenType = makeToken(JavaScriptTokenType.PIPE); return currTokenType; }
<YYINITIAL> "!"					{ currTokenType = makeToken(JavaScriptTokenType.EXCLAMATION); return currTokenType; }
<YYINITIAL> "("					{ currTokenType = makeToken(JavaScriptTokenType.LPARENTH); return currTokenType; }
<YYINITIAL> ")"					{ currTokenType = makeToken(JavaScriptTokenType.RPARENTH); return currTokenType; }
<YYINITIAL> "{"					{ currTokenType = makeToken(JavaScriptTokenType.LBRACE); return currTokenType; }
<YYINITIAL> "}"					{ currTokenType = makeToken(JavaScriptTokenType.RBRACE); return currTokenType; }
<YYINITIAL> "["					{ currTokenType = makeToken(JavaScriptTokenType.LBRACKET); return currTokenType; }
<YYINITIAL> "]"					{ currTokenType = makeToken(JavaScriptTokenType.RBRACKET); return currTokenType; }
<YYINITIAL> "."					{ currTokenType = makeToken(JavaScriptTokenType.DOT); return currTokenType; }
<YYINITIAL> ","					{ currTokenType = makeToken(JavaScriptTokenType.COMMA); return currTokenType; }
<YYINITIAL> ";"					{ currTokenType = makeToken(JavaScriptTokenType.SEMICOLON); return currTokenType; }
<YYINITIAL> ":"					{ currTokenType = makeToken(JavaScriptTokenType.COLON); return currTokenType; }
<YYINITIAL> "?"					{ currTokenType = makeToken(JavaScriptTokenType.QUESTION); return currTokenType; }

<YYINITIAL> [^]					{ currTokenType = makeToken(JavaScriptTokenType.BAD); return currTokenType; }



