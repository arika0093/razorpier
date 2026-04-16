# razorpier

Razor ファイル向けの `prettier` / `csharpier` 相当 formatter です。  
`csharpier` の補完プロジェクトとして、同等の使い勝手と設計方針を目指します。

## ポリシー

- 設定不要（prettier の哲学に準拠）
  - オプションは（ほぼ）提供しない
- 一貫した自動整形
  - 並び順や空白・改行ルールを formatter が決定する

## 整形仕様（現時点の要件）

Razor ファイル内のトップレベル要素を、以下の順で自動ソートします。

1. `@using`
2. `@page`
3. `@attributes`
4. Razor contents（markup / directive などの本文）
5. `@inject`
6. `@code`（Razor code）

### ブロック別の整形方針

- Razor contents
  - 一般的な HTML-like formatter の振る舞いを参考に整形
- `@code` 内部
  - `csharpier` API を呼び出して C# を整形

## 提供形態

`csharpier` 周辺エコシステムに準じて、以下の利用形態を提供します。

- Visual Studio 拡張
- VS Code 拡張
- dotnet tool
- MSBuild 連携
- API パッケージ
