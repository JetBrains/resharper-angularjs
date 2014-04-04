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

using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;

// TODO: Move namespace to JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJS.Parsing
// A bug in the TokenGenerator tool means that the namespace of this class
// has to start with JetBrains.ReSharper.Psi or it fails to compile (it uses
// JetBrains.ReSharper.Psi.TreeOffset without the appropriate using statement)
// See: http://youtrack.jetbrains.com/issue/RSRP-411978
// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Psi.AngularJs.Parsing
{
    // Here is where we would define the KeywordTokenNodeType, FixedTokenElement,
    // and FixedTokenNodeType base classes. But we're piggy-backing on the JS
    // parser, so we can use those definitions, by inheriting from JavaScriptTokenType
    [UsedImplicitly]
    public partial class AngularJsTokenType : JavaScriptTokenType
    {
    }
}