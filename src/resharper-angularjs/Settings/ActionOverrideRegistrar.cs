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