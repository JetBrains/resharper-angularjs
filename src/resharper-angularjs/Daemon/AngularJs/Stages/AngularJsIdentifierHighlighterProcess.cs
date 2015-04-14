#region license
// Copyright 2014 JetBrains s.r.o.
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

using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.JavaScript.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.JavaScript.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Daemon.AngularJs.Stages
{
    // We need to derive from IdentifierHighlighterProcess because IHP is used by
    // JavaScript's IdentifierHighlightingStage, and is keyed on that type. Our
    // IdentifierHighlightingStage therefore must return a different process
    // TODO: We might want to build our own, and always highlight identifiers, or at least keywords
    // TODO: Implement IJavaScriptIdentifierHighlighter for syntax highlighting
    // Need to mark with [Language(typeof(AngularJsLanguage))]
    public class AngularJsIdentifierHighlighterProcess : IdentifierHighlighterProcess
    {
        public AngularJsIdentifierHighlighterProcess(ResolveHighlighterRegistrar resolveHighlighterRegistrar, IDaemonProcess daemonProcess,
            IContextBoundSettingsStore settingsStore, IJavaScriptFile file, DaemonProcessKind processKind)
            : base(resolveHighlighterRegistrar, daemonProcess, settingsStore, file, processKind)
        {
        }
    }
}