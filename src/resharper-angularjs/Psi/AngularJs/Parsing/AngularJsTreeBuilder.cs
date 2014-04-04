#region license
// Copyright 2014 JetBrains s.r.o.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing.Tree;
using JetBrains.ReSharper.Psi.AngularJs.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing
{
    public class AngularJsTreeBuilder : JavaScriptTreeBuilder
    {
// ReSharper disable InconsistentNaming
        private CompositeNodeType FILTER_EXPRESSION;
        private CompositeNodeType FILTER_ARGUMENT_LIST;
        private CompositeNodeType REPEAT_EXPRESSION;
// ReSharper restore InconsistentNaming

        public AngularJsTreeBuilder(ILexer lexer, Lifetime lifetime)
            : base(lexer, lifetime)
        {
        }

        protected override void InitElementTypes()
        {
            FILTER_EXPRESSION = AngularJsElementType.FILTER_EXPRESSION;
            FILTER_ARGUMENT_LIST = AngularJsElementType.FILTER_ARGUMENT_LIST;
            REPEAT_EXPRESSION = AngularJsElementType.REPEAT_EXPRESSION;
            base.InitElementTypes();
        }

        public override void ParseStatement()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.SEMICOLON)
            {
                ParseEmptyStatement();
            }
            else if (CanBeIdentifier(tokenType))
            {
                var nextTokenType = LookAhead(1);
                if (nextTokenType == TokenType.EQ)
                    ParseVariableStatement();
                else if (nextTokenType == TokenType.IN_KEYWORD)
                    ParseInStatement();
                else
                    ParseExpressionStatement();
            }
            else if (tokenType == TokenType.LPARENTH)
            {
                ParseInStatement();
            }
                // TODO: Check ExpressionFirst
            else if (!Builder.Eof() && ExpressionFirst[tokenType])
            {
                ParseExpressionStatement();
            }
            else
            {
                var mark = Mark();
                if (!Builder.Eof())
                    Advance();
                Builder.Error(mark, "Unexpected token");
            }
        }

        private void ParseEmptyStatement()
        {
            var mark = Mark();
            ExpectToken(TokenType.SEMICOLON);
            Builder.DoneBeforeWhitespaces(mark, EMPTY_STATEMENT, null);
        }

        private new void ParseVariableStatement()
        {
            var mark = Mark();
            ParseVariableDeclaration();
            ParseOptionalSemiColon();
            Builder.DoneBeforeWhitespaces(mark, VARIABLE_STATEMENT, null);
        }

        private void ParseVariableDeclaration()
        {
            var mark = Mark();
            base.ParseIdentifierExpression();
            if (GetTokenType() == TokenType.EQ)
            {
                Advance();
                ParseExpression();
            }
            Builder.DoneBeforeWhitespaces(mark, VARIABLE_DECLARATION, null);
        }

        private void ParseOptionalSemiColon()
        {
            if (GetTokenType() == TokenType.SEMICOLON)
                ExpectToken(TokenType.SEMICOLON);
        }

        private void ParseInStatement()
        {
            var mark = Mark();
            if (!ParseInExpression())
            {
                Builder.Drop(mark);
                return;
            }

            Builder.DoneBeforeWhitespaces(mark, EXPRESSION_STATEMENT, null);
        }

        private bool ParseInExpression()
        {
            var mark = Mark();

            if (CanBeIdentifier(GetTokenType()))
            {
                var m = Mark();
                base.ParseIdentifierExpression();
                Builder.DoneBeforeWhitespaces(m, REFERENCE_EXPRESSION, null);
            }
            else if (GetTokenType() == TokenType.LPARENTH)
            {
                var m = Mark();
                ParseKeyValue();
                if (GetTokenType() != TokenType.IN_KEYWORD)
                {
                    Builder.RollbackTo(mark);
                    return false;
                }
                Builder.DoneBeforeWhitespaces(m, PARENTHESIZED_EXPRESSION, null);
            }
            else
            {
                Builder.ErrorBeforeWhitespaces("Unexpected token", CommentsOrWhiteSpacesTokens);
                return false;
            }

            ExpectToken(TokenType.IN_KEYWORD);
            ParseExpression();

            if (GetTokenType() == AngularJsTokenType.TRACK_BY_KEYWORD)
            {
                Advance();
                ParseExpression();
            }

            Builder.DoneBeforeWhitespaces(mark, REPEAT_EXPRESSION, null);
            return true;
        }

        private void ParseKeyValue()
        {
            ExpectToken(TokenType.LPARENTH);

            var mark = Mark();
            if (CanBeIdentifier(GetTokenType()))
            {
                var m = Mark();
                base.ParseIdentifierExpression();
                Builder.DoneBeforeWhitespaces(m, REFERENCE_EXPRESSION, null);
            }
            else
            {
                Builder.ErrorBeforeWhitespaces("Expected identifier", CommentsOrWhiteSpacesTokens);
            }

            ExpectToken(TokenType.COMMA);

            if (CanBeIdentifier(GetTokenType()))
            {
                var m = Mark();
                base.ParseIdentifierExpression();
                Builder.DoneBeforeWhitespaces(m, REFERENCE_EXPRESSION, null);
            }
            else
            {
                Builder.ErrorBeforeWhitespaces("Expected identifier", CommentsOrWhiteSpacesTokens);
            }

            Builder.DoneBeforeWhitespaces(mark, COMPOUND_EXPRESSION, null);

            ExpectToken(TokenType.RPARENTH);
        }

        private void ParseExpressionStatement()
        {
            var mark = Mark();
            ParseExpression();
            ParseOptionalSemiColon();
            Builder.DoneBeforeWhitespaces(mark, EXPRESSION_STATEMENT, null);
        }

        private void ParseExpression()
        {
            ParseFilterChainExpression();
        }

        private void ParseFilterChainExpression()
        {
            var mark = Mark();
            ParseAssignmentExpression();

            if (GetTokenType() == TokenType.PIPE)
            {
                while (GetTokenType() == TokenType.PIPE)
                {
                    ParseFilterExpression();
                    Builder.DoneBeforeWhitespaces(mark, BINARY_EXPRESSION, null);
                    Builder.Precede(mark);
                }
            }
            Builder.Drop(mark);
        }

        private void ParseFilterExpression()
        {
            ExpectToken(TokenType.PIPE);
            SkipWhitespaces();

            var mark = Builder.Mark();
            ParseFilterIdentifierExpression();
            ParseFilterArgumentList();
            Builder.DoneBeforeWhitespaces(mark, FILTER_EXPRESSION, null);
        }

        private void ParseFilterIdentifierExpression()
        {
            var mark = Builder.Mark();
            base.ParseIdentifierExpression();
            Builder.DoneBeforeWhitespaces(mark, REFERENCE_EXPRESSION, null);
        }

        private void ParseFilterArgumentList()
        {
            var mark = Builder.Mark();
            while (GetTokenType() == TokenType.COLON)
            {
                ExpectToken(TokenType.COLON);

                // TODO: ParseExpression? This prevents recursive filter chains,
                // but requires knowledge of what ParseExpressionStatement and 
                // ParseFilterChainExpression do
                ParseAssignmentExpression();
            }
            if (Builder.IsEmpty(mark))
            {
                Builder.Drop(mark);
                return;
            }
            Builder.DoneBeforeWhitespaces(mark, FILTER_ARGUMENT_LIST, null);
        }

        private new void ParseAssignmentExpression()
        {
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
            if (!ParsePrefixPostfixExpression())
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

        private new bool ParsePrefixPostfixExpression()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.PLUS || tokenType == TokenType.MINUS || tokenType == TokenType.EXCLAMATION)
            {
                return ParsePrefixExpression();
            }
            return ParsePostfixExpression();
        }

        private bool ParsePrefixExpression()
        {
            var tokenType = GetTokenType();
            if (tokenType == TokenType.PLUS || tokenType == TokenType.MINUS || tokenType == TokenType.EXCLAMATION)
            {
                var mark = Mark();
                Advance();
                ParsePrefixPostfixExpression();
                Builder.DoneBeforeWhitespaces(mark, PREFIX_EXPRESSION, null);
            }

            return true;
        }

        private bool ParsePostfixExpression()
        {
            return ParseMemberExpression();
        }

        private new bool ParseMemberExpression()
        {
            if (MemberExpressionFirst[GetTokenType()])
            {
                ParseMemberExpressionInner();
            }
            else
            {
                if (!base.ParseIdentifierExpression())
                    return false;

                var ident = Builder.PrecedeCurrent();
                Builder.DoneBeforeWhitespaces(ident, REFERENCE_EXPRESSION, null);
            }

            // TODO: Implement ParseArgumentListAux, since it calls ParseJavaScriptExpression
            return base.ParseMemberExpressionFollows(stopAtInvocation: false);
        }

        private void ParseMemberExpressionInner()
        {
            var tokenType = GetTokenType();
            if (JavaScriptTokenType.LITERALS[tokenType])
            {
                ParseLiteralExpression();
            }
            else if (tokenType == TokenType.LBRACKET)
            {
                ParseArrayLiteral();
            }
            else if (tokenType == TokenType.LBRACE)
            {
                ParseObjectLiteral();
            }
            else if (tokenType == TokenType.LPARENTH)
            {
                ParseParenthesizedExpression();
            }
            else
            {
                Builder.ErrorBeforeWhitespaces("Primary expression expected", CommentsOrWhiteSpacesTokens);
            }
        }

        private void ParseLiteralExpression()
        {
            if (JavaScriptTokenType.LITERALS[GetTokenType()])
            {
                int mark = Mark();
                Advance();
                Builder.DoneBeforeWhitespaces(mark, LITERAL_EXPRESSION, null);
            }
        }

        // Same as JavaScriptTreeBuilder's implementation, but calls out ParseExpression
        // rather than the private ParseJavaScriptExpression
        private void ParseArrayLiteral()
        {
            var mark = Mark();
            ExpectToken(TokenType.LBRACKET);

            var tokenType = GetTokenType();
            if (!Builder.Eof() && ExpressionFirst[tokenType])
                ParseExpression();

            while (GetTokenType() == TokenType.COMMA)
            {
                Advance();

                if (!Builder.Eof() && ExpressionFirst[GetTokenType()])
                {
                    ParseExpression();
                }
            }

            ExpectToken(TokenType.RBRACKET);
            Builder.DoneBeforeWhitespaces(mark, ARRAY_LITERAL, null);
        }

        private new void ParseParenthesizedExpression()
        {
            var mark = Mark();
            ExpectToken(TokenType.LPARENTH);
            ParseCompoundExpression();
            ExpectToken(TokenType.RPARENTH);
            Builder.DoneBeforeWhitespaces(mark, PARENTHESIZED_EXPRESSION, null);
        }

        private new void ParseCompoundExpression()
        {
            var mark = Mark();
            ParseExpression();
            while (GetTokenType() == TokenType.COMMA)
            {
                Advance();
                ParseExpression();
            }

            Builder.DoneBeforeWhitespaces(mark, COMPOUND_EXPRESSION, null);
        }
    }
}