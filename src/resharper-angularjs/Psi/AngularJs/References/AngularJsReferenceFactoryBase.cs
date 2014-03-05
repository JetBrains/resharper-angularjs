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

using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.References
{
    public abstract class AngularJsReferenceFactoryBase : StatelessReferenceProviderFactoryBase
    {
        public override bool IsApplicableToFile(IPsiSourceFile sourceFile, IFile file)
        {
            // TODO: Should we have an IAngularJsFile?
            // There isn't an IJsonFile, but there is ITsFile
            return file is IJavaScriptFile && file.Language.Is<AngularJsLanguage>();
        }
    }
}