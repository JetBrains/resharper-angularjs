using System.Collections.Generic;
using System.Linq;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class AngularJsCacheItemsBuilder
    {
        private readonly IDictionary<string, Directive> directives = new Dictionary<string, Directive>();
        private readonly IDictionary<string, Filter> filters = new Dictionary<string, Filter>();

        public AngularJsCacheItems Build()
        {
            return new AngularJsCacheItems(directives.Values.ToList(), filters.Values.ToList());
        }

        public void Add(Directive directive)
        {
            // TODO: Should there be rules for precedence, merging?
            if (!directives.ContainsKey(directive.Name))
                directives.Add(directive.Name, directive);
        }

        public void Add(Filter filter)
        {
            // TODO: Should there be rules for precedence, merging?
            if (!filters.ContainsKey(filter.Name))
                filters.Add(filter.Name, filter);
        }
    }
}