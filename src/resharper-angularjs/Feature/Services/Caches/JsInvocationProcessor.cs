using System.Collections.Generic;
using JetBrains.ReSharper.Psi.JavaScript.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class JsInvocationProcessor
    {
        private readonly AngularJsCacheItemsBuilder cacheItemsBuilder;

        public JsInvocationProcessor(AngularJsCacheItemsBuilder cacheItemsBuilder)
        {
            this.cacheItemsBuilder = cacheItemsBuilder;
        }

        public void ProcessInvocationExpression(IInvocationExpression invocationExpression)
        {
            if (invocationExpression.Arguments.Count <= 1)
                return;

            var stringLiteralExpression = invocationExpression.Arguments[0];
            var identifier = GetStringLiteralValue(stringLiteralExpression);
            if (identifier == null)
                return;

            var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
            if (referenceExpression == null)
                return;

            switch (referenceExpression.Name)
            {
                case "directive":
                    ProcessDirective(invocationExpression, identifier, stringLiteralExpression.GetTreeStartOffset().Offset);
                    break;
            }
        }

        private static string GetStringLiteralValue(IExpressionOrSpread expresion)
        {
            var literalExpression = expresion as IJavaScriptLiteralExpression;
            if (literalExpression != null && literalExpression.IsStringLiteral())
                return literalExpression.GetStringValue();

            return null;
        }

        private void ProcessDirective(IInvocationExpression invokedExpression, string identifier, int offset)
        {
            if (invokedExpression.Arguments.Count != 2)
                return;

            var restrictions = CalculateRestrictions(invokedExpression);

            // Directives declared in code cannot be applied to just a single tag
            var tags = new string[0];
            var directive = new Directive(identifier, DirectiveUtil.GetNormalisedName(identifier), restrictions, tags, offset, new List<Parameter>());
            cacheItemsBuilder.Add(directive);
        }

        private static string CalculateRestrictions(IInvocationExpression invokedExpression)
        {
            // TODO: Set default restrictions per version
            const string defaultRestrictions = "AE";

            // First argument is traditionally the identifier (i.e. the directive name) but can also
            // be an object literal, where the keys are the directive names and the values are the
            // factory functions. We don't support this.
            // Second argument can be:
            // * factory function, which is injectable
            // * array literal specifying injectables for the factory function
            // Return value of the factory function can be:
            // * the "postlink" function, which means we use defaults
            // * a directive definition object, wich is an object literal that contains properties such as restrict
            var argument = invokedExpression.Arguments[1];

            var function = argument as IFunctionExpression;

            if (function == null)
            {
                var arrayLiteral = argument as IArrayLiteral;
                if (arrayLiteral != null && arrayLiteral.ArrayElements.Count > 0)
                {
                    var lastElement = arrayLiteral.ArrayElements.Last();
                    function = lastElement as IFunctionExpression;
                }
            }

            if (function == null || function.Block.Statements.Count == 0)
                return defaultRestrictions;

            IObjectLiteral objectLiteral = null;

            // TODO: Perhaps this should get the JS type, and get the return value of the function
            // This might be better than trying to find an object literal used as a return value
            var lastStatement = function.Block.Statements.Last();
            var returnStatement = lastStatement as IReturnStatement;
            if (returnStatement != null)
            {
                objectLiteral = returnStatement.Value.LastExpression as IObjectLiteral;
            }

            if (objectLiteral == null)
                return defaultRestrictions;

            foreach (var initializer in objectLiteral.Properties.OfType<IObjectPropertyInitializer>())
            {
                if (initializer.DeclaredName == "restrict")
                    return GetStringLiteralValue(initializer.Value) ?? defaultRestrictions;
            }
            return defaultRestrictions;
        }
    }
}