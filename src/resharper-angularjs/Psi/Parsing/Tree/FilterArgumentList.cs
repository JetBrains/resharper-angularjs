using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Parsing.Tree
{
    internal class FilterArgumentList : JavaScriptCompositeElement
    {
        public override NodeType NodeType
        {
            get { return AngularJsElementType.FILTER_ARGUMENT_LIST; }
        }

        // TODO: Make the children available via properties

    }
}