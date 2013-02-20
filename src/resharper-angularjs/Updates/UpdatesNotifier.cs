/*
 * Copyright 2013 JetBrains s.r.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.UI.Updates;
using JetBrains.VSIntegration.Updates;
using System.Linq;

namespace JetBrains.ReSharper.Plugins.AngularJS.Updates
{
    [ShellComponent]
    public class UpdatesNotifier
    {
        public UpdatesNotifier(Lifetime lifetime, UpdatesManager updatesManager)
        {
            var uri = new Uri("https://raw.github.com/JetBrains/resharper-angularjs/master/updates.xslt");
            var category = updatesManager.Categories.AddOrActivate("AngularJS", uri);
            category.CustomizeLocalEnvironmentInfo.Advise(lifetime, args =>
                {
                    // We can customise the local environment info that the xslt will be applied to
                    // It should be an instance of UpdateLocalEnvironmentInfo, bail out early if it's
                    // not. The only reason it wouldn't be is if someone has got hold of the "AngularJS"
                    // category and subscribed to the CustomizeLocalEnvironmentInfo signal. Unlikely.
                    if (!(args.Out is UpdateLocalEnvironmentInfoVs))
                        return;

                    // Set the data the xslt will be applied against. Pass in the current environment,
                    // in case we ever need it, but really, we only care about the current version
                    args.Out = new PluginLocalEnvironmentInfo
                        {
                            LocalEnvironment = args.Out as UpdateLocalEnvironmentInfoVs,
                            PluginVersion = new UpdateLocalEnvironmentInfo.VersionSubInfo(GetThisVersion())
                        };
                });

            RemoveStaleUpdateNotification(category);
        }

        private static Version GetThisVersion()
        {
            var assembly = typeof(UpdatesNotifier).Assembly;
            return assembly.GetName().Version;
        }

        private static void RemoveStaleUpdateNotification(UpdatesCategory category)
        {
            // ReSharper downloads and evaluates the xslt on a regular basis (every 24 hours),
            // but doesn't re-evaluate it after an install (it doesn't know when something is
            // installed!) so if there's a reminder to download this or an older version, remove it
            var thisVersion = GetThisVersion();
            var updateInfo = category.UpdateInfos.FirstOrDefault(container => new Version(container.Data.ProductVersion) <= thisVersion);
            if (updateInfo != null)
                category.UpdateInfos.Remove(updateInfo);
        }
    }
}