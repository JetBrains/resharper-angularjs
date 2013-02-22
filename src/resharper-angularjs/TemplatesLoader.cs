using JetBrains.Application;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings.Storage;
using JetBrains.Application.Settings.Storage.DefaultFileStorages;
using JetBrains.Application.Settings.Storage.Persistence;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.Application.Settings.UserInterface;
using JetBrains.Application.Settings.UserInterface.FileInjectedLayers;
using JetBrains.DataFlow;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    // For such a small piece of code, there's a lot to say about it. This will add the
    // angularjs-templates.dotSettings to the This Computer layer. Pros and Cons:
    //
    // Con #1: Can reset the settings, leading to an empty file
    // Con #2: Remove is still in the context menu, but it does nothing (because I pass an empty OnDelete action)
    // 
    // Pro #1: If the plugin is disabled, then doesn't get loaded. (I don't think plugins get disabled
    //         on the fly)
    // Pro #2: Customisations are persistent - remembers checked status
    // Pro #3: Disappears when plugin is uninstalled
    // Pro #4: The customisations are in the roaming global file, but the link isn't. No file gets
    //         created if the plugin isn't installed
    // Pro #5: Changes to the templates are saved to This Computer by default, rather than this file
    //
    // Could have a readonly xml file so changes (such as resetting or saving to) have no effect.
    // Would be nicer if we could then flag the storage/mount point as read only, so no-one would
    // try. Perhaps a suggestion for 8.0?
    // And if we're using a readonly file, we could just as easily make it a resource
    //
    // Could we augment the FileSettingsStorageBehaviour to prevent the file getting trashed?
    //
    // How about displaying a message box in the delete function, to say it can't be deleted?
    [ShellComponent]
    public class TemplatesLoader
    {
        // This value just needs to be unique so that any customisations to the layer are persistent
        private const string AngularJsInjectedLayerId = "resharper-angularjs";

        public TemplatesLoader(Lifetime lifetime, GlobalSettings globalSettings, UserInjectedSettingsLayers userInjectedSettingsLayers,
            IThreading threading, IFileSystemTracker filetracker, FileSettingsStorageBehavior behavior)
        {
            var path = GetSettingsFile();

            var pathAsProperty = new Property<FileSystemPath>(lifetime, "InjectedFileStoragePath", path);
            var serialization = new XmlFileSettingsStorage(lifetime, "angularjs-templates::" + path.FullPath.QuoteIfNeeded(), pathAsProperty,
                SettingsStoreSerializationToXmlDiskFile.SavingEmptyContent.DeleteFile, threading, filetracker, behavior);
            var persistentId = new UserInjectedSettingsLayers.InjectedLayerPersistentIdentity(AngularJsInjectedLayerId);
            var descriptor = new UserInjectedSettingsLayers.UserInjectedLayerDescriptor(lifetime, globalSettings.ProductGlobalLayerId,
                persistentId, serialization.Storage, SettingsStorageMountPoint.MountPath.Default, () => { });
            descriptor.InitialMetadata.Set(UserFriendlySettingsLayers.DisplayName, "angularjs-templates");
            descriptor.InitialMetadata.Set(UserFriendlySettingsLayers.Origin, "Angular JS templates");

            userInjectedSettingsLayers.RegisterUserInjectedLayer(lifetime, descriptor);
        }

        private FileSystemPath GetSettingsFile()
        {
            return new FileSystemPath(GetType().Assembly.Location).Directory.Combine("angularjs-templates.dotSettings");
        }
    }
}