using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Razorpier.Core;

namespace Razorpier.VisualStudio;

internal static class ActiveDocumentFormatter
{
    public static bool TryFormat(Document? document)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (document == null || !document.FullName.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var textDocument = document.Object("TextDocument") as TextDocument;
        if (textDocument == null)
        {
            return false;
        }

        var start = textDocument.StartPoint.CreateEditPoint();
        var end = textDocument.EndPoint;
        var original = start.GetText(end);
        var formatted = RazorFormatter.Format(original);
        if (string.Equals(original, formatted, StringComparison.Ordinal))
        {
            return true;
        }

        start.ReplaceText(end, formatted, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        document.Save();
        return true;
    }
}
