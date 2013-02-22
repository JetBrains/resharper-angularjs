using JetBrains.Application;
using JetBrains.Application.Settings.Storage.DefaultFileStorages;
using JetBrains.Application.Settings.UserInterface.FileInjectedLayers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    // For such a small piece of code, there's a lot to say about it. This will add the
    // angularjs-templates.dotSettings to the This Computer layer. Pros and Cons:
    //
    // Con #1: If the plugin is disabled, the templates will still be loaded. The file can
    //         be unchecked, but it's still a separate step
    // Con #2: If the plugin is uninstalled, and the dotSettings file deleted, ReSharper will
    //         recreate it rather than removing it
    // Con #3: Since the file is registered in the global settings, which is roaming, the
    //         file will be recreated on machines that don't have the plugin installed
    //
    // Pro #1: The file and enabled state are persistent, so ReSharper remembers if you've
    //         unchecked it
    // Pro #2: Unless specifically selected, changes to the templates are saved into the
    //         This Copmuter layer (%APPDATA%\JetBrains\ReSharper\vAny\GlobalSettings.dotSettings)
    //         rather than changing the file in the plugin folder
    // Pro #3: Changes saved to the This Computer layer are not applied if the original file
    //         is removed (this is good, you can customise the plugin's templates, but uninstalling
    //         the plugin doesn't leave you with only the customised templates)
    //
    [ShellComponent]
    public class TemplatesLoader
    {
        public TemplatesLoader(GlobalSettings globalSettings, FileInjectedLayers fileInjectedLayers)
        {
            var host = globalSettings.ProductGlobalLayerId;
            var settingsFile = GetSettingsFile();

            // Be careful. This needs to be the same case as the filename in the injected layer, and
            // that is the case of the file on the disk, not the casing we give in the call to
            // InjectLayer
            if (settingsFile.ExistsFile && !fileInjectedLayers.IsLayerInjected(host, settingsFile))
                fileInjectedLayers.InjectLayer(host, settingsFile);
        }

        private FileSystemPath GetSettingsFile()
        {
            return new FileSystemPath(GetType().Assembly.Location).Directory.Combine("angularjs-templates.dotSettings");
        }
    }
}