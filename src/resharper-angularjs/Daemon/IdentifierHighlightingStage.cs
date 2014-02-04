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
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.JavaScript.Stages.SmartResolver;
using JetBrains.ReSharper.Daemon.Stages.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Daemon
{
    // NOTE: We need this because AngularJsLanguage derives from KnownLanguage, so we don't get
    // to take advantage of JavaScript's IdentifierHighlightingStage. This is how the JsonLanguage
    // works, too, but I wonder if we derive from JavaScriptLanguage then would we get this for free?
    [DaemonStage(StagesBefore = new[] { typeof(SmartResolverStage) })]
    public class IdentifierHighlightingStage : AngularJsDaemonStageBase
    {
        private readonly ResolveHighlighterRegistrar resolveHighlighterRegistrar;

        public IdentifierHighlightingStage(ResolveHighlighterRegistrar resolveHighlighterRegistrar)
        {
            this.resolveHighlighterRegistrar = resolveHighlighterRegistrar;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, IJavaScriptFile file)
        {
            return new AngularJsIdentifierHighlighterProcess(resolveHighlighterRegistrar, process, settings, file, processKind);
        }

        public override ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile, IContextBoundSettingsStore settings)
        {
            return IsSupported(sourceFile) ? ErrorStripeRequest.STRIPE_AND_ERRORS : ErrorStripeRequest.NONE;
        }
    }
}