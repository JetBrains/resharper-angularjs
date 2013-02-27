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
using System;
using JetBrains.ActionManagement;
using JetBrains.ActionManagement.Impl;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.UI.Settings;

namespace JetBrains.ReSharper.Plugins.AngularJS.Settings
{
    [ShellComponent]
    public class ActionOverrideRegistrar
    {
        public ActionOverrideRegistrar(Lifetime lifetime, IActionManager actionManager)
        {
            // Add a new handler to the delete and reset layers actions. We'll get called before
            // the system, so we can intercept and allow/disallow on different conditions.
            // We don't need to override the reset all action, since that won't wipe existing
            // files, but will remove any mount points. We add back in on next restart, anyway.
            //
            // TODO: Can we do this declaratively?
            // A quick look suggests the action loader will just add handlers to existing actions,
            // and that would prevent the need for this (admittedly small) class
            AddOverridingHandler(lifetime, actionManager, typeof (DeleteInjectedLayerAction), new PreventDeleteInjectedLayerAction());
            AddOverridingHandler(lifetime, actionManager, typeof (ResetSelectedSettingsLayersAction), new PreventResetSelectedSettingsLayerAction());
        }

        private static void AddOverridingHandler(Lifetime lifetime, IActionManager actionManager, Type actionType, IActionHandler actionHandler)
        {
            var action = ActionInfo.GetActionFromActionHandler(actionType, actionManager);
            action.AddHandler(lifetime, actionHandler);
        }
    }
}