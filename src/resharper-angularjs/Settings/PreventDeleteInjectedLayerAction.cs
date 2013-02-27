#region license
// Copyright 2013 JetBrains s.r.o.
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