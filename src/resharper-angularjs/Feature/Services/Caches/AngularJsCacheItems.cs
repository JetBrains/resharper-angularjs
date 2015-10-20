using System.Collections.Generic;
using System.Linq;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    // All the cached items in a file, directives + filters
    public class AngularJsCacheItems
    {
        public static readonly IUnsafeMarshaller<AngularJsCacheItems> Marshaller =
            new UniversalMarshaller<AngularJsCacheItems>(Read, Write);

        private readonly IList<Directive> directives;
        private readonly IList<Filter> filters;

        public AngularJsCacheItems(IList<Directive> directives, IList<Filter> filters)
        {
            this.directives = directives;
            this.filters = filters;
        }

        public IEnumerable<Directive> Directives { get { return directives; } }
        public IEnumerable<Filter> Filters { get { return filters; } }

        public bool IsEmpty
        {
            get { return directives.Count == 0 && filters.Count == 0; }
        }

        private static AngularJsCacheItems Read(UnsafeReader reader)
        {
            var directives = reader.ReadCollection(Directive.Read, count => new List<Directive>(count));
            var filters = reader.ReadCollection(Filter.Read, count => new List<Filter>(count));
            return new AngularJsCacheItems(directives, filters);
        }

        private static void Write(UnsafeWriter writer, AngularJsCacheItems value)
        {
            writer.Write<Directive, ICollection<Directive>>((w, directive) => directive.Write(w), value.Directives.ToList());
            writer.Write<Filter, ICollection<Filter>>((w, filter) => filter.Write(w), value.Filters.ToList());
        }
    }
}