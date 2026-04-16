using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Razorpier.Core;

namespace Razorpier.MSBuild;

public sealed class RazorpierFormatTask : Task
{
    [Required]
    public ITaskItem[] Files { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in Files ?? Array.Empty<ITaskItem>())
        {
            var fullPath = Path.GetFullPath(item.ItemSpec);
            if (!seen.Add(fullPath) || !File.Exists(fullPath) || !fullPath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var original = File.ReadAllText(fullPath);
            var formatted = RazorFormatter.Format(original);
            if (string.Equals(original, formatted, StringComparison.Ordinal))
            {
                continue;
            }

            File.WriteAllText(fullPath, formatted);
            Log.LogMessage(MessageImportance.Normal, $"Formatted {fullPath}");
        }

        return !Log.HasLoggedErrors;
    }
}
