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

        public override void ParseStatement()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.SEMICOLON)
            {
                ParseEmptyStatement();
                return;
            }
            if (CanBeIdentifier(tokenType))
            {
                if (LookAhead(1) == TokenType.EQ)
                {
                    ParseVariableStatement();
                    return;
                }
            }
            ParseExpressionStatement();
        }

        private void ParseExpressionStatement()
        {
            var mark = Mark();
            ParseFilterChainExpression();
            if (GetTokenType() == TokenType.SEMICOLON)
                ExpectToken(TokenType.SEMICOLON);
            Builder.DoneBeforeWhitespaces(mark, EXPRESSION_STATEMENT, null);
        }

        private void ParseEmptyStatement()
        {
            var mark = Mark();
            ExpectToken(TokenType.SEMICOLON);
            Builder.DoneBeforeWhitespaces(mark, EMPTY_STATEMENT, null);
        }

        protected override void ParseVariableStatement()
        {
            var mark = Mark();
            ParseVariableDeclarationList(false);
            if (GetTokenType() == TokenType.SEMICOLON)
                ExpectToken(TokenType.SEMICOLON);
            Builder.DoneBeforeWhitespaces(mark, VARIABLE_STATEMENT, null);
        }

        private void ParseFilterChainExpression()
        {
            // ParseBinaryExpression
            //   ParseExpression
            // ParsePipe
            // ParseFilterExpression

            ParseAssignmentExpression();
            // TODO: followed by | and filter. Wrapped in binary expression (and probably expression statement, too)
        }

        private new void ParseAssignmentExpression()
        {
            // TODO: Should this be a variable statement?
            var mark = Mark();
            if (!ParseTernaryExpression())
            {
                Builder.Drop(mark);
                return;
            }

            if (GetTokenType() != TokenType.EQ)
            {
                Builder.Drop(mark);
                return;
            }

            // TODO: angular checks that leftmost part of expression can be assigned
            Advance();
            ParseAssignmentExpression();
            Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
        }

        private bool ParseTernaryExpression()
        {
            if (!ParseLogicalOrExpression())
                return false;

            if (GetTokenType() == JavaScriptTokenType.QUESTION)
            {
                var mark = Builder.PrecedeCurrent();
                // This pattern might be wrong - we backtrack too much?
                // The real JS parser doesn't check return values. Does this handle
                // parse failures better?
                if (ExpectToken(JavaScriptTokenType.QUESTION)
                    && ParseTernaryExpression()
                    && ExpectToken(JavaScriptTokenType.COLON)
                    && ParseTernaryExpression())
                {
                    Builder.DoneBeforeWhitespaces(mark, CONDITIONAL_TERNARY_EXPRESSION, null);
                    return true;
                }
                Builder.Drop(mark);
                return false;
            }

            return true;
        }

        private bool ParseLogicalOrExpression()
        {
            if (!ParseLogicalAndExpression())
                return false;

            if (GetTokenType() == JavaScriptTokenType.PIPE2)
            {
                var mark = Builder.PrecedeCurrent();
                if (ExpectToken(JavaScriptTokenType.PIPE2)
                    && ParseLogicalOrExpression())    // TODO: AngularJs parser uses a loop instead of recursion
                {
                    Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
                    return true;
                }
                Builder.Drop(mark);
                return false;
            }

            return true;
        }

        private bool ParseLogicalAndExpression()
        {
            if (!ParseEqualityExpression())
                return false;

            if (GetTokenType() == JavaScriptTokenType.AMPER2)
            {
                var mark = Builder.PrecedeCurrent();
                if (ExpectToken(JavaScriptTokenType.AMPER2)
                    && ParseLogicalAndExpression())    // TODO: AngularJs parser uses a loop instead of recursion
                {
                    Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
                    return true;
                }
                Builder.Drop(mark);
                return false;
            }

            return true;
        }

        private bool ParseEqualityExpression()
        {
            if (!ParseRelationalExpression())
                return false;

            var tokenType = GetTokenType();
            if (tokenType == JavaScriptTokenType.EQ2 || tokenType == JavaScriptTokenType.NOTEQ
                || tokenType == JavaScriptTokenType.EQ3 || tokenType == JavaScriptTokenType.NOTEQ2)
            {
                var mark = Builder.PrecedeCurrent();
                Advance();  // Past operator
                ParseEqualityExpression();
                Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
            }

            return true;
        }

        private bool ParseRelationalExpression()
        {
            if (!ParseAdditiveExpression())
                return false;

            var tokenType = GetTokenType();
            if (tokenType == JavaScriptTokenType.LT || tokenType == JavaScriptTokenType.GT
                || tokenType == JavaScriptTokenType.LTEQ || tokenType == JavaScriptTokenType.GTEQ)
            {
                var mark = Builder.PrecedeCurrent();
                Advance();  // Past operator
                ParseRelationalExpression();
                Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
            }

            return true;
        }

        private bool ParseAdditiveExpression()
        {
            if (!ParseMultiplicativeExpression())
                return false;

            var tokenType = GetTokenType();
            if (tokenType == JavaScriptTokenType.PLUS || tokenType == JavaScriptTokenType.MINUS)
            {
                var mark = Builder.PrecedeCurrent();
                Advance();  // Past operator
                ParseAdditiveExpression();
                Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
            }

            return true;
        }

        private bool ParseMultiplicativeExpression()
        {
            if (!ParsePrefixExpression())
                return false;

            var tokenType = GetTokenType();
            if (tokenType == JavaScriptTokenType.STAR || tokenType == JavaScriptTokenType.DIVIDE
                || tokenType == JavaScriptTokenType.PERCENT)
            {
                var mark = Builder.PrecedeCurrent();
                Advance(); // Past operator
                ParseMultiplicativeExpression();
                Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
            }

            return true;
        }

        private bool ParsePrefixExpression()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.PLUS || tokenType == TokenType.MINUS || tokenType == TokenType.EXCLAMATION)
            {
                var mark = Mark();
                Advance();
                ParsePrefixExpression();
                Builder.DoneBeforeWhitespaces(mark, PREFIX_EXPRESSION, null);
            }
            else
            {
                //ParsePrimaryExpression();
                ParseMemberExpression();
            }

            return true;
        }

        private void ParsePrimaryExpression()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.LPARENTH)
            {
                // TODO: I don't think this should be a statement
                ParseExpressionStatement();
            }
            else if (tokenType == TokenType.LBRACKET)
            {
                ParseArrayLiteral();
            }
            else if (tokenType == TokenType.LBRACE)
            {
                ParseObjectLiteral();
            }
            else if (JavaScriptTokenType.LITERALS[tokenType])
            {
                ParseLiteralExpression();
            }
            else if (CanBeIdentifier(tokenType))
            {
                ParseIdentifierExpression();
            }
            else if (tokenType == TokenType.SEMICOLON)
            {
                ParseEmptyStatement();
            }
            else
            {
                // Angular gets current token, expression is fn, which
                // for a literal will just return the literal
                // e.g. function() { return number; }

                // Is this right?
                ParseExpressionStatement();
            }
        }

        private void ParseArrayLiteral()
        {
            var mark = Mark();
            ExpectToken(TokenType.LBRACKET);

            // TODO: ExpressionFirst probably needs updating
            if (!Builder.Eof() && ExpressionFirst[GetTokenType()])
            {
                ParseExpressionStatement();
            }

            while (GetTokenType() == TokenType.COMMA)
            {
                Advance();  // Past the comma

                if (!Builder.Eof() && ExpressionFirst[GetTokenType()])
                {
                    ParseExpressionStatement();
                }
            }

            ExpectToken(TokenType.RBRACKET);
            Builder.DoneBeforeWhitespaces(mark, ARRAY_LITERAL, null);
        }

        private void ParseLiteralExpression()
        {
            // TODO: LITERALS might need altering
            if (JavaScriptTokenType.LITERALS[GetTokenType()])
            {
                var mark = Mark();
                Advance();
                Builder.DoneBeforeWhitespaces(mark, LITERAL_EXPRESSION, null);
            }
        }
    }
}