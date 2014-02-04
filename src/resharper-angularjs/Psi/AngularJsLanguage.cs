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

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi
{
    [LanguageDefinition(Name)]
    public class AngularJsLanguage : KnownLanguage
    {
        public new const string Name = "AngularJS";

        [CanBeNull, UsedImplicitly]
        public static readonly AngularJsLanguage Instance;

        private AngularJsLanguage() : base(Name, "AngularJS")
        {
        }

        protected AngularJsLanguage([NotNull] string name)
            : base(name)
        {
        }

        protected AngularJsLanguage([NotNull] string name, [NotNull] string presentableName)
            : base(name, presentableName)
        {
        }
    }
}