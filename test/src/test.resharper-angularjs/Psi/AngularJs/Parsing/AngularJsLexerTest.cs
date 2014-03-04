#region license
// Copyright 2013 JetBrains s.r.o.
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

using System.IO;
using System.Text.RegularExpressions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.PsiTests.Lexing;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing
{
    [TestFileExtension(JavaScriptProjectFileType.JS_EXTENSION)]
    public class AngularJsLexerTest : LexerTestBase
    {
        protected override string RelativeTestDataPath
        {
            get { return @"lexing"; }
        }

        protected override ILexer CreateLexer(StreamReader sr)
        {
            var buffer = new StringBuffer(sr.ReadToEnd());
            return new AngularJsLexerGenerated(buffer);
        }

        protected override void WriteToken(TextWriter writer, ILexer lexer)
        {
            var tokenText = lexer.GetCurrTokenText();
            tokenText = Regex.Replace(tokenText, @"\p{Cc}", a => string.Format("[{0:X2}]", (byte)a.Value[0]));
            writer.WriteLine("{0} «{1}»", lexer.TokenType, tokenText);
        }

        [TestCase("expression")]
        [TestCase("identifier")]
        [TestCase("key_value")]
        [TestCase("keyword")]
        [TestCase("number")]
        [TestCase("string")]
        public void TestFiles(string file)
        {
            // Note that the "string" file for the IntelliJ plugin actually picks out invalid escape sequences
            DoOneTest(file);
        }
    }
}