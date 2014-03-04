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
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing
{
    [UsedImplicitly]
    public class AngularJsTokenType : JavaScriptTokenType
    {
        private class UndefinedKeywordNodeType : KeywordTokenNodeType
        {
            public UndefinedKeywordNodeType(int index)
                : base("UNDEFINED_KEYWORD", "undefined", index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new UndefinedKeywordTokenElement(this);
            }
        }

        private class UndefinedKeywordTokenElement : FixedTokenElement
        {
            public UndefinedKeywordTokenElement(UndefinedKeywordNodeType tokenNodeType)
                : base(tokenNodeType)
            {
            }
        }

        public static readonly TokenNodeType UNDEFINED_KEYWORD = new UndefinedKeywordNodeType(7000);


        private class TrackByKeywordNodeType : KeywordTokenNodeType
        {
            public TrackByKeywordNodeType(int index)
                : base("TRACK_BY_KEYWORD", "track by", index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                return new TrackByKeywordTokenElement(this);
            }
        }

        private class TrackByKeywordTokenElement : FixedTokenElement
        {
            public TrackByKeywordTokenElement(TrackByKeywordNodeType tokenNodeType)
                : base(tokenNodeType)
            {
            }
        }

        public static readonly TokenNodeType TRACK_BY_KEYWORD = new TrackByKeywordNodeType(7001);
    }
}