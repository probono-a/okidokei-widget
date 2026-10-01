# Bug Fix: 閉じたウィジェットの時計更新タイマーが止まらず、ウィンドウがメモリに残り続ける

- **Slug**: clock-timer-leak
- **Fixed**: 2026-09-28
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`ClockWindow` で `OnClosed` を override し、時計更新タイマーを止めるようにした。
閉じたウィンドウがタイマー経由で Dispatcher から参照され続けることがなくなる。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/ClockWindow.xaml.cs` | modified | `OnClosed` の override を追加し、`_timer.Stop()` を呼ぶ |
| `specs/001-clock-widget/tasks.md` | modified | Phase 20 (T106) を追記 |

## Diff Highlights (optional)

```csharp
protected override void OnClosed(EventArgs e)
{
    _timer.Stop();
    base.OnClosed(e);
}
```

## Tests Added or Updated

- なし。WPF のウィンドウとタイマーは Core のテストの対象外 (assessment のとおり)

## Local Verification

- `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Release -o <作業用フォルダ>` → 警告 0、エラー 0
  - 既定の出力先は、起動中のウィジェットが exe を使用中でコピーできなかった。そのため出力先だけ変えた
- `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` → 92 件すべて合格

## Deviations from Assessment

- なし

## Follow-ups

- なし
