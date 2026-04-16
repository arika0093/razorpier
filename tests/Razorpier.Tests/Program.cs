using System.Diagnostics;
using Razorpier.Core;

var failures = new List<string>();
var repoRoot = FindRepoRoot();

ShouldFormatTopLevelSections(failures);
ShouldIndentMarkupBlocks(failures);
await ShouldSupportCliStandardInputAsync(repoRoot, failures);
await ShouldFormatFixtureThroughMsBuildTargetAsync(repoRoot, failures);

if (failures.Count == 0)
{
    Console.WriteLine("All Razorpier formatter tests passed.");
    return 0;
}

foreach (var failure in failures)
{
    Console.Error.WriteLine(failure);
}

return 1;

static void ShouldFormatTopLevelSections(List<string> failures)
{
    const string input =
        """
        @inject WeatherForecastService Service
        <div>
        <p>Hello</p>
        </div>
        @using Zebra
        @code {
        private int count=0;
        void Increment(){
        count++;
        }
        }
        @page "/counter"
        @using Alpha
        """;

    const string expected =
        """
        @using Alpha

        @using Zebra

        @page "/counter"

        <div>
            <p>Hello</p>
        </div>

        @inject WeatherForecastService Service

        @code
        {
            private int count = 0;

            void Increment()
            {
                count++;
            }
        }
        """;

    AssertEqual("ShouldFormatTopLevelSections", expected + "\n", RazorFormatter.Format(input), failures);
}

static void ShouldIndentMarkupBlocks(List<string> failures)
{
    const string input =
        """
        <div>
        @if (true)
        {
        <span>Hello</span>
        }
        </div>
        """;

    const string expected =
        """
        <div>
            @if (true)
            {
                <span>Hello</span>
            }
        </div>
        """;

    AssertEqual("ShouldIndentMarkupBlocks", expected + "\n", RazorFormatter.Format(input), failures);
}

static async Task ShouldSupportCliStandardInputAsync(string repoRoot, List<string> failures)
{
    const string input =
        """
        @inject WeatherForecastService Service
        <div>
        <p>Hello</p>
        </div>
        @using Zebra
        @page "/counter"
        """;

    var result = await RunProcessAsync(
        "dotnet",
        $"run --project {Path.Combine(repoRoot, "src", "Razorpier.Tool", "Razorpier.Tool.csproj")} -- --stdin",
        input);

    if (result.ExitCode != 0 || !result.StdOut.Contains("@using Zebra", StringComparison.Ordinal) || !result.StdOut.Contains("    <p>Hello</p>", StringComparison.Ordinal))
    {
        failures.Add($"[FAIL] ShouldSupportCliStandardInputAsync\nExit: {result.ExitCode}\nStdOut:\n{result.StdOut}\nStdErr:\n{result.StdErr}");
    }
}

static async Task ShouldFormatFixtureThroughMsBuildTargetAsync(string repoRoot, List<string> failures)
{
    var fixtureSource = Path.Combine(repoRoot, "tests", "fixtures", "MsBuildSample");
    var tempRoot = Path.Combine(Path.GetTempPath(), "razorpier-msbuild-" + Guid.NewGuid().ToString("N"));
    CopyDirectory(fixtureSource, tempRoot);
    var projectPath = Path.Combine(tempRoot, "MsBuildSample.csproj");
    var projectText = await File.ReadAllTextAsync(projectPath);
    projectText = projectText.Replace("__REPO_ROOT__", repoRoot, StringComparison.Ordinal);
    await File.WriteAllTextAsync(projectPath, projectText);

    var result = await RunProcessAsync("dotnet", $"build {projectPath}");
    var componentPath = Path.Combine(tempRoot, "Component.razor");
    var content = await File.ReadAllTextAsync(componentPath);

    if (result.ExitCode != 0 || !content.Contains("    <p>Hello</p>", StringComparison.Ordinal) || !content.StartsWith("@using Zebra", StringComparison.Ordinal))
    {
        failures.Add($"[FAIL] ShouldFormatFixtureThroughMsBuildTargetAsync\nExit: {result.ExitCode}\nStdOut:\n{result.StdOut}\nStdErr:\n{result.StdErr}\nFormatted:\n{content}");
    }
}

static void AssertEqual(string name, string expected, string actual, List<string> failures)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        failures.Add(
            $"""
            [FAIL] {name}
            Expected:
            ---
            {expected}
            ---
            Actual:
            ---
            {actual}
            ---
            """);
    }
}

static void CopyDirectory(string sourceDir, string destinationDir)
{
    Directory.CreateDirectory(destinationDir);

    foreach (var file in Directory.GetFiles(sourceDir))
    {
        File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), overwrite: true);
    }

    foreach (var directory in Directory.GetDirectories(sourceDir))
    {
        CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
    }
}

static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string fileName, string arguments, string? standardInput = null)
{
    var startInfo = new ProcessStartInfo(fileName, arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = standardInput != null,
        UseShellExecute = false,
    };

    using var process = Process.Start(startInfo)!;
    if (standardInput != null)
    {
        await process.StandardInput.WriteAsync(standardInput);
        process.StandardInput.Close();
    }

    var stdoutTask = process.StandardOutput.ReadToEndAsync();
    var stderrTask = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    return (process.ExitCode, await stdoutTask, await stderrTask);
}

static string FindRepoRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Razorpier.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate the repository root.");
}
