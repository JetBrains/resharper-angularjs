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

using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.AngularJS.Hacks.LiveTemplates.Scope
{
    internal class ReplacingAllowedPrefixCharsScopePoint : DelegatingScopePoint
    {
        private readonly char[] allowedPrefixChars;

        public ReplacingAllowedPrefixCharsScopePoint(ITemplateScopePoint innerScopePoint, IDocument document, int caretOffset, char[] allowedPrefixChars)
            : base(innerScopePoint)
        {
            this.allowedPrefixChars = allowedPrefixChars;

// ReSharper disable DoNotCallOverridableMethodsInConstructor
            Prefix = CalcPrefix(document, caretOffset);
// ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        public override string CalcPrefix(IDocument document, int caretOffset)
        {
            return document == null ? string.Empty : LiveTemplatesManager.GetPrefix(document, caretOffset, allowedPrefixChars);
        }
    }
}