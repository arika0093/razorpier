# razorpier

Razor ファイル向けの `Prettier` / `CSharpier` 相当 formatter です。  
`CSharpier` の補完的プロジェクトとして、同等の使い勝手と設計方針を目指します。

## ポリシー

- 設定不要（Prettier の哲学に準拠）
  - オプションは（ほぼ）提供しない
- 一貫した自動整形
  - 並び順や空白・改行ルールを formatter が決定する

## 整形仕様（現時点の要件）

Razor ファイル内のトップレベル要素を、以下の順で自動ソートします。

1. `@using`
2. `@page`
3. `@attributes`
4. Razor markup（要件上の `razor-contents`。markup / directive などの本文）
5. `@inject`
6. `@code`（Razor code）

### ブロック別の整形方針

- Razor markup（`razor-contents`）
  - 一般的な HTML-like formatter の振る舞いを参考に整形
- `@code` 内部
  - `CSharpier` API を呼び出して C# を整形

## 実装済みパッケージ

- API パッケージ（`src/Razorpier.Core`）
- dotnet tool（`src/Razorpier.Tool`）
- MSBuild 連携（`src/Razorpier.MSBuild`）
- VS Code 拡張（`src/Razorpier.VSCode`）
- Visual Studio 拡張（`src/Razorpier.VisualStudio`）

## 使い方

### API

```csharp
using Razorpier.Core;

var formatted = RazorFormatter.Format(source);
```

### dotnet tool

```bash
dotnet run --project src/Razorpier.Tool/Razorpier.Tool.csproj -- path/to/Component.razor
dotnet run --project src/Razorpier.Tool/Razorpier.Tool.csproj -- --check path/to/Component.razor
dotnet run --project src/Razorpier.Tool/Razorpier.Tool.csproj -- --stdin < path/to/Component.razor
```

### MSBuild 連携

`Razorpier.MSBuild` パッケージを参照し、ビルド時整形を有効にします。

```xml
<PropertyGroup>
  <RazorpierFormatOnBuild>true</RazorpierFormatOnBuild>
</PropertyGroup>
```

### VS Code 拡張

```bash
cd src/Razorpier.VSCode
npm run build:server
```

- Razor ドキュメントフォーマッタを提供します。
- コマンド `Razorpier: Format Razor Document` を追加します。

### Visual Studio 拡張

```bash
dotnet build src/Razorpier.VisualStudio/Razorpier.VisualStudio.csproj
```

- Tools メニューに `Format Razor Document` コマンドを追加します。
- アクティブな `.razor` ドキュメントに対して整形を実行します。

## テスト

```bash
dotnet build Razorpier.sln
dotnet run --project tests/Razorpier.Tests/Razorpier.Tests.csproj
cd src/Razorpier.VSCode && npm test
```
