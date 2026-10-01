# Bug Fix: 詳細設定で自動起動を切り替えたときに失敗すると、アプリごと落ちる

- **Slug**: autostart-toggle-crash
- **Fixed**: 2026-09-28
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

- `AutoStartManager` に、失敗したら例外を投げずに false を返す `TrySetEnabled` を追加した
- 詳細設定での切り替えはこれを使い、失敗したら設定値を戻して MessageBox で知らせる
- チェックの表示も設定値に合わせ直すので、表示と実際のショートカットの有無が食い違わない

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs` | modified | `TrySetEnabled` を追加 |
| `src/OkidokeiWidget.App/App.xaml.cs` | modified | `OnAutoStartChanged` を `TrySetEnabled` に切り替え、失敗時は設定値を戻して通知する |
| `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` | modified | 切り替えの後、チェックの表示を設定値に合わせ直す |
| `tests/OkidokeiWidget.Core.Tests/Persistence/AutoStartManagerTests.cs` | added test | 3 件追加 |
| `specs/001-clock-widget/tasks.md` | modified | Phase 21 (T107〜T109) を追記 |

## Diff Highlights (optional)

```csharp
// AutoStartManager
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or COMException or InvalidOperationException)
{
    return false;
}

// App.OnAutoStartChanged
if (!AutoStartManager.TrySetEnabled(_settings.AutoStartEnabled))
{
    _settings.AutoStartEnabled = !_settings.AutoStartEnabled;
    MessageBox.Show("自動起動の設定を変更できませんでした。", ...);
    return;
}
```

## Tests Added or Updated

- `AutoStartManagerTests.TrySetEnabled_成功した場合はtrueを返しショートカットを作成する`
- `AutoStartManagerTests.TrySetEnabled_ショートカットを保存できない場合は例外を投げずfalseを返す`
  - Startup フォルダに `OkidokeiWidget.lnk` という名前のフォルダを作り、保存を失敗させる
- `AutoStartManagerTests.TrySetEnabled_スタートアップフォルダを作れない場合は例外を投げずfalseを返す`
  - Startup フォルダの場所に同名のファイルを置き、フォルダの作成を失敗させる

## Local Verification

- 修正前の `SetEnabled` が上の 2 つの状況で投げる例外を、一時的なテストで確認した (確認後に削除)
  - ショートカットを保存できない: `UnauthorizedAccessException` (COM の `E_ACCESSDENIED` が変換されたもの)
  - フォルダを作れない: `IOException`
  - どちらも `TrySetEnabled` が受け止める種類に含まれる
- `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Release -o <作業用フォルダ>` → 警告 0、エラー 0
  - 起動中のウィジェットが既定の出力先の exe を使用中のため、出力先だけ変えた
- `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` → 95 件すべて合格 (3 件追加)

## Deviations from Assessment

- なし

## Follow-ups

- なし
