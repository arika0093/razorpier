using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Razorpier.VisualStudio;

internal sealed class FormatRazorDocumentCommand
{
    public const int CommandId = 0x0100;
    public static readonly Guid CommandSet = new Guid(PackageGuids.CommandSetString);

    private readonly AsyncPackage package;

    private FormatRazorDocumentCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        this.package = package;
        var commandId = new CommandID(CommandSet, CommandId);
        var menuItem = new MenuCommand((_, _) =>
        {
            _ = package.JoinableTaskFactory.RunAsync(ExecuteAsync);
        }, commandId);
        commandService.AddCommand(menuItem);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        Assumes.Present(commandService);
        _ = new FormatRazorDocumentCommand(package, commandService);
    }

    private async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
        if (dte == null || !ActiveDocumentFormatter.TryFormat(dte.ActiveDocument))
        {
            VsShellUtilities.ShowMessageBox(
                package,
                "Open a .razor document to format it with Razorpier.",
                "Razorpier",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
