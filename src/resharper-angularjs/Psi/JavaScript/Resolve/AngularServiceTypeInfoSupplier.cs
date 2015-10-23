#region license
// Copyright 2015 JetBrains s.r.o.
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

using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Util;
using JetBrains.ReSharper.Psi.JavaScript.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.JavaScript.Resolve
{
    // TODO: Filters. Can inject a filter into a controller, etc. with <name>Filter
    // (Note we can parse built in filters with @ngdoc filter)
    // TODO: Only inject providers or constants into config?
    // Don't think this is possible as we're kinda loosely coupled as to what gets injected

    [PsiComponent]
    public class AngularServiceTypeInfoSupplier : IJsTypeInfoSupplier
    {
        public void ProcessFile(IFile file, IJavaScriptCacheBuilderContext context)
        {
            if (!ShouldProcess(file))
                return;

            var jsFile = (IJavaScriptFile) file;
            var processor = new Processor(context);
            jsFile.ProcessDescendants(processor);
        }

        private static bool ShouldProcess(IFile file)
        {
            // TODO: TypeScript?
            return file is IJavaScriptFile;
        }

        private class Processor : TreeNodeVisitor, IRecursiveElementProcessor
        {
            private static readonly JetHashSet<string> InjectableNames;

            private readonly IJavaScriptCacheBuilderContext context;

            static Processor()
            {
                // Also $injector.get and $injector.invoke
                InjectableNames = new JetHashSet<string> {"controller", "directive", "filter", "factory", "service", "provider", "animation" };
            }

            public Processor(IJavaScriptCacheBuilderContext context)
            {
                this.context = context;
            }

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                return true;
            }

            public void ProcessBeforeInterior(ITreeNode element)
            {
                var jsTreeNode = element as IJavaScriptTreeNode;
                if (jsTreeNode != null)
                    jsTreeNode.Accept(this);
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            public bool ProcessingIsFinished { get { return false; } }

            // Built in services
            // Sets up the type for an injected factory function (type of the last parameter
            // to the array literal assigned to $get)
            // var $AnimateProvider = [ '$provide', function($provider) { ...
            //   this.$get = [ '$$animateQueue', function($$animateQueue) { ...
            public override void VisitVariableStatement(IVariableStatement variableStatement)
            {
                foreach (var variableDeclaration in variableStatement.DeclarationsEnumerable)
                {
                    var name = variableDeclaration.NameNode.GetDeclaredName();
                    if (name == null)
                        continue;

                    if (name.EndsWith("Provider"))
                        ProcessExplicitProviderDeclaration(name, variableDeclaration);
                }

                base.VisitVariableStatement(variableStatement);
            }

            // Inject into $get factory functions
            // Improves type information for the 
            // this.$get = [ '$cacheFactory', function($cacheFactory) { ...
            public override void VisitSimpleAssignmentExpression(ISimpleAssignmentExpression simpleAssignmentExpression)
            {
                var target = simpleAssignmentExpression.Dest as IReferenceExpression;
                if (target == null)
                    return;

                if (target.Name == "$get" && target.Qualifier is IThisExpression)
                {
                    var factoryFunction = GetFactoryFunction(simpleAssignmentExpression.Source);
                    if (factoryFunction == null)
                        return;

                    ProcessServiceInjection(simpleAssignmentExpression.Source, factoryFunction);
                }

                base.VisitSimpleAssignmentExpression(simpleAssignmentExpression);
            }

            // Built in services
            // Sets up the type for an injected factory function (type of the last parameter
            // to the array literal assigned to $get)
            // function $BrowserProvider() {
            //   this.$get = [ '$window', '$log', ..., function($window, $log, ...) { ...
            public override void VisitFunctionExpression(IFunctionExpression functionExpression)
            {
                if (functionExpression.DeclaredName.EndsWith("Provider"))
                {
                    ProcessExplicitProviderDeclaration(functionExpression.DeclaredName, functionExpression);
                    return;
                }
                base.VisitFunctionExpression(functionExpression);
            }

            private void ProcessExplicitProviderDeclaration(string providerFunctionName, IVariableDeclaration variableDeclaration)
            {
                var factoryFunction = GetFactoryFunction(variableDeclaration.Value);
                if (factoryFunction == null)
                    return;

                ProcessExplicitProviderDeclaration(providerFunctionName, factoryFunction);

                var constructedFactoryType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                var providerGlobalType = GetHiddenGlobalPropertyType(providerFunctionName + "Fn");
                context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, providerGlobalType, constructedFactoryType,
                    context.GetDocumentStartOffset(factoryFunction));
            }

            private void ProcessExplicitProviderDeclaration(string providerFunctionName, IFunctionExpression functionExpression)
            {
                var block = functionExpression.Block;
                if (block == null)
                    return;

                IFunctionExpression getFactoryFunction = null;
                foreach (var statement in block.StatementsEnumerable.OfType<IExpressionStatement>())
                {
                    var assignment = statement.Expression.LastExpression as ISimpleAssignmentExpression;
                    if (assignment == null)
                        continue;

                    var target = assignment.Dest as IReferenceExpression;
                    if (target == null)
                        continue;

                    if (target.Name == "$get" && target.Qualifier is IThisExpression)
                    {
                        getFactoryFunction = GetFactoryFunction(assignment.Source);
                        if (getFactoryFunction != null)
                            break;
                    }
                }

                if (getFactoryFunction != null)
                {
                    var serviceType = getFactoryFunction.GetJsType(context).GetReturnType();
                    var providerGetGlobalType = GetHiddenGlobalPropertyType(providerFunctionName + "$get");
                    context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, providerGetGlobalType, serviceType, context.GetDocumentStartOffset(functionExpression));
                }
            }

            // Bulk registration of builtin services - $provide.provider({ $log: $LogProvider, ... });
            // Registration of custom services - module.factory, module.service, etc.
            // Usage of custom services
            public override void VisitInvocationExpression(IInvocationExpression invocationExpression)
            {
                ProcessBulkProviderRegistration(invocationExpression);
                ProcessServiceRegistration(invocationExpression);
                ProcessServiceInjection(invocationExpression);
            }

            private void ProcessBulkProviderRegistration(IInvocationExpression invocationExpression)
            {
                var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                if (referenceExpression == null || referenceExpression.Name != "provider")
                    return;

                var qualifierReferenceExpression = referenceExpression.Qualifier as IReferenceExpression;
                if (qualifierReferenceExpression == null || qualifierReferenceExpression.Name != "$provide")
                    return;

                if (invocationExpression.Arguments.Count != 1)
                    return;

                var bulkProvidersLiteral = invocationExpression.Arguments[0] as IObjectLiteral;
                if (bulkProvidersLiteral == null)
                    return;

                foreach (var property in bulkProvidersLiteral.PropertiesEnumerable.OfType<IObjectPropertyInitializer>())
                {
                    var providerExpression = property.Value as IReferenceExpression;
                    if (providerExpression == null)
                        continue;

                    var serviceName = property.DeclaredName;
                    var providerFunctionName = providerExpression.Name;

                    // TODO: How to get offset of provider function?
                    // Do we need to? Who can navigate to it when we're only using the type?
                    var providerOffset = -1;

                    // The type of the injected provider is the constructed type of the provider factory function,
                    // as long as the provider factory function is a function. It might also be an injectable array
                    // literal, but we don't know that here. Same trick as service type below. Composite type
                    var constructedFactoryType = providerExpression.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                    var providerType2 = GetHiddenGlobalPropertyType(providerFunctionName + "Fn");
                    var providerType = JavaScriptType.CreateCompositeType(JsCombinedTypeKind.JsDynamic, constructedFactoryType, providerType2);
                    var providerGlobalType = GetHiddenGlobalPropertyType(serviceName + "Provider");
                    context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, providerGlobalType, providerType, providerOffset);

                    // We don't have enough information here to tell if the service type is the return value of the
                    // $get function, or the return value of the last element in the array literal of the $get property.
                    // Create a type that is a composite of the return value of $get, and a hidden global property that
                    // will be the type of the $get property, which we'll create an association for when/if we encounter
                    // the provider function when walking the file. Note that the hiden global property is named after
                    // the provider function name, not the provider itself, e.g. $LogProvider, not $logProvider
                    var serviceType1 = constructedFactoryType.GetPropertyReferenceType("$get").GetReturnType();
                    var serviceType2 = GetHiddenGlobalPropertyType(providerFunctionName + "$get");
                    var serviceType = JavaScriptType.CreateCompositeType(JsCombinedTypeKind.JsDynamic, serviceType1, serviceType2);

                    var serviceGlobalType = GetHiddenGlobalPropertyType(serviceName);
                    var serviceOffset = context.GetDocumentStartOffset(property);

                    context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, serviceGlobalType, serviceType, serviceOffset);
                }
            }

            private void ProcessServiceRegistration(IInvocationExpression invocationExpression)
            {
                if (invocationExpression.Arguments.Count != 2)
                    return;

                var stringLiteralExpression = invocationExpression.Arguments[0];
                var serviceName = stringLiteralExpression.GetStringLiteralValue();
                if (serviceName == null)
                    return;

                var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                if (referenceExpression == null)
                    return;

                var secondArgument = invocationExpression.Arguments[1];
                if (secondArgument == null)
                    return;

                var serviceGlobalType = GetHiddenGlobalPropertyType(serviceName);
                IJsUnresolvedType serviceType = null;
                var serviceOffset = 0;
                var factoryFunction = secondArgument as IFunctionExpression;

                // TODO: Should probably check qualifier is a module, or $provide
                switch (referenceExpression.Name)
                {
                    case "provider":
                        if (factoryFunction != null)
                        {
                            // The factory is a constructor that builds an object with a $get property
                            // that is a function that returns the service. So, we want the return value
                            // of the $get function of the constructed factory. Who said Angular was hard?
                            // TODO: $get might be an injectable function, that is wrapped in an array literal...
                            // And the output of the function might depend on what's injected...
                            var constructedFactoryType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);

                            var providerGlobalType = GetHiddenGlobalPropertyType(serviceName + "Provider");

                            serviceType = constructedFactoryType.GetPropertyReferenceType("$get").GetReturnType();
                            serviceOffset = context.GetDocumentStartOffset(factoryFunction);

                            context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, providerGlobalType, constructedFactoryType, serviceOffset);
                        }
                        break;

                    case "factory":
                        if (factoryFunction != null)
                        {
                            // The return type of the factory function
                            serviceType = factoryFunction.GetFunctionImplicitReturnType(context);
                            serviceOffset = context.GetDocumentStartOffset(factoryFunction);
                        }
                        break;

                    case "service":
                        if (factoryFunction != null)
                        {
                            // The factory function is a constructor, use the constructed type
                            // TODO: Is this right? What invocation info should I use?
                            serviceType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                            serviceOffset = context.GetDocumentStartOffset(factoryFunction);
                        }
                        break;

                    case "value":
                    case "constant":
                        // The type of the second argument - function, value, etc.
                        serviceType = secondArgument.GetJsType(context);
                        serviceOffset = context.GetDocumentStartOffset(secondArgument);
                        break;

                    // TODO: "decorator" - override an existing service
                    // This will have the same name, so could mess up our naming scheme...
                    // Can return its own type, or something based on the original type
                }

                if (serviceGlobalType == null || serviceType == null)
                    return;

                // Register an assignment from the actual service type to the global service type.
                // Now the global service type has the type of the actual service.
                context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, serviceGlobalType, serviceType, serviceOffset);
            }

            private void ProcessServiceInjection(IInvocationExpression invocationExpression)
            {
                if (!IsInjectableFunction(invocationExpression))
                    return;

                // For config/run, the factory function is the first argument. For the others,
                // it's the second argument. Technically, that means it's always the last
                var argument = invocationExpression.Arguments.Last();
                var factoryFunction = GetFactoryFunction(argument);
                if (factoryFunction == null)
                    return;

                ProcessServiceInjection(argument, factoryFunction);
            }

            private void ProcessServiceInjection(IExpressionOrSpread argument, IFunctionExpression factoryFunction)
            {
                var injectedServiceNames = GetInjectedServiceNames(argument, factoryFunction);

                for (var i = 0; i < factoryFunction.Parameters.Count; i++)
                {
                    var parameter = factoryFunction.Parameters[i];
                    var serviceName = injectedServiceNames[i];

                    var parameterType = parameter.GetParameterType(context);

                    var serviceGlobalType = GetHiddenGlobalPropertyType(serviceName);

                    // Register a association for the parameter type to be declared as the service global type,
                    // which in turn has been assigned the type of the actual service.
                    // This declares the parameter to have the same type as the actual service
                    context.AddAssocRule(JsRuleType.Declaration, JsTypingType.ExplicitTyping, parameterType, serviceGlobalType,
                        context.GetDocumentStartOffset(parameter));
                }
            }

            private bool IsInjectableFunction(IInvocationExpression invocationExpression)
            {
                var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                if (referenceExpression == null)
                    return false;

                if (invocationExpression.Arguments.Count == 1)
                {
                    return referenceExpression.Name == "config" || referenceExpression.Name == "run";
                }

                if (invocationExpression.Arguments.Count == 2)
                {
                    var expression = invocationExpression.Arguments[0] as IJavaScriptExpression;
                    if (expression == null || !expression.IsStringLiteral())
                        return false;

                    // TODO: Also handle $injector.invoke, $injector.get, provider.$get?
                    // What about injecting providers into module.config?
                    // TODO: Also handle injection into the functions passed to the service registration
                    if (!InjectableNames.Contains(referenceExpression.Name))
                        return false;

                    return true;
                }

                return false;
            }

            private IFunctionExpression GetFactoryFunction(IExpressionOrSpread argument)
            {
                var factoryFunction = argument as IFunctionExpression;
                if (factoryFunction == null)
                {
                    var arrayLiteral = argument as IArrayLiteral;
                    if (arrayLiteral != null)
                        factoryFunction = arrayLiteral.ArrayElements.LastOrDefault() as IFunctionExpression;
                }

                return factoryFunction;
            }

            private string[] GetInjectedServiceNames(IExpressionOrSpread secondArgument, IFunctionExpression factoryFunction)
            {
                // TODO: Get the names from the $inject string literal array
                var injectedServiceNames = factoryFunction.Parameters.Select(p => p.GetDeclaredName()).ToArray();

                var arrayLiteral = secondArgument as IArrayLiteral;
                if (arrayLiteral != null)
                {
                    for (int i = 0; i < injectedServiceNames.Length && i < arrayLiteral.ArrayElements.Count; i++)
                    {
                        var serviceName = arrayLiteral.ArrayElements[i].GetStringLiteralValue();
                        if (!string.IsNullOrEmpty(serviceName))
                            injectedServiceNames[i] = serviceName;
                    }
                }

                return injectedServiceNames;
            }

            private IJsUnresolvedType GetHiddenGlobalPropertyType(string serviceName)
            {
                // Use special system property name prefix so that ReSharper ignores this property in
                // things such as code completion and symbol navigation
                return
                    JavaScriptType.GlobalType.GetPropertyReferenceType(string.Format("{0}$angular$injectable${1}",
                        JavaScriptPsiImplUtil.SystemPropertyNamePrefix, serviceName));
            }
        }
    }
}