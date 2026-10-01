# Bug Fix: コードで作る SolidColorBrush に Freeze() していない

- **Slug**: freeze-brushes
- **Fixed**: 2026-09-28
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

コードで作っている `SolidColorBrush` 4 箇所を、作った直後に `Freeze()` してから使うようにした。
どのブラシも作った後に変更しないため、WPF の変更監視を省ける。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/ClockWindow.xaml.cs` | modified | 文字色 2 つと背景色のブラシを `CreateFrozenBrush` で作る |
| `src/OkidokeiWidget.App/ColorPickerWindow.xaml.cs` | modified | パレットとプレビューのブラシを `CreateFrozenBrush` で作る |

## Diff Highlights

各クラスに、ブラシを作って Freeze するだけの小さなヘルパーを置いた。

```csharp
private static SolidColorBrush CreateFrozenBrush(Color color)
{
    var brush = new SolidColorBrush(color);
    brush.Freeze();
    return brush;
}
```

## Tests Added or Updated

- なし。App 層に自動テストはない (research.md #7)

## Local Verification

- `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Debug` → 成功 (警告 0、エラー 0)
- `dotnet build ... -c Release` → コンパイルは通ったが、起動中のウィジェットが exe と dll を使っていたため、出力先へのコピーで失敗
  - 実機確認の前に、ウィジェットを終了してから Release ビルドをやり直す
- `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` → 119 件すべて合格
- 実機での確認は `/speckit-bug-test` で行う

## Deviations from Assessment

- なし

## Follow-ups

- なし
