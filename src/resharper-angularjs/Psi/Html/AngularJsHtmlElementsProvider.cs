﻿#region license
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

using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html.Impl.Html;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    [PsiComponent]
    public class AngularJsHtmlElementsProvider : HtmlDeclaredElementsProvider
    {
        public AngularJsHtmlElementsProvider(Lifetime lifetime, ISolution solution)
            : base(lifetime, solution, "JetBrains.ReSharper.Plugins.AngularJS.Resources.HtmlElements.xml", Assembly.GetExecutingAssembly(), true)
        {
        }

        public override bool IsApplicable(IPsiSourceFile file)
        {
            // TODO: Only when angularjs is referenced in the project?
            return true;
        }
    }
}