using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.Parsing.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Parsing
{
    public class AngularJsTreeBuilder : JavaScriptTreeBuilder
    {
// ReSharper disable InconsistentNaming
        private CompositeNodeType REPEAT_EXPRESSION;
// ReSharper restore InconsistentNaming

        public AngularJsTreeBuilder(ILexer lexer, Lifetime lifetime)
            : base(lexer, lifetime)
        {
        }

        protected override void InitElementTypes()
        {
            REPEAT_EXPRESSION = AngularJsElementType.REPEAT_EXPRESSION;
            base.InitElementTypes();
        }

        // sets expression parser to be a custom class
        // sets statement parser to be a custom class
        // parseAngular(IElementType root)
        //  builder.mark(), while (!builder.eof) parseStatement
        //  rootMarker.done(root)

        public override void ParseStatement()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.LBRACE)
            {
                ParseAssignmentExpression();    // ??? parseExpressionStatement
                ExpectToken(TokenType.SEMICOLON);
                // ParseSemicolon(StatementFirst);
                return;
            }

            if (CanBeIdentifier(tokenType))
            {
                var nextToken = LookAhead(1);
                if (nextToken == TokenType.EQ)
                {
                    ParseVariableDeclaration(false);
                    ParseSemicolon(StatementFirst);
                    return;
                }
                if (nextToken == TokenType.IN_KEYWORD)
                {
                    ParseInStatement();
                    return;
                }
            }

            if (GetTokenType() == TokenType.LPARENTH)
            {
                if (ParseInStatement())
                    return;
            }

            base.ParseStatement();
        }

        private bool ParseInStatement()
        {
            var mark = Mark();

            if (!ParseInExpression())
            {
                Builder.Drop(mark);
                return false;
            }

            Builder.DoneBeforeWhitespaces(mark, EXPRESSION_STATEMENT, null);
            return true;
        }

        private bool ParseInExpression()
        {
            var expressionMarker = Mark();

            if (CanBeIdentifier(GetTokenType()))
            {
                var definitionMarker = Mark();
                Advance();
                Builder.Done(definitionMarker, REFERENCE_EXPRESSION, null); // ? DEFINITION_EXPRESSION
            }
            else
            {
                var keyValueMarker = Mark();
                ParseKeyValue();
                if (GetTokenType() != TokenType.IN_KEYWORD)
                {
                    Builder.RollbackTo(expressionMarker);
                    return false;
                }

                Builder.DoneBeforeWhitespaces(keyValueMarker, PARENTHESIZED_EXPRESSION, null);
            }

            Advance();
            ExpectToken(TokenType.IN_KEYWORD);

            ParseJavaScriptExpression(); // ?? parseExpression

            if (GetTokenType() == AngularJsTokenType.TRACK_BY_KEYWORD)
            {
                Advance();
                ParseJavaScriptExpression(); // ?? parseExpression
            }

            Builder.DoneBeforeWhitespaces(expressionMarker, REPEAT_EXPRESSION, null);
            return true;
        }

        private void ParseKeyValue()
        {
            Advance();

            var commaMarker = Mark();
            if (CanBeIdentifier(GetTokenType()))
            {
                var definitionMarker = Mark();
                //buildTokenElement(REFERENCE_EXPRESSION);
                Builder.DoneBeforeWhitespaces(definitionMarker, IDENTIFIER_EXPRESSION, null);   // ? DEFINITION_EXPRESSION
            }
            else
            {
                Builder.Error("Expected identifier (angularjs)");
            }

            if (GetTokenType() == TokenType.COMMA)
            {
                Advance();
            }
            else
            {
                Builder.Error("Expected comma (angularjs)");
            }

            if (CanBeIdentifier(GetTokenType()))
            {
                var definitionMarker = Mark();
                //buildTokenElement(REFERENCE_EXPRESSION);
                Builder.DoneBeforeWhitespaces(definitionMarker, IDENTIFIER_EXPRESSION, null);   // ? DEFINITION_EXPRESSION
            }
            else
            {
                Builder.Error("Expected identifier (angularjs)");
            }

            Builder.DoneBeforeWhitespaces(commaMarker, COMPOUND_EXPRESSION, null);

            if (GetTokenType() == TokenType.RPARENTH)
            {
                Advance();
            }
            else
            {
                Builder.Error("Expected right parenthesis (angularjs)");
            }
        }
    }
}