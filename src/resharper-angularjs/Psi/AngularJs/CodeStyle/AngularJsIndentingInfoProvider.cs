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
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.JavaScript.CodeStyle;
using JetBrains.ReSharper.Psi.JavaScript.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.CodeStyle
{
    [Language(typeof (AngularJsLanguage))]
    public class AngularJsIndentingInfoProvider : JavaScriptIndentingInfoProviderBase
    {
        public AngularJsIndentingInfoProvider(IFormatterDebugInfoLoggersProvider formatterDebugInfoLoggersProvider,
            JavaScriptNodeTypesBase nodeTypeSets)
            : base(formatterDebugInfoLoggersProvider, nodeTypeSets)
        {
        }
    }
}