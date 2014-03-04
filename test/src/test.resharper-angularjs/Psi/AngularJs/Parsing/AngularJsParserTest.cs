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

using System;
using System.IO;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.Shared;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestShell.Infra;
using JetBrains.Text;
using JetBrains.Util.Logging;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing
{
    // TODO: Should be able to use ParserTestBase with the injected PSI. Looks like
    // it outputs all PSI files in a file, so, create a HTML file with angular expressions
    [TestFileExtension(JavaScriptProjectFileType.JS_EXTENSION)]
    public class AngularJsParserTest : BaseTest
    {
        protected override string RelativeTestDataPath
        {
            get { return @"parsing"; }
        }

        [TestCase("array")]
        [TestCase("comparison")]
        [TestCase("filters")]
        [TestCase("in")]
        [TestCase("logical")]
        [TestCase("object")]
        [TestCase("repeat_expressions")]
        [TestCase("statements")]
        [TestCase("string")]
        [TestCase("ternary")]
        public void TestParser(string testName)
        {
            // TODO: filters is wrong, but good for a first attempt
            DoOneTest(testName);
        }

        protected override void DoOneTest(string testName)
        {
            Logger.Catch(() =>
            {
                testName = testName + ".js";
                var testFile = GetTestDataFilePath2(testName);
                ExecuteWithGold(testName, writer =>
                {
                    var expressions = File.ReadAllLines(testFile.FullPath);
                    foreach (var angularJsExpression in expressions)
                    {
                        var buffer = new StringBuffer(angularJsExpression);
                        var lexer = new AngularJsLexerGenerated(buffer);
                        var parser = new AngularJsParser(lexer);


                        try
                        {
                            var parsedFile = parser.ParseFile();

                            Assert.NotNull(parsedFile);

                            writer.WriteLine("Expression: «{0}»", angularJsExpression);
                            writer.WriteLine("Language: {0}", parsedFile.Language);
                            DebugUtil.DumpPsi(writer, parsedFile);
                            writer.WriteLine();
                            var containingFile = parsedFile.GetContainingFile();
                            if (containingFile != null)
                            {
                                var rangeTranslator =
                                    ((IFileImpl) containingFile).SecondaryRangeTranslator as
                                        RangeTranslatorWithGeneratedRangeMap;
                                if (rangeTranslator != null)
                                    WriteCommentedText(writer, "//", rangeTranslator.Dump(containingFile));
                            }
                        }
                        catch (Exception e)
                        {
                            writer.WriteLine(e);
                        }
                    }
                });
            });
        }

        private static void WriteCommentedText(TextWriter w, string commentPrefix, string text)
        {
            int start = 0, end;
            while ((end = text.IndexOf("\n", start, StringComparison.Ordinal)) != -1)
            {
                int off = (text[end - 1] == '\r') ? 1 : 0;
                w.WriteLine("{0} {1}", commentPrefix, text.Substring(start, end - start - off));
                start = end + 1;
            }
            w.WriteLine("{0} {1}", commentPrefix, text.Substring(start));
        }
    }
}