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

    // Set up types for injected services and providers.
    // A refresher:
    // * A consumer can be injected with a provider or service (the term service is overloaded).
    // * A provider is a constructor function that builds an object that holds state, provides
    //   configuration methods, and includes a $get property that is used to create the service.
    // * A service is the return value of the function associated with a provider's $get property.
    //   When injecting a provider or service, we create an association between the consumer's
    //   parameter type and the provider or service type, via a hidden global object.
    //   For example, given a service 'foo' and provider 'fooProvider'
    //
    // module.factory, module.service, module.value and module.constant are just shorcuts for
    // creating providers that have different return types. They essentially implement $get differently.
    //
    // * module.factory injects a parameter with the return type of the factory function
    // * module.service injects a parameter with the constructed type of the factory function
    // * module.value and module.constant inject a parameter with the type of the passed object
    //   (number, string, function, etc.)
    // * module.provider is a shortcut for setting up a provider as described above. The passed
    //   in factory function is a constructor that builds an object with properties for configuration,
    //   etc. and also a $get property that is used to construct the service.
    //
    // Injecting types works by creating associations in the type system between the types of the
    // providers and services and the parameter types of the injectable functions, via hidden global
    // objects. The type information flows through the associations, and the parameters will show
    // code completion for the injected types. We're explicitly telling the type system about associations
    // it would normally pick up through variable assignments (e.g. if you assign a string to an object,
    // then you know that other object is also a string). The nice thing is that the type system is
    // loosley coupled. We can set up an association from a named object to another named object, and
    // if either end doesn't exist, then type information just doesn't flow through.
    //
    // In theory:
    // For example, given a provider called 'fooProvider' that creates a service called 'foo'
    //
    // Registration:
    // * Create hidden global object types called 'fooProvider' and 'foo'
    // * Find the provider's constructor function
    // * Create an assignment association between the provider global type and the constructed type
    //   of the provider constructor
    // * Find the provider's $get property, and get the return type of the associated factory
    //   function (this might be an injected function)
    // * Create an assignment association between the return type of the $get factory function
    //   and the global provider object type
    //
    // Injecting:
    // * A declaration association is created from the parameter type to the hidden global object
    //   identified by the name of the parameter (or the name from its containing array literal).
    // * If the parameter is 'foo', the association is to the 'foo' service global type, which has
    //   an association to the return type of the $get property's factory function
    // * If the parameter is 'fooProvider', the association is to the 'fooProvider' provider global
    //   type, which has an asociation to the constructed type returned by the provider's factory
    //   function.
    //
    // In practice:
    // * module.factory, service, provider, value and constant are special cased, and the service
    //   global type is associated with the factory's return type, the constructed type of module.service's
    //   factory function, and the type of the object passed to value and constant. module.provider
    //   creates the service global type from the constructed passed factory function's $get property,
    //   but also creates a <serviceName>Provider global object with the constructed factory function.
    // * When processing the source file, the $get property might be a factory function, or an array
    //   literal that contains an injectable factory function. So we can't automatically set up an
    //   association between the global service type and the return type of $get. We also don't have
    //   enough information at that point to examine the $get property and see if it's an array.
    // * Instead, we create an association between the global service and a composite type - the $get
    //   return type, and another hidden global object named after the provider function name (e.g.
    //   'FooProvider', not 'fooProvider' - i.e. 'FooProvider$get')
    // * Later, when we do find the $get property, we can get the accurate type of the factory function
    //   and create an association between 'FooProvider$get' and this accurate type
    // * Similarly (and especially for the built in services) the provider function might also be an
    //   injectable, with array literal, so we can't always say that the provider is the constructed type
    //   of the value passed to $provider.provide. So we set up another composite type, and another
    //   hidden global object (e.g. 'FooProviderFn') to provide the accurate type when we're parsing
    //   the appropriate part of the file
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
            private const string ProviderSuffix = "Provider";

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

            // Bulk registration of builtin services - $provide.provider({ $log: $LogProvider, ... });
            // Registration of custom services - module.factory, module.service, etc.
            // Usage of custom services
            public override void VisitInvocationExpression(IInvocationExpression invocationExpression)
            {
                ProcessBulkProviderRegistration(invocationExpression);
                ProcessModuleHelperMethodsRegistration(invocationExpression);
                ProcessServiceInjection(invocationExpression);
            }

            public override void VisitVariableStatement(IVariableStatement variableStatement)
            {
                foreach (var variableDeclaration in variableStatement.DeclarationsEnumerable)
                    ProcessProviderFunctionDeclaration(variableDeclaration);
                base.VisitVariableStatement(variableStatement);
            }

            public override void VisitFunctionExpression(IFunctionExpression functionExpression)
            {
                ProcessProviderFunctionDeclaration(functionExpression);
                base.VisitFunctionExpression(functionExpression);
            }

            public override void VisitSimpleAssignmentExpression(ISimpleAssignmentExpression simpleAssignmentExpression)
            {
                ProcessInjectionIntoProviderGet(simpleAssignmentExpression);
                base.VisitSimpleAssignmentExpression(simpleAssignmentExpression);
            }

            // Built in service registration, via object literal
            // $provider.provide({$log: $LogProvider, ...});
            // '$log' is the service name, '$LogProvider' is a reference to a provider function, or
            // an injectable array literal
            private void ProcessBulkProviderRegistration(IInvocationExpression invocationExpression)
            {
                if (!IsProviderProvideInvocation(invocationExpression) || invocationExpression.Arguments.Count != 1)
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
                    AssociateToServiceGlobalType(serviceName, providerFunctionName, providerExpression);
                    AssociateToProviderGlobalType(serviceName, providerFunctionName, providerExpression);
                }
            }

            // Calling module.factory, module.service, etc.
            private void ProcessModuleHelperMethodsRegistration(IInvocationExpression invocationExpression)
            {
                if (invocationExpression.Arguments.Count != 2)
                    return;

                var serviceName = invocationExpression.Arguments[0].GetStringLiteralValue();
                var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                var secondArgument = invocationExpression.Arguments[1];

                if (string.IsNullOrEmpty(serviceName) || referenceExpression == null || secondArgument == null)
                    return;

                var factoryFunction = secondArgument as IFunctionExpression;

                // TODO: Should probably check qualifier is a module, or $provide
                switch (referenceExpression.Name)
                {
                    case "provider":
                        ProcessModuleProviderRegistration(serviceName, factoryFunction);
                        break;

                    case "factory":
                        ProcessModuleFactoryRegistration(serviceName, factoryFunction);
                        break;

                    case "service":
                        ProcessModuleServiceRegistration(serviceName, factoryFunction);
                        break;

                    case "value":
                    case "constant":
                        ProcessModuleValueRegistration(serviceName, secondArgument);
                        break;

                    // TODO: "decorator" - override an existing service
                    // This will have the same name, so could mess up our naming scheme...
                    // Can return its own type, or something based on the original type
                }
            }

            private void ProcessModuleProviderRegistration(string serviceName, IFunctionExpression factoryFunction)
            {
                if (factoryFunction == null)
                    return;

                // The function is likely to be anonymous, in which case, create a default name
                var providerName = factoryFunction.HasName ? factoryFunction.DeclaredName : serviceName + ProviderSuffix;

                AssociateToProviderGlobalType(serviceName, providerName, factoryFunction);
                AssociateToServiceGlobalType(serviceName, providerName, factoryFunction);
            }

            private void ProcessModuleFactoryRegistration(string serviceName, IFunctionExpression factoryFunction)
            {
                if (factoryFunction == null)
                    return;

                // The return type of the factory function
                var serviceType = factoryFunction.GetFunctionImplicitReturnType(context);
                var serviceOffset = context.GetDocumentStartOffset(factoryFunction);
                AssociateToServiceGlobalType(serviceName, serviceType, serviceOffset);
            }

            private void ProcessModuleServiceRegistration(string serviceName, IFunctionExpression factoryFunction)
            {
                if (factoryFunction == null)
                    return;

                // The factory function is a constructor, use the constructed type
                // TODO: Is this right? What invocation info should I use?
                var serviceType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                var serviceOffset = context.GetDocumentStartOffset(factoryFunction);
                AssociateToServiceGlobalType(serviceName, serviceType, serviceOffset);
            }

            private void ProcessModuleValueRegistration(string serviceName, IExpressionOrSpread expression)
            {
                // The type of the second argument - function, value, etc.
                var serviceType = expression.GetJsType(context);
                var serviceOffset = context.GetDocumentStartOffset(expression);
                AssociateToServiceGlobalType(serviceName, serviceType, serviceOffset);
            }

            // Calling controller, directive, filter, etc.
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

            private void ProcessServiceInjection(IExpressionOrSpread owner, IFunctionExpression factoryFunction)
            {
                var injectedServiceNames = GetInjectedServiceNames(owner, factoryFunction);

                for (var i = 0; i < factoryFunction.Parameters.Count; i++)
                {
                    var parameter = factoryFunction.Parameters[i];
                    var serviceName = injectedServiceNames[i];

                    AssociateToParameter(serviceName, parameter);
                }
            }

            private string[] GetInjectedServiceNames(IExpressionOrSpread owner, IFunctionExpression factoryFunction)
            {
                // TODO: Get the names from the $inject string literal array
                var injectedServiceNames = factoryFunction.Parameters.Select(p => p.GetDeclaredName()).ToArray();

                var arrayLiteral = owner as IArrayLiteral;
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

            // var $AnimateProvider = [ '$provide', function($provide) { ...
            private void ProcessProviderFunctionDeclaration(IVariableDeclaration variableDeclaration)
            {
                var providerFunctionName = variableDeclaration.NameNode.GetDeclaredName();
                if (string.IsNullOrEmpty(providerFunctionName))
                    return;

                var factoryFunction = GetFactoryFunction(variableDeclaration.Value);
                if (factoryFunction == null)
                    return;

                ProcessProviderFunctionDeclaration(providerFunctionName, factoryFunction);
                AssociateToProviderFunction(providerFunctionName, factoryFunction);
            }

            // Built in services
            // Sets up the type for an injected factory function (type of the last parameter
            // to the array literal assigned to $get)
            // function $BrowserProvider() {
            //   this.$get = [ '$window', '$log', ..., function($window, $log, ...) { ...
            private void ProcessProviderFunctionDeclaration(IFunctionExpression functionExpression)
            {
                ProcessProviderFunctionDeclaration(functionExpression.DeclaredName, functionExpression);
            }

            private void ProcessProviderFunctionDeclaration(string providerFunctionName, IFunctionExpression functionExpression)
            {
                if (!providerFunctionName.EndsWith(ProviderSuffix))
                    return;

                var block = functionExpression.Block;
                if (block == null)
                    return;

                IFunctionExpression providerGetFactoryFunction = null;
                foreach (var statement in block.StatementsEnumerable.OfType<IExpressionStatement>())
                {
                    var assignment = statement.Expression.LastExpression as ISimpleAssignmentExpression;
                    if (assignment == null)
                        continue;

                    if (IsAssignmentToGet(assignment))
                    {
                        providerGetFactoryFunction = GetFactoryFunction(assignment.Source);
                        if (providerGetFactoryFunction != null)
                            break;
                    }
                }

                if (providerGetFactoryFunction != null)
                    AssociateToProviderGet(providerFunctionName, providerGetFactoryFunction);
            }

            // Inject into $get factory functions
            // Improves type information for the returned provider
            // this.$get = [ '$cacheFactory', function($cacheFactory) { ...
            private void ProcessInjectionIntoProviderGet(ISimpleAssignmentExpression simpleAssignmentExpression)
            {
                if (IsAssignmentToGet(simpleAssignmentExpression))
                {
                    var factoryFunction = GetFactoryFunction(simpleAssignmentExpression.Source);
                    if (factoryFunction == null)
                        return;

                    ProcessServiceInjection(simpleAssignmentExpression.Source, factoryFunction);
                }
            }

            private void AssociateToProviderGlobalType(string serviceName, string providerFunctionName,
                IJavaScriptTypedExpression providerExpression)
            {
                var providerGlobalType = GetHiddenGlobalPropertyType(serviceName + ProviderSuffix);

                // The type of the injected provider is the constructed type of the provider factory
                // function. But the referenced expression might not be a factory function, but an
                // injectable array literal. Create a composite type that we can populate later, when
                // we see a provider-like function (e.g. a function ending in Provider)
                var constructedFactoryType = providerExpression.GetJsType(context).
                    GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                var providerFunctionGlobalType = GetProviderFunctionGlobalType(providerFunctionName);
                var providerType = JavaScriptType.CreateCompositeType(JsCombinedTypeKind.JsDynamic,
                    constructedFactoryType, providerFunctionGlobalType);

                // We can't provide a decent offset here. But do we need to?
                CreateAssignmentAssociation(providerGlobalType, providerType, -1);
            }

            private void AssociateToProviderFunction(string providerFunctionName, IFunctionExpression factoryFunction)
            {
                var constructedFactoryType = factoryFunction.GetJsType(context).GetConstructedType(JsUnresolvedTypeArray.EmptyList);
                var providerFunctionGlobalType = GetProviderFunctionGlobalType(providerFunctionName);
                var offset = context.GetDocumentStartOffset(factoryFunction);
                CreateAssignmentAssociation(providerFunctionGlobalType, constructedFactoryType, offset);
            }

            private void AssociateToProviderGet(string providerFunctionName, IFunctionExpression providerGetFactoryFunction)
            {
                var serviceType = providerGetFactoryFunction.GetJsType(context).GetReturnType();
                var providerGetGlobalType = GetProviderGetGlobalType(providerFunctionName);
                var offset = context.GetDocumentStartOffset(providerGetFactoryFunction);
                CreateAssignmentAssociation(providerGetGlobalType, serviceType, offset);
            }

            private void AssociateToServiceGlobalType(string serviceName, string providerFunctionName, IExpressionOrSpread providerExpression)
            {
                var constructedFactoryType = providerExpression.GetJsType(context).
                    GetConstructedType(JsUnresolvedTypeArray.EmptyList);

                // The type of the service is the return type of the $get property, if it's a function.
                // If it's an array literal injectable, we need to add the association later, so create
                // a composite type that allows us to add it later
                var providerGetReturnType = constructedFactoryType.GetPropertyReferenceType("$get").GetReturnType();
                var providerGetGlobalType = GetProviderGetGlobalType(providerFunctionName);
                var serviceType = JavaScriptType.CreateCompositeType(JsCombinedTypeKind.JsDynamic,
                    providerGetReturnType, providerGetGlobalType);

                var offset = context.GetDocumentStartOffset(providerExpression);
                AssociateToServiceGlobalType(serviceName, serviceType, offset);
            }

            private void AssociateToServiceGlobalType(string serviceName, IJsUnresolvedType serviceType, int offset)
            {
                var serviceGlobalType = GetHiddenGlobalPropertyType(serviceName);
                CreateAssignmentAssociation(serviceGlobalType, serviceType, offset);
            }

            private void AssociateToParameter(string serviceName, IJavaScriptParameterDeclaration parameter)
            {
                var parameterType = parameter.GetParameterType(context);
                var serviceGlobalType = GetHiddenGlobalPropertyType(serviceName);
                var offset = context.GetDocumentStartOffset(parameter);
                CreateDeclarationAssociation(parameterType, serviceGlobalType, offset);
            }

            private void CreateAssignmentAssociation(IJsUnresolvedType keyType, IJsUnresolvedType type, int offset)
            {
                context.AddAssocRule(JsRuleType.Assignment, JsTypingType.ExplicitTyping, keyType, type, offset);
            }

            private void CreateDeclarationAssociation(IJsUnresolvedType keyType, IJsUnresolvedType type, int offset)
            {
                context.AddAssocRule(JsRuleType.Declaration, JsTypingType.ExplicitTyping, keyType, type, offset);
            }

            private IJsUnresolvedType GetProviderFunctionGlobalType(string providerFunctionName)
            {
                return GetHiddenGlobalPropertyType(providerFunctionName + "Fn");
            }

            private IJsUnresolvedType GetProviderGetGlobalType(string providerFunctionName)
            {
                return GetHiddenGlobalPropertyType(providerFunctionName + "$get");
            }

            private IJsUnresolvedType GetHiddenGlobalPropertyType(string serviceName)
            {
                // Use special system property name prefix so that ReSharper ignores this property in
                // things such as code completion and symbol navigation
                return
                    JavaScriptType.GlobalType.GetPropertyReferenceType(string.Format("{0}$angular$injectable${1}",
                        JavaScriptPsiImplUtil.SystemPropertyNamePrefix, serviceName));
            }

            private static bool IsAssignmentToGet(ISimpleAssignmentExpression simpleAssignmentExpression)
            {
                var target = simpleAssignmentExpression.Dest as IReferenceExpression;
                return target != null && target.Name == "$get" && target.Qualifier is IThisExpression;
            }

            private static bool IsProviderProvideInvocation(IInvocationExpression invocationExpression)
            {
                var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                if (referenceExpression != null && referenceExpression.Name == "provider")
                {
                    var qualifierReferenceExpression = referenceExpression.Qualifier as IReferenceExpression;
                    return qualifierReferenceExpression != null && qualifierReferenceExpression.Name == "$provide";
                }
                return false;
            }

            private static bool IsInjectableFunction(IInvocationExpression invocationExpression)
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

            private static IFunctionExpression GetFactoryFunction(IExpressionOrSpread argument)
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
        }
    }
}