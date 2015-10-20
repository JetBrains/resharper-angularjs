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
        protected override string RelativeTestDataPath { get { return @"Psi\Html\ElementsProvider"; } }

        private string GetAngularJs(Version version)
        {
            return AngularJsTestVersions.GetAngularJsVersion(BaseTestDataPath, version);
        }

        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetCommonAttributesSymbolTable(Version version)
        {
            DoTest(GetAngularJs(version), version, (writer, provider) =>
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
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetAllAttributesSymbolTable(Version version)
        {
            DoTest(GetAngularJs(version), version, (writer, provider) =>
            {
                var symbolTable = provider.GetAllAttributesSymbolTable();
                var symbols = symbolTable.GetAllSymbolInfos().OrderBy(s => s.ShortName).ToList();
                writer.WriteLine("Symbols: {0}", symbols.Count);
                foreach (var symbolInfo in symbols)
                {
                    var element = symbolInfo.GetDeclaredElement() as IHtmlAttributeDeclaredElement;
                    Assert.IsNotNull(element);
                    writer.WriteLine("{0} {1} ({2})", symbolInfo.ShortName,
                        element.Tag == null ? "(ANY)" : element.Tag.ShortName, element.ValueType.Name);
                }
            });
        }

        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetAllTagsSymbolTable(Version version)
        {
            DoTest(GetAngularJs(version), version, (writer, provider) =>
            {
                var symbolTable = provider.GetAllTagsSymbolTable();
                var symbols = symbolTable.GetAllSymbolInfos().OrderBy(s => s.ShortName).ToList();
                writer.WriteLine("Symbols: {0}", symbols.Count);
                foreach (var symbolInfo in symbols)
                {
                    var tag = symbolInfo.GetDeclaredElement() as IHtmlTagDeclaredElement;
                    Assert.IsNotNull(tag);
                    writer.WriteLine("{0} {1}", symbolInfo.ShortName, symbolInfo.Level);
                    writer.WriteLine("\tOwn attributes: {0}", tag.OwnAttributes.Count());
                    foreach (var attribute in tag.OwnAttributes.OrderBy(a => a.AttributeDeclaredElement.ShortName))
                        writer.WriteLine("\t\t{0}", attribute.AttributeDeclaredElement.ShortName);
                    writer.WriteLine("\tInherited attributes: {0}", tag.InheritedAttributes.Count());
                    foreach (var attribute in tag.InheritedAttributes.OrderBy(a => a.AttributeDeclaredElement.ShortName))
                        writer.WriteLine("\t\t{0}", attribute.AttributeDeclaredElement.ShortName);
                }
            });
        }

        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetTagForAngularTag(Version version)
        {
            DoTest(GetAngularJs(version), version, (writer, provider) =>
            {
                var tag = provider.GetTag("ng-include");
                Assert.IsNotNull(tag);
                writer.WriteLine("{0}", tag.ShortName);
                writer.WriteLine("Own attributes: {0}", tag.OwnAttributes.Count());
                foreach (var attribute in tag.OwnAttributes.OrderBy(a => a.AttributeDeclaredElement.ShortName))
                    writer.WriteLine("\t{0}", attribute.AttributeDeclaredElement.ShortName);
                writer.WriteLine("Inherited attributes: {0}", tag.InheritedAttributes.Count());
                foreach (var attribute in tag.InheritedAttributes.OrderBy(a => a.AttributeDeclaredElement.ShortName))
                    writer.WriteLine("\t{0}", attribute.AttributeDeclaredElement.ShortName);
            });
        }

        [Test]
        [TestCaseSource(typeof (AngularJsTestVersions), "Versions")]
        public void GetStandardHtmlTag(Version version)
        {
            WithSingleProject(GetAngularJs(version), (lifetime, solution, project) =>
            {
                // This tag is declared in Angular, but we keep the standard HTML tag
                // as the declared element, and just add our attributes
                var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                var tag = provider.GetTag("a");
                Assert.IsNull(tag);
            });
        }

        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetAttributeDeclaredElementForAttributeName(Version version)
        {
            WithSingleProject(GetAngularJs(version), (lifetime, solution, project) =>
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

        // TODO: Does this test what it says it tests?
        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetAttributeInfoForCommonAttributes(Version version)
        {
            WithSingleProject(GetAngularJs(version), (lifetime, solution, arg3) =>
            {
                ExecuteWithGold(TestMethodName + version.Major + version.Minor, tw =>
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
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void GetAttributeInfoForSpecificTag(Version version)
        {
            WithSingleProject(GetAngularJs(version), (lifetime, solution, arg3) =>
            {
                ExecuteWithGold(TestMethodName + version.Major + version.Minor, tw =>
                {
                    var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                    var cache = solution.GetComponent<IHtmlDeclaredElementsCache>();
                    var tag = cache.GetTag("input", null);
                    Assert.IsNotNull(tag);
                    var attributeInfos = provider.GetAttributeInfos(null, tag, true).ToList();
                    tw.WriteLine("Attributes: {0}", attributeInfos.Count);
                    foreach (var attributeInfo in attributeInfos.OrderBy(a => a.AttributeDeclaredElement.ShortName))
                    {
                        var attributeTag = attributeInfo.AttributeDeclaredElement.Tag;
                        tw.WriteLine("{0} ({1} - {2})", attributeInfo.AttributeDeclaredElement.ShortName,
                            attributeInfo.DefaultValueType, attributeInfo.DefaultValue);
                    }
                });
            });
        }

        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void ReturnsNoEvents(Version version)
        {
            WithSingleProject(GetAngularJs(version), (lifetime, solution, project) =>
            {
                var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                var eventsSymbolTable = provider.GetEventsSymbolTable();
                Assert.IsTrue(eventsSymbolTable.GetAllSymbolInfos().Count == 0);
            });
        }

        [Test]
        [TestCaseSource(typeof(AngularJsTestVersions), "Versions")]
        public void ReturnsNoLegacyEvents(Version version)
        {
            WithSingleProject(GetAngularJs(version), (lifetime, solution, arg3) =>
            {
                var provider = solution.GetComponent<AngularJsHtmlElementsProvider>();
                var eventsSymbolTable = provider.GetLegacyEventsSymbolTable();
                Assert.IsTrue(eventsSymbolTable.GetAllSymbolInfos().Count == 0);
            });
        }

        private void DoTest(string filename, Version version, Action<TextWriter, AngularJsHtmlElementsProvider> action)
        {
            WithSingleProject(filename, (l, s, p) =>
            {
                ExecuteWithGold(AngularJsTestVersions.GetTestMethodName(TestMethodName, version), tw =>
                {
                    var provider = s.GetComponent<AngularJsHtmlElementsProvider>();
                    action(tw, provider);
                });
            });
        }
    }
}