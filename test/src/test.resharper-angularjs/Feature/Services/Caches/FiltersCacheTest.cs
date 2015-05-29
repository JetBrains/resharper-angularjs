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
        public void CacheDefaultAngularFilters()
        {
            DoTestSolution("angular.js");
        }
    }
}