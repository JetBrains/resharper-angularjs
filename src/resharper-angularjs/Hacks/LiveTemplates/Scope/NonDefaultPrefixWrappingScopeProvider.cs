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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Hacks.LiveTemplates.Scope
{
    // ReSharper doesn't correctly handle templates with a non-default prefix, i.e.
    // it only correctly handles templates that start with characters, digits or '_'.
    // We have templates that start with '$'
    // This class allows ReSharper to expand templates with the tab key. The default
    // JavaScript scope provider creates scopes that represent the current context in
    // a JS file, but doesn't provide any custom prefixes, so defaults to '_'. Here
    // we create the same scopes, but wrap them, and create a prefix that includes
    // the '$' character, so we can better match against the template shortcut
    public abstract class NonDefaultPrefixWrappingScopeProvider<T> : IScopeProvider
        where T : IScopeProvider
    {
        private readonly T originalScopeProvider;

        protected NonDefaultPrefixWrappingScopeProvider(T originalScopeProvider)
        {
            this.originalScopeProvider = originalScopeProvider;
        }

        public IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            return from scopePoint in originalScopeProvider.ProvideScopePoints(context)
                select (ITemplateScopePoint) new ReplacingAllowedPrefixCharsScopePoint(scopePoint, context.Document, context.Selection.GetMinOffset(), JsAllowedPrefixes.Chars);
        }

        public ITemplateScopePoint ReadFromXml(XmlElement scopeElement)
        {
            return null;
        }

        public ITemplateScopePoint CreateScope(Guid scopeGuid, string typeName, IEnumerable<Pair<string, string>> customProperties)
        {
            return null;
        }
    }
}