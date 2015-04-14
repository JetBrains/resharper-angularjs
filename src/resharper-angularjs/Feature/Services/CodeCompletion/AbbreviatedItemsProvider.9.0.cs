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

using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    public partial class AbbreviatedItemsProvider
    {
        public override CompletionMode SupportedCompletionMode
        {
            get { return CompletionMode.All; }
        }

        public override EvaluationMode SupportedEvaluationMode
        {
            get { return EvaluationMode.All; }
        }

        private static int[] GetMatchingIndicies(IdentifierMatcher matcher, string abbreviation)
        {
            return matcher.MatchingIndicies(abbreviation);
        }
    }
}