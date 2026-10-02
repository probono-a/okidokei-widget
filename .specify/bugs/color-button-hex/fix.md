# Bug Fix: 詳細設定の色選択ボタンに、カラーコードが透明度込みの 8 桁で表示される

- **Slug**: color-button-hex
- **Fixed**: 2026-10-02
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`#AARRGGBB` → `#RRGGBB` の変換を `ColorHexResolver.ToRgbHex` として Core に移した。
詳細設定のボタンと色の選択画面の両方がこれを使い、どちらも 6 桁で表示するようにした。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.Core/Settings/ColorHexResolver.cs` | modified | `ToRgbHex` を追加。パース不可の値はデフォルト色にしてから変換する |
| `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` | modified | `UpdateColorButtonLabel` で `ToRgbHex` を使う (文字色 2 つ・背景色の 3 ボタン共通) |
| `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs` | modified | private な `ToRgbHex` を削除し、Core の `ToRgbHex` を使う |
| `tests/OkidokeiWidget.Core.Tests/Settings/ColorHexResolverTests.cs` | added test | `ToRgbHex` のテスト 2 件 (4 ケース) |
| `specs/001-clock-widget/tasks.md` | modified | Phase 27 (T147・T148) を追記 |

## Diff Highlights

```csharp
// ColorHexResolver.cs
public static string ToRgbHex(string color) => "#" + Resolve(color).Substring(3);

// SettingsWindow.xaml.cs
button.Content = $"{label}…({ColorHexResolver.ToRgbHex(color)})";
```

## Tests Added or Updated

- `ColorHexResolverTests.ToRgbHex_有効な16進ARGB文字列はアルファ成分を除いた6桁を返す` — `#FFFFFFFF` → `#FFFFFF`、`#80112233` → `#112233`
- `ColorHexResolverTests.ToRgbHex_パース不可な文字列はデフォルト色の6桁を返す` — 空文字・6 桁の値で `#FFFFFF`

## Local Verification

- Commands run: `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` → 169 件すべて成功
- Commands run: `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Debug` → 警告 0・エラー 0
  - Release 構成は、起動中のアプリが出力先の DLL をつかんでいてコピーに失敗したため、Debug 構成でコンパイルを確認した
- Manual checks: 実機での確認は `/speckit-bug-test` で行う

## Deviations from Assessment

なし

## Follow-ups

なし
