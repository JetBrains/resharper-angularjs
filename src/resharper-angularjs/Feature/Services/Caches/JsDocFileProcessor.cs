using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi.JavaScript.Tree.JsDoc;
using JetBrains.ReSharper.Psi.JavaScript.Util.JsDoc;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class JsDocFileProcessor
    {
        private readonly AngularJsCacheItemsBuilder cacheBuilder;

        public JsDocFileProcessor(AngularJsCacheItemsBuilder cacheBuilder)
        {
            this.cacheBuilder = cacheBuilder;
        }

        public void ProcessJsDocFile(IJsDocFile jsDocFile)
        {
            ISimpleTag ngdocTag = null;
            ISimpleTag nameTag = null;
            ISimpleTag restrictTag = null;
            ISimpleTag elementTag = null;
            IList<IParameterTag> paramTags = null;

            foreach (var simpleTag in jsDocFile.GetTags<ISimpleTag>())
            {
                if (simpleTag.Keyword == null)
                    continue;

                if (simpleTag.Keyword.GetText() == "@ngdoc")
                    ngdocTag = simpleTag;
                else if (simpleTag.Keyword.GetText() == "@name")
                    nameTag = simpleTag;
                else if (simpleTag.Keyword.GetText() == "@restrict")
                    restrictTag = simpleTag;
                else if (simpleTag.Keyword.GetText() == "@element")
                    elementTag = simpleTag;
            }

            foreach (var parameterTag in jsDocFile.GetTags<IParameterTag>())
            {
                if (paramTags == null)
                    paramTags = new List<IParameterTag>();
                paramTags.Add(parameterTag);
            }

            if (ngdocTag != null && nameTag != null)
            {
                var nameValue = nameTag.DescriptionText;
                var name = string.IsNullOrEmpty(nameValue) ? null : nameTag.DescriptionText;

                // TODO: Should we strip off the module?
                // What about 3rd party documented code?
                if (!string.IsNullOrEmpty(name))
                    name = name.Substring(name.IndexOf(':') + 1);

                var nameOffset = nameTag.GetDocumentStartOffset().Offset;

                var ngdocValue = ngdocTag.DescriptionText;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ngdocValue))
                {
                    // TODO: Could support "event", "function", etc.
                    if (ngdocValue == "directive")
                    {
                        // Default is AE for 1.3 and above, just A for 1.2
                        // This why the IntelliJ plugin uses "D", and resolves when required
                        // Also checks angular version by presence of known directives for those
                        // versions
                        var restrictions = restrictTag != null ? restrictTag.DescriptionText : "AE";
                        var element = elementTag != null ? elementTag.DescriptionText : "ANY";
                        var tags = element.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);

                        // Pull the attribute/element type from the param tag(s). Optional parameters
                        // are specified with a trailing equals sign, e.g. {string=}
                        // There can be more than one parameter, especially for E directives, e.g. textarea
                        // (Presumably these parameters are named attributes)
                        // Type can also be another directive, e.g. textarea can have an ngModel parameter
                        // Might be worth setting up special attribute types, e.g. string, expression, template
                        // For attributes, the parameter name is the same as the directive name

                        name = StringUtil.Unquote(name);
                        var formattedName = DirectiveUtil.GetNormalisedName(name);

                        // TODO: There might be alternative names <- what does this mean?
                        // If the description starts with e.g. "|name ", then this is an alternative name
                        // Type can be string, expression, number, boolean
                        // TODO: Type can also be multiple values, e.g. string|expression (ngPluralize.count)
                        // TODO: A parameter with the same name as the directive gives the type + default value of the directive itself - add this information to Directive
                        var parameters = from p in paramTags ?? EmptyArray<IParameterTag>.Instance
                            let parameterName = DirectiveUtil.GetNormalisedName(p.DeclaredName)
                            where !parameterName.Equals(formattedName, StringComparison.InvariantCultureIgnoreCase)
                            select CreateParameter(p, parameterName);

                        cacheBuilder.Add(new Directive(name, formattedName, restrictions, tags, nameOffset, parameters.ToList()));
                    }
                    else if (ngdocValue == "filter")
                    {
                        cacheBuilder.Add(new Filter(name, nameOffset));
                    }
                }
            }
        }

        private static Parameter CreateParameter(IParameterTag parameterTag, string parameterName)
        {
            var description = parameterTag.DescriptionText;
            var type = parameterTag.DeclaredType.Replace("=", string.Empty);
            var isOptional = parameterTag.DeclaredType.EndsWith("=");
            var defaultValue = parameterTag.DefaultValue != null ? parameterTag.DefaultValue.GetText() : string.Empty;
            return CreateParameter(parameterName, description, type, isOptional, defaultValue);
        }

        private static Parameter CreateParameter(string parameterName, string description, string type, bool isOptional, string defaultValue)
        {
            return new Parameter(parameterName, type, isOptional, description, defaultValue);
        }
    }
}