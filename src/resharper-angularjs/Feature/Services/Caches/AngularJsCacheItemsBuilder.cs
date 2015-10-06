using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class AngularJsCacheItemsBuilder
    {
        private readonly IList<Directive> directives = new List<Directive>();
        private readonly IList<Filter> filters = new List<Filter>();

        public AngularJsCacheItems Build()
        {
            return new AngularJsCacheItems(directives, filters);
        }

        public void Add(Directive directive)
        {
            directives.Add(directive);
        }

        public void Add(Filter filter)
        {
            filters.Add(filter);
        }
    }
}