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
using System.Xml;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Hacks.LiveTemplates.Scope
{
    internal abstract class DelegatingScopePoint : ITemplateScopePoint
    {
        private readonly ITemplateScopePoint innerScopePoint;

        protected DelegatingScopePoint(ITemplateScopePoint innerScopePoint)
        {
            this.innerScopePoint = innerScopePoint;
        }

        public virtual bool IsSubsetOf(ITemplateScopePoint other)
        {
            return innerScopePoint.IsSubsetOf(other);
        }

        public virtual string CalcPrefix(IDocument document, int caretOffset)
        {
            return innerScopePoint.CalcPrefix(document, caretOffset);
        }

        public virtual XmlElement WriteToXml(XmlElement element)
        {
            return innerScopePoint.WriteToXml(element);
        }

        public virtual Guid GetDefaultUID()
        {
            return innerScopePoint.GetDefaultUID();
        }

        public virtual string GetTagName()
        {
            return innerScopePoint.GetTagName();
        }

        public virtual IEnumerable<Pair<string, string>> EnumerateCustomProperties()
        {
            return innerScopePoint.EnumerateCustomProperties();
        }

        public virtual string Prefix { get; protected set; }
        public virtual string PresentableShortName { get { return innerScopePoint.PresentableShortName; } }
        public virtual PsiLanguageType RelatedLanguage { get { return innerScopePoint.RelatedLanguage; } }

        public virtual Guid UID
        {
            get { return innerScopePoint.UID; }
            set { innerScopePoint.UID = value; }
        }

        public override string ToString()
        {
            return innerScopePoint + " (with JS prefixes)";
        }
    }
}