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

using System.Collections.Generic;
using System.Drawing;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.Html;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.UI.RichText;

#if RESHARPER_8
using JetBrains.ReSharper.Psi.Modules;
#endif

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    [DeclaredElementDescriptionProvider]
    public class AngularJsHtmlElementDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        private readonly HtmlDescriptionsCache htmlDescriptionsCache;

        public AngularJsHtmlElementDescriptionProvider(HtmlDescriptionsCache htmlDescriptionsCache)
        {
            this.htmlDescriptionsCache = htmlDescriptionsCache;
        }

        // This is the ReSharper 8.2 version. 'context' has a default value of null
        public RichTextBlock GetElementDescription(IDeclaredElement element, DeclaredElementDescriptionStyle style,
            PsiLanguageType language, IPsiModule module, IModuleReferenceResolveContext context)
        {
            return GetElementDescription(element, style, language, module);
        }

        // This is the ReSharper 8.1 version
        public RichTextBlock GetElementDescription(IDeclaredElement element, DeclaredElementDescriptionStyle style,
                                                   PsiLanguageType language, IPsiModule module)
        {
            var attribute = element as IHtmlAttributeDeclaredElement;
            if (attribute == null)
                return null;

            var attributeDescription = GetAttributeDescription(attribute.ShortName);

            var block = new RichTextBlock();
            var typeDescription = new RichText(htmlDescriptionsCache.GetDescriptionForHtmlValueType(attribute.ValueType));
            if (style.IntendedDescriptionPlacement == DescriptionPlacement.AFTER_NAME &&
                (style.ShowSummary || style.ShowFullDescription))
                block.SplitAndAdd(typeDescription);

            string description = null;
            if (style.ShowSummary && attributeDescription != null)
                description = attributeDescription.Summary;
            else if (style.ShowFullDescription && attributeDescription != null)
                description = attributeDescription.Description;

            if (!string.IsNullOrEmpty(description))
                block.SplitAndAdd(description);

            if (style.IntendedDescriptionPlacement == DescriptionPlacement.ON_THE_NEW_LINE &&
                (style.ShowSummary || style.ShowFullDescription))
            {
                // TODO: Perhaps we should show Value: Expression for attributes that take an Angular expression, etc
                typeDescription.Prepend("Value: ");
                block.SplitAndAdd(typeDescription);
            }

            return block;
        }

        private static HtmlDescriptionsCache.AttributeDescription GetAttributeDescription(string shortName)
        {
            HtmlDescriptionsCache.AttributeDescription attributeDescription;
            if (shortName.StartsWith("data-"))
            {
                shortName = shortName.Substring("data-".Length);
            }
            AttributeDescriptions.TryGetValue(shortName, out attributeDescription);
            return attributeDescription;
        }

        public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock obsoleteDescription,
                                       DeclaredElementDescriptionStyle style)
        {
            obsoleteDescription = null;
            var obsoletable = element as IObsoletable;
            if (obsoletable == null)
                return null;

            if (!obsoletable.Obsolete && !obsoletable.NonStandard)
                return false;

            obsoleteDescription = new RichTextBlock();
            var richText = new RichText();
            if (obsoletable.Obsolete)
                richText.Append("Obsolete!", new TextStyle(FontStyle.Bold));
            else if (obsoletable.NonStandard)
                richText.Append("Non-standard!", new TextStyle(FontStyle.Bold));

            obsoleteDescription.Add(richText);

            return true;
        }

        // Needs to be less than the priority of the HtmlDescriptionsCache's implementation,
        // or it takes precedence and we get a boring description (it's actually annoying that
        // we can't use HtmlDescriptionsCache's implementation, as it does everything we want
        // from a nice handy xml file, that's annoyingly hardcoded)
        public int Priority { get { return -1; } }

        private static readonly IDictionary<string, HtmlDescriptionsCache.AttributeDescription> AttributeDescriptions = new Dictionary<string, HtmlDescriptionsCache.AttributeDescription>
            {
                {"ng-app", CreateDescription("ng-app", "Used to auto-bootstrap on application", "Use this directive to auto-bootstrap on application. Only one directive can be used per HTML document. The directive designates the root of the application and is typically placed at the root of the page.")},
                {"ng-bind", CreateDescription("ng-bind", "Replaces the text content of the element with the value of the given expression", "The ngBind attribute tells Angular to replace the text content of the specified HTML element with the value of a given expression, and to update the text content when the value of that expression changes.")},
                {"ng-bind-html", CreateDescription("ng-bind-html", "Replaces the HTML content of the element with the value of the given expression", "description")},
                {"ng-bind-html-unsafe", CreateDescription("ng-bind-html-unsafe", "Replaces the HTML content of the element with the un-santized value of the given expression", "Creates a binding that will innerHTML the result of evaluating the expression into the current element. The innerHTML-ed content will not be sanitized! You should use this directive only if ngBindHtml directive is too restrictive and when you absolutely trust the source of the content you are binding to.")},
                {"ng-bind-template", CreateDescription("ng-bind-template", "Relaces the element text with the given template", "The ngBindTemplate directive specifies that the element text should be replaced with the template in ngBindTemplate. Unlike ngBind the ngBindTemplate can contain multiple {{ }} expressions. (This is required since some HTML elements can not have SPAN elements such as TITLE, or OPTION to name a few.)")},
                {"ng-change", CreateDescription("ng-change", "Evaluate given expression when user changes the input", "Evaluate given expression when user changes the input. The expression is not evaluated when the value change is coming from the model.\r\nNote, this directive requires ngModel to be present.")},
                {"ng-checked", CreateDescription("ng-checked", "Allows Angular to see the checked attribute", "The HTML specs do not require browsers to preserve the special attributes such as checked. (The presence of them means true and absence means false) This prevents the angular compiler from correctly retrieving the binding expression. To solve this problem, we introduce the ngChecked directive.")},
                {"ng-class", CreateDescription("ng-class", "Set the CSS class attribute from an expression", "The ngClass allows you to set CSS class on HTML element dynamically by databinding an expression that represents all classes to be added.\r\nThe directive won't add duplicate classes if a particular class was already set.\r\nWhen the expression changes, the previously added classes are removed and only then the new classes are added.")},
                {"ng-class-even", CreateDescription("ng-class-even", "Set the CSS class attribute from an expression, for even rows", "The ngClassOdd and ngClassEven works exactly as ngClass, except it works in conjunction with ngRepeat and takes affect only on odd (even) rows.\r\nThis directive can be applied only within a scope of an ngRepeat.")},
                {"ng-class-odd", CreateDescription("ng-class-odd", "Set the CSS class attribute from an expression, for odd rows", "The ngClassOdd and ngClassEven works exactly as ngClass, except it works in conjunction with ngRepeat and takes affect only on odd (even) rows.\r\nThis directive can be applied only within a scope of an ngRepeat.")},
                {"ng-click", CreateDescription("ng-click", "Specify custom behaviour for the click event", "The ngClick allows you to specify custom behavior when element is clicked.")},
                {"ng-cloak", CreateDescription("ng-cloak", "Prevent the Angular HTML template being displayed during loading", "The ngCloak directive is used to prevent the Angular html template from being briefly displayed by the browser in its raw (uncompiled) form while your application is loading. Use this directive to avoid the undesirable flicker effect caused by the html template display.")},
                {"ng-controller", CreateDescription("ng-controller", "Assigns behaviour to a scope", "The ngController directive assigns behavior to a scope. This is a key aspect of how angular supports the principles behind the Model-View-Controller design pattern.")},
                {"ng-csp", CreateDescription("ng-csp", "Enables Content Security Policy support", "Enables CSP (Content Security Policy) support. This directive should be used on the root element of the application (typically the <html> element or other element with the ngApp directive).\r\nIf enabled the performance of template expression evaluator will suffer slightly, so don't enable this mode unless you need it.")},
                {"ng-dblclick", CreateDescription("ng-dblclick", "Specify custom behaviour for the dblclick event", "The ngDblclick directive allows you to specify custom behavior on dblclick event.")},
                {"ng-disabled", CreateDescription("ng-disabled", "Allows Angular to see the disabled attribute", "The HTML specs do not require browsers to preserve the special attributes such as disabled. (The presence of them means true and absence means false) This prevents the angular compiler from correctly retrieving the binding expression. To solve this problem, we introduce the ngDisabled directive.")},
                {"ng-form", CreateDescription("ng-form", "Nestable alias of form directive", "Nestable alias of form directive. HTML does not allow nesting of form elements. It is useful to nest forms, for example if the validity of a sub-group of controls needs to be determined.")},
                {"ng-hide", CreateDescription("ng-hide", "Hides a portion of the DOM based on the expression", "The ngHide and ngShow directives hide or show a portion of the DOM tree (HTML) conditionally.")},
                {"ng-href", CreateDescription("ng-href", "Used to prevent href attributes containing invalid Angular expressions", "Using Angular markup like in an href attribute makes the page open to a wrong URL, if the user clicks that link before angular has a chance to replace the with actual URL, the link will be broken and will most likely return a 404 error. The ngHref directive solves this problem.")},
                {"ng-if", CreateDescription("ng-if", "Removes or recreates a portion of the DOM tree based on an expression", "Removes or recreates a portion of the DOM tree based on an {expression}. If the expression assigned to ngIf evaluates to a false value then the element is removed from the DOM, otherwise a clone of the element is reinserted into the DOM.")},
                {"ng-include", CreateDescription("ng-include", "Fetches, compiles and includes an external HTML fragment.", "Fetches, compiles and includes an external HTML fragment.\r\nKeep in mind that Same Origin Policy applies to included resources (e.g. ngInclude won't work for cross-domain requests on all browsers and for file:// access on some browsers).")},
                {"ng-init", CreateDescription("ng-init", "Specifies tasks to execute during bootstrap", "The ngInit directive specifies initialization tasks to be executed before the template enters execution mode during bootstrap.")},
                {"ng-list", CreateDescription("ng-list", "Text input that converts between comma-separated string into an array of strings.", "Text input that converts between comma-separated string into an array of strings.")},
                {"ng-model", CreateDescription("ng-model", "Create two way binding", "Create two way binding. It works together with input, select, textarea. You can easily write your own directives to use ngModel as well.")},
                {"ng-mousedown", CreateDescription("ng-mousedown", "Specify custom behaviour for the mousedown event", "Specify custom behaviour for the mousedown event")},
                {"ng-mouseenter", CreateDescription("ng-mouseenter", "Specify custom behaviour for the mouseenter event", "Specify custom behaviour for the mouseenter event")},
                {"ng-mouseleave", CreateDescription("ng-mouseleave", "Specify custom behaviour for the mouseleave event", "Specify custom behaviour for the mouseleave event")},
                {"ng-mousemove", CreateDescription("ng-mousemove", "Specify custom behaviour for the mousemove event", "Specify custom behaviour for the mousemove event")},
                {"ng-mouseover", CreateDescription("ng-mouseover", "Specify custom behaviour for the mouseover event", "Specify custom behaviour for the mouseover event")},
                {"ng-mouseup", CreateDescription("ng-mouseup", "Specify custom behaviour for the mouseup event", "Specify custom behaviour for the mouseup event")},
                {"ng-multiple", CreateDescription("ng-multiple", "Allows Angular to see the multiple attribute", "The HTML specs do not require browsers to preserve the special attributes such as multiple. (The presence of them means true and absence means false) This prevents the angular compiler from correctly retrieving the binding expression. To solve this problem, we introduce the ngMultiple directive.")},
                {"ng-non-bindable", CreateDescription("ng-non-bindable", "Used to make Angular ignore the element", "Sometimes it is necessary to write code which looks like bindings but which should be left alone by angular. Use ngNonBindable to make angular ignore a chunk of HTML.")},
                // TODO: ng-options is specfic to the select element
                {"ng-options", CreateDescription("ng-options", "Generates a list of option elements for the select element", "Dynamically generates a list of option elements for the select element using the array or object obtained by evaluating the attribute's expression")},
                {"ng-pluralize", CreateDescription("ng-pluralize", "Displays messages according to localisation rules", "ngPluralize is a directive that displays messages according to en-US localization rules. These rules are bundled with angular.js and the rules can be overridden (see Angular i18n dev guide). You configure ngPluralize directive by specifying the mappings between plural categories and the strings to be displayed.")},
                {"ng-readonly", CreateDescription("ng-readonly", "Allows Angular to see the readonly attribute", "The HTML specs do not require browsers to preserve the special attributes such as readonly. (The presence of them means true and absence means false) This prevents the angular compiler from correctly retrieving the binding expression. To solve this problem, we introduce the ngReadonly directive.")},
                {"ng-repeat", CreateDescription("ng-repeat", "Instantiates a template once per item from a collection", "The ngRepeat directive instantiates a template once per item from a collection. Each template instance gets its own scope, where the given loop variable is set to the current collection item, and $index is set to the item index or key.")},
                {"ng-selected", CreateDescription("ng-selected", "Allows Angular to see the selected attribute", "The HTML specs do not require browsers to preserve the special attributes such as selected. (The presence of them means true and absence means false) This prevents the angular compiler from correctly retrieving the binding expression. To solve this problem, we introduced the ngSelected directive.")},
                {"ng-show", CreateDescription("ng-show", "Shows a portion of the DOM based on the expression", "The ngHide and ngShow directives hide or show a portion of the DOM tree (HTML) conditionally.")},
                {"ng-src", CreateDescription("ng-src", "Used to prevent src attributes containing invalid Angular expressions", "Using Angular markup like {{hash}} in a src attribute doesn't work right: The browser will fetch from the URL with the literal text {{hash}} until Angular replaces the expression inside {{hash}}. The ngSrc directive solves this problem.")},
                {"ng-style", CreateDescription("ng-style", "Set the CSS styles from an expression", "The ngStyle directive allows you to set CSS style on an HTML element conditionally. It evaluates an expression which evals to an object whose keys are CSS style names and values are corresponding values for those CSS keys.")},
                {"ng-submit", CreateDescription("ng-submit", "Specify custom behaviour for the onsubmit event", "Enables binding angular expressions to onsubmit events.\r\nAdditionally it prevents the default action (which for form means sending the request to the server and reloading the current page).")},
                {"ng-switch", CreateDescription("ng-switch", "Conditionally change the DOM structure", "Conditionally change the DOM structure.")},
                {"ng-transclude", CreateDescription("ng-transclude", "Insert the transcluded DOM here", "Insert the transcluded DOM here.")},
                {"ng-view", CreateDescription("ng-view", "Insert the view for the current route here", "ngView is a directive that complements the $route service by including the rendered template of the current route into the main layout (index.html) file. Every time the current route changes, the included view changes with it according to the configuration of the $route service.")},
            };

        private static HtmlDescriptionsCache.AttributeDescription CreateDescription(string name, string summary,
                                                                             string description)
        {
            return new HtmlDescriptionsCache.AttributeDescription(name, summary, description, obsolete: false, obsoleteDescription: null, unimplemented: false, nonStandard: true);
        }
    }
}
