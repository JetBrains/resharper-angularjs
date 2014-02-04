using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.JavaScript.Stages;
using JetBrains.ReSharper.Daemon.Stages.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;

namespace JetBrains.ReSharper.Plugins.AngularJS.Daemon
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