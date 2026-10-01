# Bug Fix: 壊れた設定ファイルが警告の前に上書きされる・一部が null だと起動直後に落ちる

- **Slug**: broken-settings-file
- **Fixed**: 2026-09-28
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

- #51: `Load` で、設定の一部が `null` のファイルもパース不可として扱い、デフォルト設定と通知フラグを返すようにした
- #50: 読めなかった設定ファイルを、起動時の保存より前に `settings.json.bak` としてコピーして残し、警告文にその場所を書き添えるようにした

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` | modified | `HasNullSection` で `null` を調べる。`BackupBrokenFile` を追加 |
| `src/OkidokeiWidget.App/App.xaml.cs` | modified | `OnStartup` で、読み込みに失敗したら保存の前に `BackupBrokenFile` を呼ぶ。警告文に残した場所を書き添える |
| `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` | added test | 6 件追加 (Theory の 4 件を含む) |
| `specs/001-clock-widget/contracts/settings-file.md` | modified | 「読み込み契約」に `null` の扱いと `.bak` を追記 |
| `specs/001-clock-widget/tasks.md` | modified | Phase 22 (T110〜T113) を追記 |

## Diff Highlights (optional)

```csharp
// SettingsRepository.Load
return settings is null || HasNullSection(settings) ? (WidgetSettings.CreateDefault(), true) : (settings, false);

// App.OnStartup
var backupPath = fellBackToDefaults ? SettingsRepository.BackupBrokenFile() : null;
RefreshConnectedMonitors();
SettingsRepository.Save(settings);
```

## Tests Added or Updated

- `SettingsRepositoryTests.Load_設定の一部がnullの場合はデフォルト値へフォールバックし通知フラグを立てる`
  - `Appearance`・`WindowBehavior`・`Monitors`・モニタの値が `null` の 4 通り
- `SettingsRepositoryTests.BackupBrokenFile_読めなかった設定ファイルを残し後の保存で上書きされない`
  - 起動時と同じ順序 (読み込み → `.bak` を残す → 保存) で、`.bak` に元の内容が残ること
- `SettingsRepositoryTests.BackupBrokenFile_設定ファイルが存在しない場合は何もせずnullを返す`

## Local Verification

- 追加したテストを、修正前のコードに対して実行した
  - `BackupBrokenFile` がまだないため、その呼び出しだけ一時的に `null` に置き換えた
  - `null` の 4 件と `.bak` の 1 件が失敗することを確認した
  - 「ファイルがなければ何もしない」の 1 件は、修正前から成り立つ内容なので合格した
- `dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Release -o <作業用フォルダ>` → 警告 0、エラー 0
  - 起動中のウィジェットが既定の出力先の exe を使用中のため、出力先だけ変えた
- `dotnet test tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` → 101 件すべて合格 (6 件追加)

## Deviations from Assessment

- なし

## Follow-ups

- なし
