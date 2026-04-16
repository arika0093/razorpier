using Razorpier.Core;

if (args.Length == 0 || args.Any(static arg => arg is "-h" or "--help"))
{
    Console.WriteLine(
        """
        razorpier <file1.razor> [file2.razor ...] [--check]
        razorpier --stdin

          --check   Verify formatting without writing files.
          --stdin   Read Razor content from standard input and write formatted output to standard output.
        """);
    return 0;
}

if (args.Contains("--stdin", StringComparer.Ordinal))
{
    var input = await Console.In.ReadToEndAsync();
    var formatted = RazorFormatter.Format(input);
    await Console.Out.WriteAsync(formatted);
    return 0;
}

var checkOnly = args.Contains("--check", StringComparer.Ordinal);
var files = args
    .Where(static arg => arg != "--check")
    .Select(Path.GetFullPath)
    .ToArray();

if (files.Length == 0)
{
    Console.Error.WriteLine("No Razor files were specified.");
    return 1;
}

var exitCode = 0;

foreach (var file in files)
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"File not found: {file}");
        exitCode = 1;
        continue;
    }

    if (!file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Unsupported file type: {file}");
        exitCode = 1;
        continue;
    }

    var original = await File.ReadAllTextAsync(file);
    var formatted = RazorFormatter.Format(original);

    if (checkOnly)
    {
        if (!string.Equals(original, formatted, StringComparison.Ordinal))
        {
            Console.WriteLine(file);
            exitCode = 1;
        }

        continue;
    }

    if (!string.Equals(original, formatted, StringComparison.Ordinal))
    {
        await File.WriteAllTextAsync(file, formatted);
        Console.WriteLine($"Formatted {file}");
    }
}

return exitCode;
