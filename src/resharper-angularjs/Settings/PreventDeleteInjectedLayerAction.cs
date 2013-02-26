using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings.UserInterface;
using JetBrains.Util;
using DataConstants = JetBrains.UI.Settings.DataConstants;

namespace JetBrains.ReSharper.Plugins.AngularJS.Settings
{
    // An action handler that adds a handler to the "DeleteInjectedLayer" action.
    // It will prevent the user deleting our action handler
    public class PreventDeleteInjectedLayerAction : IActionHandler
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            var layers = context.GetData(DataConstants.SelectedUserFriendlySettingsLayers);
            if (layers == null || layers.IsEmpty())
                return false;

            // Action is disabled if *all* layers have deletion blocked
            return layers.Any(CanDelete) && nextUpdate();
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            // Just let the "real" handler do its stuff. We either won't be called
            // (because we returned false to Update) or there is more than one layer
            // being removed. Since we do nothing in response to a delete request,
            // it doesn't matter
            nextExecute();
        }

        private static bool CanDelete(UserFriendlySettingsLayer.Identity layer)
        {
            if (layer.CharacteristicMount == null)
                return false;

            // Look for our metadata id to see if we should prevent deletion
            var metadata = layer.CharacteristicMount.Metadata;
            return !metadata.TryGet(TemplatesLoader.PreventDeletion);
        }
    }
}