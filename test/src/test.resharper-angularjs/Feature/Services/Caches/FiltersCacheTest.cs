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

using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    [TestFixture]
    public class FiltersCacheTest : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath { get { return "Caches"; } }

        protected override void DoTest(IProject testProject)
        {
            Solution.GetPsiServices().Files.CommitAllDocuments();

            var ngdocCache = Solution.GetComponent<AngularJsCache>();

            ExecuteWithGold(tw =>
            {
                foreach (var filter in ngdocCache.Filters.OrderBy(f => f.Name))
                {
                    tw.WriteLine("{0} {1}", filter.Name, filter.Offset);
                }
            });
        }

        [Test]
        public void CacheDefaultAngularFilters12()
        {
            DoTestSolution(@"..\angular.1.2.28.js");
        }

        [Test]
        public void CacheDefaultAngularFilters13()
        {
            DoTestSolution(@"..\angular.1.3.15.js");
        }

        [Test]
        public void CacheDefaultAngularFilters14()
        {
            DoTestSolution(@"..\angular.1.4.0.js");
        }
    }
}