﻿#region license
// Copyright 2015 JetBrains s.r.o.
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

using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    public static class TreeNodeExtensions
    {
        public static string GetStringLiteralValue(this ITreeNode treeNode)
        {
            var literalExpression = treeNode as IJavaScriptLiteralExpression;
            if (literalExpression != null && literalExpression.IsStringLiteral())
                return literalExpression.GetStringValue();

            return null;
        }
    }
}