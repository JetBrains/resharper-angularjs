using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Html;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.Html;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    // Removes the regular HTML declared elements that match AngularJS items, e.g. the
    // `a` element is implemented by 
    [Language(typeof (HtmlLanguage))]
    public class OverriddenAngularJsItemsRemover : ItemsProviderOfSpecificContext<HtmlCodeCompletionContext>
    {
        protected override void TransformItems(HtmlCodeCompletionContext context, IItemsCollector collector)
        {
            var angularItems = (from item in collector.Items
                let unwrappedItem = GetDeclaredElementLookupItem(item)
                where
                    unwrappedItem != null &&
                    unwrappedItem.GetPreferredDeclaredElement().Element is IAngularJsDeclaredElement
                select item).ToHashSet();
            var angularItemNames = angularItems.ToHashSet(i => i.DisplayName.Text);
            var toRemove = from item in collector.Items
                where !angularItems.Contains(item) && angularItemNames.Contains(item.DisplayName.Text)
                select item;

            foreach (var item in toRemove.ToList())
                collector.Remove(item);
        }

        private static IDeclaredElementLookupItem GetDeclaredElementLookupItem(ILookupItem item)
        {
            var wrapped = item as IWrappedLookupItem;
            if (wrapped != null)
                return wrapped.Item as IDeclaredElementLookupItem;
            return item as IDeclaredElementLookupItem;
        }
    }
}