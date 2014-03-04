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

using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Resolve;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing.Tree
{
    internal class RepeatExpression : JavaScriptExpressionBase
    {
        public override NodeType NodeType
        {
            get { return AngularJsElementType.REPEAT_EXPRESSION; }
        }

        // TODO: Make the children available via properties

        public override IJavaScriptType GetJsType(IJsLocalElementResolver context)
        {
            return JavaScriptType.Empty;
        }
    }
}