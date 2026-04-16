# razorpier

razorpier is a Razor formatter with a Prettier/CSharpier-style workflow.
It complements `CSharpier` and aims to provide the same kind of predictable, low-configuration experience for `.razor` files.

## Principles

- Zero-configuration by default
- Consistent automatic formatting
- Formatter-defined whitespace, ordering, and line breaking rules

## Current formatting behavior

Top-level Razor sections are sorted in this order:

1. `@using`
2. `@page`
3. `@attributes`
4. Razor markup (`razor-contents`)
5. `@inject`
6. `@code`

### Section-specific behavior

- Razor markup
  - Indents nested markup and Razor control flow consistently
  - Formats HTML/Razor tag attributes with expression normalization and wrapping around 120 columns
  - Converts empty elements to self-closing tags when appropriate
- `@code`
  - Uses the `CSharpier` API to format embedded C#

## Implemented packages

- API package (`/home/runner/work/razorpier/razorpier/src/Razorpier.Core`)
- dotnet tool (`/home/runner/work/razorpier/razorpier/src/Razorpier.Tool`)
- MSBuild integration (`/home/runner/work/razorpier/razorpier/src/Razorpier.MSBuild`)
- VS Code extension (`/home/runner/work/razorpier/razorpier/src/Razorpier.VSCode`)
- Visual Studio extension (`/home/runner/work/razorpier/razorpier/src/Razorpier.VisualStudio`)

## Usage

### API

```csharp
using Razorpier.Core;

var formatted = RazorFormatter.Format(source);
```

### dotnet tool

```bash
dotnet run --project /home/runner/work/razorpier/razorpier/src/Razorpier.Tool/Razorpier.Tool.csproj -- path/to/Component.razor
dotnet run --project /home/runner/work/razorpier/razorpier/src/Razorpier.Tool/Razorpier.Tool.csproj -- --check path/to/Component.razor
dotnet run --project /home/runner/work/razorpier/razorpier/src/Razorpier.Tool/Razorpier.Tool.csproj -- --stdin < path/to/Component.razor
```

### MSBuild integration

Reference `Razorpier.MSBuild` and enable formatting during builds.

```xml
<PropertyGroup>
  <RazorpierFormatOnBuild>true</RazorpierFormatOnBuild>
</PropertyGroup>
```

### VS Code extension

```bash
cd /home/runner/work/razorpier/razorpier/src/Razorpier.VSCode
npm run build:server
```

- Provides a Razor document formatter
- Adds the `Razorpier: Format Razor Document` command

### Visual Studio extension

```bash
dotnet build /home/runner/work/razorpier/razorpier/src/Razorpier.VisualStudio/Razorpier.VisualStudio.csproj
```

- Adds a `Format Razor Document` command to the Tools menu
- Formats the active `.razor` document

## Tests

```bash
dotnet build /home/runner/work/razorpier/razorpier/Razorpier.sln
dotnet run --project /home/runner/work/razorpier/razorpier/tests/Razorpier.Tests/Razorpier.Tests.csproj
cd /home/runner/work/razorpier/razorpier/src/Razorpier.VSCode && npm test
```
