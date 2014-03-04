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

using JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs
{
    [Language(typeof(AngularJsLanguage))]
    public class AngularJsLanguageService : JavaScriptBasedLanguageService
    {
        private readonly IJavaScriptCodeFormatter codeFormatter;

        public AngularJsLanguageService(PsiLanguageType psiLanguageType,
                                        IConstantValueService constantValueService,
                                        IJavaScriptCodeFormatter codeFormatter)
            : base(psiLanguageType, constantValueService)
        {
            this.codeFormatter = codeFormatter;
        }

        public override ILexerFactory GetPrimaryLexerFactory()
        {
            return new AngularJsLexerFactory();
        }

        public override ILexer CreateFilteringLexer(ILexer lexer)
        {
            return new JavaScriptLexer(lexer);
        }

        public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
        {
            return new AngularJsParser(lexer);
        }

        public override ILanguageCacheProvider CacheProvider
        {
            // TODO: What does ILanguageCacheProvider do?
            get { return null; }
        }

        public override bool IsCaseSensitive
        {
            get { return true; }
        }

        public override bool SupportTypeMemberCache
        {
            // TODO: What is the type member cache?
            get { return false; }
        }

        public override ITypePresenter TypePresenter
        {
            get { return DefaultTypePresenter.Instance; }
        }

        public override ICodeFormatter CodeFormatter
        {
            get { return codeFormatter; }
        }
    }

    public class AngularJsLexerFactory : ILexerFactory
    {
        public ILexer CreateLexer(IBuffer buffer)
        {
            return new AngularJsLexerGenerated(buffer);
        }
    }
}