#region license
// Copyright 2013 JetBrains s.r.o.
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
using System.Xml;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.TemplateHacks
{
    // ReSharper doesn't yet support TypeScript, but templates can still work in
    // TypeScript files if you use the *.ts file map. Unfortunately, that means
    // we get the default prefix chars (just '_'), and our templates starting 
    // with '$' don't work, this scope provider wraps the InFileWithMask provider
    // and adds extra prefixes
    [ShellComponent]
    public class TypeScriptFilePrefixScopeProvider : IScopeProvider
    {
        public IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            if (context.ProjectFile != null && context.ProjectFile.Name.EndsWith(".ts", StringComparison.InvariantCultureIgnoreCase))
            {
                var scopePoint = new InFileWithMask("*.ts");
                return new[] {new DelegatingScopePoint(scopePoint, context.Document, context.CaretOffset)};
            }
            return EmptyList<ITemplateScopePoint>.InstanceList;
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