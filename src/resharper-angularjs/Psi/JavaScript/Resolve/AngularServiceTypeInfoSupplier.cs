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
    // TODO: $get on providers can be injectable, which might help resolve return types
    // An example here might be nice - $get returning something that comes from the type
    // injected in
    // TODO: Get built in services
    // Look for function $<name>Provider, with @ngdoc service, use $get
    // Or how about $provide.provider with an object literal of name: provider
    // Or use @ngdoc provider to get service
    // TODO: Inject providers
    // How does this work?
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
                // Also, $get, $injector.get and $injector.invoke
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

            public override void VisitInvocationExpression(IInvocationExpression invocationExpression)
            {
                ProcessServiceRegistration(invocationExpression);
                ProcessServiceInjection(invocationExpression);
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

                var serviceGlobalType = GetServiceGlobalType(serviceName);
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
                            var constructedFactoryType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                            serviceType = constructedFactoryType.GetPropertyReferenceType("$get").GetReturnType();
                            serviceOffset = factoryFunction.GetTreeStartOffset().Offset;
                        }
                        break;

                    case "factory":
                        if (factoryFunction != null)
                        {
                            // The return type of the factory function
                            serviceType = factoryFunction.GetFunctionImplicitReturnType(context);
                            serviceOffset = factoryFunction.GetTreeStartOffset().Offset;
                        }
                        break;

                    case "service":
                        if (factoryFunction != null)
                        {
                            // The factory function is a constructor, use the constructed type
                            // TODO: Is this right? What invocation info should I use?
                            serviceType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                            serviceOffset = factoryFunction.GetTreeStartOffset().Offset;
                        }
                        break;

                    case "value":
                    case "constant":
                        // The type of the second argument - function, value, etc.
                        serviceType = secondArgument.GetJsType(context);
                        serviceOffset = secondArgument.GetTreeStartOffset().Offset;
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

                var injectedServiceNames = GetInjectedServiceNames(argument, factoryFunction);

                for (var i = 0; i < factoryFunction.Parameters.Count; i++)
                {
                    var parameter = factoryFunction.Parameters[i];
                    var serviceName = injectedServiceNames[i];

                    var parameterType = parameter.GetParameterType(context);

                    var serviceGlobalType = GetServiceGlobalType(serviceName);

                    // Register a association for the parameter type to be declared as the service global type,
                    // which in turn has been assigned the type of the actual service.
                    // This declares the parameter to have the same type as the actual service
                    context.AddAssocRule(JsRuleType.Declaration, JsTypingType.ExplicitTyping, parameterType, serviceGlobalType, context.GetDocumentStartOffset(parameter));
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

            private IFunctionExpression GetFactoryFunction(IExpressionOrSpread secondArgument)
            {
                var factoryFunction = secondArgument as IFunctionExpression;
                if (factoryFunction == null)
                {
                    var arrayLiteral = secondArgument as IArrayLiteral;
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

            private IJsUnresolvedType GetServiceGlobalType(string serviceName)
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