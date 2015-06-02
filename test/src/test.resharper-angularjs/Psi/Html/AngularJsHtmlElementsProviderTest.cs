#region license
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

using System;
using System.IO;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    [TestFixture]
    public class AngularJsHtmlElementsProviderTest : BaseTestWithSingleProject
    {
        private const string AngularJs = @"..\..\..\angular.js";

        protected override string RelativeTestDataPath { get { return @"Psi\Html\ElementsProvider"; } }

        [Test]
        public void GetCommonAttributesSymbolTable()
        {
            DoTest(AngularJs, (writer, provider) =>
            {
                var symbolTable = provider.GetCommonAttributesSymbolTable();
                var symbols = symbolTable.GetAllSymbolInfos().OrderBy(s => s.ShortName).ToList();
                writer.WriteLine("Symbols: {0}", symbols.Count);
                foreach (var symbolInfo in symbols)
                {
                    var element = symbolInfo.GetDeclaredElement() as IHtmlAttributeDeclaredElement;
                    Assert.IsNotNull(element);
                    writer.WriteLine("{0} ({1})", symbolInfo.ShortName, element.ValueType.Name);
                }
            });
        }

        [Test]
        public void GetAllAttributesSymbolTable()
        {
            DoTest(AngularJs, (writer, provider) =>
            {
                var symbolTable = provider.GetAllAttributesSymbolTable();
                var symbols = symbolTable.GetAllSymbolInfos().OrderBy(s => s.ShortName).ToList();
                writer.WriteLine("Symbols: {0}", symbols.Count);
                foreach (var symbolInfo in symbols)
                {
                    var element = symbolInfo.GetDeclaredElement() as IHtmlAttributeDeclaredElement;
                    Assert.IsNotNull(element);
                    writer.WriteLine("{0} ({1})", symbolInfo.ShortName, element.ValueType.Name);
                }
            });
        }

        [Test]
        public void GetAllTagsSymbolTable()
        {
            DoTest(AngularJs, (writer, provider) =>
            {
                var symbolTable = provider.GetAllTagsSymbolTable();
                var symbols = symbolTable.GetAllSymbolInfos().OrderBy(s => s.ShortName).ToList();
                writer.WriteLine("Symbols: {0}", symbols.Count);
                foreach (var symbolInfo in symbols)
                {
                    // TODO: Dump more stuff, but only once we support tags
                    var element = symbolInfo.GetDeclaredElement() as IHtmlTagDeclaredElement;
                    Assert.IsNotNull(element);
                    writer.WriteLine("{0} {1}", symbolInfo.ShortName, symbolInfo.Level);
                }
            });
        }

        [Test]
        public void GetAttributeDeclaredElementForAttributeName()
        {
            WithSingleProject(AngularJs, (lifetime, solution, project) =>
            {
                var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                var attributes = provider.GetAttributes("ng-app");
                Assert.IsNotNull(attributes);
                var attributesList = attributes.ToList();
                Assert.AreEqual(1, attributesList.Count);
                Assert.AreEqual("ng-app", attributesList[0].ShortName);
                Assert.AreEqual("CDATA", attributesList[0].ValueType.Name);
            });
        }

        [Test]
        public void GetAttributeInfo()
        {
            WithSingleProject(AngularJs, (lifetime, solution, arg3) =>
            {
                ExecuteWithGold(tw =>
                {
                    var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                    var cache = solution.GetComponent<IHtmlDeclaredElementsCache>();
                    var tag = cache.GetTag("body", null);
                    Assert.IsNotNull(tag);
                    var attributeInfos = provider.GetAttributeInfos(null, tag, true).ToList();
                    tw.WriteLine("Attributes: {0}", attributeInfos.Count);
                    foreach (var attributeInfo in attributeInfos.OrderBy(a => a.AttributeDeclaredElement.ShortName))
                    {
                        tw.WriteLine("{0} ({1} - {2})", attributeInfo.AttributeDeclaredElement.ShortName, attributeInfo.DefaultValueType, attributeInfo.DefaultValue);
                    }
                });
            });
        }

        [Test]
        public void ReturnsNoEvents()
        {
            WithSingleProject(AngularJs, (lifetime, solution, project) =>
            {
                var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                var eventsSymbolTable = provider.GetEventsSymbolTable();
                Assert.IsTrue(eventsSymbolTable.GetAllSymbolInfos().Count == 0);
            });
        }

        [Test]
        public void ReturnsNoLegacyEvents()
        {
            WithSingleProject(AngularJs, (lifetime, solution, arg3) =>
            {
                var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                var eventsSymbolTable = provider.GetLegacyEventsSymbolTable();
                Assert.IsTrue(eventsSymbolTable.GetAllSymbolInfos().Count == 0);
            });
        }

        private void DoTest(string filename, Action<TextWriter, AngularJsHtmlElementsProvider> action)
        {
            WithSingleProject(filename, (l, s, p) =>
            {
                ExecuteWithGold(tw =>
                {
                    var provider = s.GetComponent<AngularJsHtmlElementsProvider>();
                    action(tw, provider);
                });
            });
        }
    }
}