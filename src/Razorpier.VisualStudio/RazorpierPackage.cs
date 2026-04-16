using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Razorpier.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("Razorpier", "Formats the active Razor document with Razorpier.", "1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(PackageGuids.PackageString)]
public sealed class RazorpierPackage : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await FormatRazorDocumentCommand.InitializeAsync(this);
    }
}
